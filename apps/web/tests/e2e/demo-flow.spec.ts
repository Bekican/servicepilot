import { expect, test } from "@playwright/test";

test("protected routes redirect to login", async ({ page }) => {
  await page.goto("/dashboard");

  await expect(page).toHaveURL(/\/login$/);
  await expect(
    page.getByRole("heading", { name: "Hesabınıza giriş yapın" }),
  ).toBeVisible();
});

test("demo owner can reach the operational dashboard", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Organizasyon adresi").fill("servicepilot-demo");
  await page.getByLabel("E-posta").fill("owner@servicepilot.local");
  await page.getByLabel("Parola").fill("Demo1234!");
  await page.getByRole("button", { name: "Giriş yap" }).click();

  await expect(page).toHaveURL(/\/dashboard$/);
  await expect(
    page.getByRole("heading", { name: "Genel Bakış" }),
  ).toBeVisible();
  await expect(page.getByText("Bugünkü randevular")).toBeVisible();
  await expect(page.getByRole("link", { name: "Müşteriler" })).toBeVisible();

  await page.getByRole("link", { name: "Müşteriler" }).click();
  await expect(page.getByRole("heading", { name: "Müşteriler" })).toBeVisible();
  await expect(page.getByText("Ahmet Yılmaz")).toBeVisible();
});
