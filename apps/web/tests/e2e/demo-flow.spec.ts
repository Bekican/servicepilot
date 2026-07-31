import { expect, test } from "@playwright/test";

test("protected routes redirect to login", async ({ page }) => {
  await page.goto("/dashboard");

  await expect(page).toHaveURL(/\/login$/);
  await expect(
    page.getByRole("heading", { name: "Hesabınıza giriş yapın" }),
  ).toBeVisible();
});
