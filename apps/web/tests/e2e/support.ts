import { expect, type Page } from "@playwright/test";
import { Client } from "pg";

export const apiUrl =
  process.env.SERVICEPILOT_E2E_API_URL ?? "http://127.0.0.1:15267";
const mailpitUrl =
  process.env.SERVICEPILOT_E2E_MAILPIT_URL ?? "http://127.0.0.1:18025";
const databaseUrl =
  process.env.SERVICEPILOT_E2E_DATABASE_URL ??
  "postgresql://servicepilot_e2e:e2e-only-password@127.0.0.1:15432/servicepilot_e2e";

export type TestOrganization = {
  email: string;
  name: string;
  ownerFirstName: string;
  ownerLastName: string;
  password: string;
  slug: string;
};

type AuthenticationResponse = {
  accessToken: string;
  organizationId: string;
  userId: string;
};

export function uniqueOrganization(prefix: string): TestOrganization {
  const suffix = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`;
  return {
    email: `owner-${suffix}@servicepilot.test`,
    name: `${prefix} ${suffix}`,
    ownerFirstName: "İpek",
    ownerLastName: "Çağlar",
    password: "E2eDemo123!",
    slug: `${prefix.toLocaleLowerCase("en-US")}-${suffix}`,
  };
}

export async function registerThroughUi(
  page: Page,
  organization: TestOrganization,
) {
  await page.goto("/register");
  await page.getByLabel("Organizasyon adı").fill(organization.name);
  await page.getByLabel("Organizasyon kısa adı").fill(organization.slug);
  await page
    .getByLabel("Ad", { exact: true })
    .fill(organization.ownerFirstName);
  await page
    .getByLabel("Soyad", { exact: true })
    .fill(organization.ownerLastName);
  await page.getByLabel("E-posta").fill(organization.email);
  await page.getByLabel("Parola").fill(organization.password);
  await page.getByRole("button", { name: "Organizasyonu oluştur" }).click();
  await expect(page).toHaveURL(/\/dashboard$/, { timeout: 15_000 });
}

export async function loginThroughUi(
  page: Page,
  organizationSlug: string,
  email: string,
  password: string,
) {
  await page.goto("/login");
  await page.getByLabel("Organizasyon adresi").fill(organizationSlug);
  await page.getByLabel("E-posta").fill(email);
  await page.getByLabel("Parola").fill(password);
  await page.getByRole("button", { name: "Giriş yap" }).click();
  await page.waitForURL((url) => !url.pathname.endsWith("/login"));
}

export async function registerThroughApi(
  organization: TestOrganization,
): Promise<AuthenticationResponse> {
  return apiRequest<AuthenticationResponse>("/api/auth/register", {
    body: {
      organizationName: organization.name,
      organizationSlug: organization.slug,
      firstName: organization.ownerFirstName,
      lastName: organization.ownerLastName,
      email: organization.email,
      password: organization.password,
      timeZoneId: "Europe/Istanbul",
    },
    expectedStatus: 201,
    method: "POST",
  });
}

export async function loginThroughApi(
  organization: TestOrganization,
): Promise<AuthenticationResponse> {
  return apiRequest<AuthenticationResponse>("/api/auth/login", {
    body: {
      organizationSlug: organization.slug,
      email: organization.email,
      password: organization.password,
    },
    expectedStatus: 200,
    method: "POST",
  });
}

export async function apiRequest<T>(
  path: string,
  options: {
    body?: unknown;
    expectedStatus: number;
    method?: string;
    token?: string;
  },
): Promise<T> {
  const response = await fetch(`${apiUrl}${path}`, {
    body: options.body ? JSON.stringify(options.body) : undefined,
    headers: {
      ...(options.body ? { "Content-Type": "application/json" } : {}),
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
    },
    method: options.method ?? "GET",
  });

  if (response.status !== options.expectedStatus) {
    const detail = await response.text();
    throw new Error(
      `${options.method ?? "GET"} ${path} returned ${response.status}: ${detail}`,
    );
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

type MailpitMessage = {
  ID: string;
  To: Array<{ Address: string }>;
};

export async function invitationLinkFor(
  recipient: string,
  timeoutMs = 10_000,
): Promise<string> {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const listResponse = await fetch(`${mailpitUrl}/api/v1/messages`);
    const list = (await listResponse.json()) as { messages: MailpitMessage[] };
    const message = list.messages.find((candidate) =>
      candidate.To.some(
        (address) => address.Address.toLowerCase() === recipient.toLowerCase(),
      ),
    );

    if (message) {
      const detailResponse = await fetch(
        `${mailpitUrl}/api/v1/message/${message.ID}`,
      );
      const detail = (await detailResponse.json()) as { Text: string };
      const link = detail.Text.match(/https?:\/\/\S+/)?.[0];
      if (link) return link;
    }

    await new Promise((resolve) => setTimeout(resolve, 250));
  }

  throw new Error(`Invitation email was not delivered to ${recipient}`);
}

export async function markReminderFailed(appointmentId: string) {
  const client = new Client({ connectionString: databaseUrl });
  await client.connect();
  try {
    const result = await client.query<{ id: string }>(
      `UPDATE reminders
       SET status = 'Failed',
           attempt_count = 3,
           next_attempt_at_utc = NULL,
           processing_started_at_utc = NULL,
           last_attempt_at_utc = NOW(),
           last_error = 'SMTP bağlantısı kurulamadı.',
           updated_at_utc = NOW()
       WHERE appointment_id = $1
       RETURNING id`,
      [appointmentId],
    );
    if (result.rowCount !== 1) {
      throw new Error("Appointment reminder could not be prepared as Failed");
    }
  } finally {
    await client.end();
  }
}

export function futureLocalDateTime(daysAhead = 2) {
  const value = new Date();
  value.setDate(value.getDate() + daysAhead);
  value.setHours(10, 0, 0, 0);
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}T10:00`;
}
