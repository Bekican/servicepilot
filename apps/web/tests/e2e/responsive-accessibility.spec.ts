import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

import {
  apiRequest,
  loginThroughUi,
  registerThroughApi,
  uniqueOrganization,
} from "./support";

test("critical screens are responsive and accessible", async ({
  page,
}, testInfo) => {
  test.setTimeout(60_000);
  const organization = uniqueOrganization(`a11y-${testInfo.project.name}`);
  const owner = await registerThroughApi(organization);

  const customer = await apiRequest<{ id: string }>("/api/customers", {
    body: {
      type: "Individual",
      firstName: "Gökçe",
      lastName: "Işık",
      companyName: null,
      contactPerson: null,
      email: `gokce-${Date.now()}@servicepilot.test`,
      phone: "+905559998877",
    },
    expectedStatus: 201,
    method: "POST",
    token: owner.accessToken,
  });
  const service = await apiRequest<{ id: string }>("/api/services", {
    body: { name: "Elektrik Tesisatı", defaultDurationMinutes: 45 },
    expectedStatus: 201,
    method: "POST",
    token: owner.accessToken,
  });
  const startAt = new Date(Date.now() + 48 * 60 * 60 * 1000);
  const appointment = await apiRequest<{ id: string }>("/api/appointments", {
    body: {
      customerId: customer.id,
      serviceId: service.id,
      technicianUserId: null,
      startAt: startAt.toISOString(),
      endAt: new Date(startAt.getTime() + 45 * 60 * 1000).toISOString(),
    },
    expectedStatus: 201,
    method: "POST",
    token: owner.accessToken,
  });

  await page.goto("/login");
  await assertAccessible(page);
  await page.keyboard.press("Tab");
  await expect(page.getByLabel("Organizasyon adresi")).toBeFocused();
  await loginThroughUi(
    page,
    organization.slug,
    organization.email,
    organization.password,
  );

  const paths = [
    "/dashboard",
    "/customers",
    "/customers/new",
    "/users",
    `/appointments/${appointment.id}`,
    "/reminders",
  ];

  for (const path of paths) {
    await page.goto(path);
    await expect(page.locator("main")).toBeVisible();
    await assertNoPageOverflow(page);
    await assertAccessible(page);
  }

  await page.goto("/dashboard");
  await expect(page.getByText("Bugünkü randevular")).toBeVisible();
  await expect(page.locator("body")).not.toContainText(/Ã|Ä|Å/);

  if (testInfo.project.name === "mobile-chromium") {
    await page.getByRole("button", { name: "Menüyü aç" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await page.getByRole("link", { name: "Müşteriler" }).click();
    await expect(page).toHaveURL(/\/customers$/);
    await expect(page.getByRole("dialog")).not.toBeVisible();
    await assertNoPageOverflow(page);
  }
});

async function assertAccessible(page: Page) {
  const result = await new AxeBuilder({ page }).analyze();
  const blocking = result.violations.filter(
    (violation) =>
      violation.impact === "serious" || violation.impact === "critical",
  );
  expect(
    blocking,
    blocking
      .map(
        (violation) =>
          `${violation.id}: ${violation.help} (${violation.nodes.length})`,
      )
      .join("\n"),
  ).toEqual([]);
}

async function assertNoPageOverflow(page: Page) {
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - window.innerWidth,
  );
  expect(overflow).toBeLessThanOrEqual(1);
}
