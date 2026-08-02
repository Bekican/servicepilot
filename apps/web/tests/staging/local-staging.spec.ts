import { expect, test, type APIRequestContext } from "@playwright/test";

import {
  loginThroughUi,
  registerThroughUi,
  type TestOrganization,
} from "../e2e/support";

function requiredEnvironment(name: string) {
  const value = process.env[name];
  if (!value) throw new Error(`${name} is required`);
  return value;
}

function smokeData() {
  const suffix = requiredEnvironment("STAGING_SMOKE_SUFFIX");
  const organization: TestOrganization = {
    email: `owner-${suffix}@servicepilot.test`,
    name: `Staging Smoke ${suffix}`,
    ownerFirstName: "İpek",
    ownerLastName: "Çağlar",
    password: requiredEnvironment("STAGING_SMOKE_PASSWORD"),
    slug: `staging-${suffix}`,
  };

  return {
    customerEmail: `customer-${suffix}@servicepilot.test`,
    customerName: `Staging ${suffix}`,
    organization,
    serviceName: `Bakım ${suffix}`,
    technicianEmail: `technician-${suffix}@servicepilot.test`,
    technicianName: `Özgür ${suffix}`,
    technicianPassword: requiredEnvironment("STAGING_SMOKE_PASSWORD"),
  };
}

type MailpitMessage = {
  ID: string;
  To: Array<{ Address: string }>;
};

async function invitationLinkFor(
  mailpit: APIRequestContext,
  recipient: string,
  timeoutMs = 15_000,
) {
  const deadline = Date.now() + timeoutMs;

  while (Date.now() < deadline) {
    const listResponse = await mailpit.get("/api/v1/messages");
    expect(listResponse.ok()).toBeTruthy();
    const list = (await listResponse.json()) as {
      messages: MailpitMessage[];
    };
    const message = list.messages.find((candidate) =>
      candidate.To.some(
        (address) => address.Address.toLowerCase() === recipient.toLowerCase(),
      ),
    );

    if (message) {
      const detailResponse = await mailpit.get(`/api/v1/message/${message.ID}`);
      expect(detailResponse.ok()).toBeTruthy();
      const detail = (await detailResponse.json()) as { Text: string };
      const link = detail.Text.match(/https?:\/\/\S+/)?.[0];
      if (link) return link;
    }

    await new Promise((resolve) => setTimeout(resolve, 250));
  }

  throw new Error("Invitation email was not delivered to the smoke recipient");
}

test("creates durable staging data through HTTPS", async ({
  browser,
  page,
  playwright,
}) => {
  test.setTimeout(90_000);
  const data = smokeData();

  await registerThroughUi(page, data.organization);
  await expect(
    page.getByRole("heading", { name: "Genel Bakış" }),
  ).toBeVisible();

  await page.goto("/customers/new");
  await page.getByLabel("Ad", { exact: true }).fill("Staging");
  await page
    .getByLabel("Soyad", { exact: true })
    .fill(requiredEnvironment("STAGING_SMOKE_SUFFIX"));
  await page.getByLabel("E-posta").fill(data.customerEmail);
  await page.getByRole("button", { name: "Müşteriyi oluştur" }).click();
  await expect(page).toHaveURL(/\/customers\/[0-9a-f-]+/);
  await expect(
    page.getByRole("heading", { name: data.customerName }),
  ).toBeVisible();

  await page.goto("/services");
  await page.getByLabel("Hizmet adı").fill(data.serviceName);
  await page.getByLabel("Varsayılan süre").fill("60");
  await page.getByRole("button", { name: "Hizmeti oluştur" }).click();
  await expect(page.getByText("Hizmet oluşturuldu")).toBeVisible();

  await page.goto("/users");
  await page.getByLabel("E-posta").fill(data.technicianEmail);
  await page.getByLabel("Başlangıç rolü").selectOption("Technician");
  await page.getByRole("button", { name: "Davet gönder" }).click();
  await expect(page.getByText("Davet e-postası gönderildi")).toBeVisible({
    timeout: 15_000,
  });

  const mailpit = await playwright.request.newContext({
    baseURL:
      process.env.SERVICEPILOT_STAGING_MAILPIT_URL ?? "https://localhost:8443",
    extraHTTPHeaders: {
      Host: "mailpit.localhost:8443",
    },
    ignoreHTTPSErrors: true,
  });

  try {
    const invitationLink = await invitationLinkFor(
      mailpit,
      data.technicianEmail,
    );
    const technicianContext = await browser.newContext({
      ignoreHTTPSErrors: true,
    });

    try {
      const technicianPage = await technicianContext.newPage();
      await technicianPage.goto(invitationLink);
      await technicianPage.evaluate(() =>
        window.history.replaceState({}, "", "/invitations/accept"),
      );
      await technicianPage.getByLabel("Ad", { exact: true }).fill("Özgür");
      await technicianPage
        .getByLabel("Soyad", { exact: true })
        .fill(requiredEnvironment("STAGING_SMOKE_SUFFIX"));
      await technicianPage.getByLabel("Parola").fill(data.technicianPassword);
      await technicianPage
        .getByRole("button", { name: "Daveti kabul et" })
        .click();
      await technicianPage.waitForURL(/\/appointments$/);
    } finally {
      await technicianContext.close();
    }
  } finally {
    await mailpit.dispose();
  }
});

test("keeps staging data after the runtime restart", async ({ page }) => {
  const data = smokeData();

  await loginThroughUi(
    page,
    data.organization.slug,
    data.organization.email,
    data.organization.password,
  );

  await page.goto("/customers");
  await expect(page.getByText(data.customerName).first()).toBeVisible();

  await page.goto("/services");
  await expect
    .poll(() =>
      page
        .locator('input[name="name"]')
        .evaluateAll((inputs) =>
          inputs.map((input) => (input as HTMLInputElement).value),
        ),
    )
    .toContain(data.serviceName);

  await page.goto("/users");
  await expect(page.getByText(data.technicianName).first()).toBeVisible();
});
