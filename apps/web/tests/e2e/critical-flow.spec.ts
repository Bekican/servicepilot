import { expect, test } from "@playwright/test";

import {
  apiRequest,
  futureLocalDateTime,
  invitationLinkFor,
  loginThroughApi,
  loginThroughUi,
  markReminderFailed,
  registerThroughApi,
  registerThroughUi,
  uniqueOrganization,
} from "./support";

test("owner and technician can complete the MVP operational journey", async ({
  browser,
  page,
}) => {
  test.setTimeout(90_000);
  const organization = uniqueOrganization("operasyon");
  const technicianEmail = `teknisyen-${Date.now()}@servicepilot.test`;
  const technicianPassword = "Teknisyen123!";

  await registerThroughUi(page, organization);
  await expect(
    page.getByRole("heading", { name: "Genel Bakış" }),
  ).toBeVisible();
  await expect(page.getByText("Bugünkü randevular")).toBeVisible();

  await page.getByRole("link", { name: "Müşteriler" }).click();
  await page
    .getByRole("link", { name: /Yeni Müşteri|İlk müşteriyi oluştur/ })
    .first()
    .click();
  await page.getByLabel("Ad", { exact: true }).fill("Ayşe");
  await page.getByLabel("Soyad", { exact: true }).fill("Yılmaz");
  await page.getByLabel("E-posta").fill("ayse.yilmaz@servicepilot.test");
  await page.getByLabel("Telefon").fill("+905551112233");
  await page.getByRole("button", { name: "Müşteriyi oluştur" }).click();
  await expect(page).toHaveURL(/\/customers\/[0-9a-f-]+/);
  const customerId = page.url().match(/\/customers\/([0-9a-f-]+)/)?.[1];
  expect(customerId).toBeTruthy();
  await expect(
    page.getByRole("heading", { name: "Ayşe Yılmaz" }),
  ).toBeVisible();

  await page.getByRole("button", { name: "Pasifleştir" }).click();
  await expect(
    page.getByRole("alertdialog", { name: "Müşteri pasifleştirilsin mi?" }),
  ).toBeVisible();
  await page.getByRole("button", { name: "Vazgeç" }).click();
  await expect(page.getByText(/CUS-\d+ · Aktif/)).toBeVisible();

  await page.getByRole("link", { name: "Müşteriler" }).click();
  await page.getByRole("link", { name: "Yeni Müşteri" }).click();
  await page.getByLabel("Ad", { exact: true }).fill("Şule");
  await page.getByLabel("Soyad", { exact: true }).fill("Çetin");
  await page.getByLabel("E-posta").fill("ayse.yilmaz@servicepilot.test");
  await page.getByRole("button", { name: "Müşteriyi oluştur" }).click();
  await expect(
    page.getByRole("alert").filter({
      hasText: "Bu e-posta başka bir müşteride kullanılıyor.",
    }),
  ).toBeVisible();
  await expect(page.getByLabel("Ad", { exact: true })).toHaveValue("Şule");

  await page.getByRole("link", { name: "Hizmetler" }).click();
  await page.getByLabel("Hizmet adı").fill("Klima Bakımı");
  await page.getByLabel("Varsayılan süre").fill("60");
  await page.getByRole("button", { name: "Hizmeti oluştur" }).click();
  await expect(page.getByText("Hizmet oluşturuldu")).toBeVisible();

  await page.getByRole("link", { name: "Kullanıcılar" }).click();
  await page.getByLabel("E-posta").fill(technicianEmail);
  await page.getByLabel("Başlangıç rolü").selectOption("Technician");
  await page.getByRole("button", { name: "Davet gönder" }).click();
  await expect(page.getByText("Davet e-postası gönderildi")).toBeVisible({
    timeout: 15_000,
  });

  const invitationLink = await invitationLinkFor(technicianEmail);
  const technicianContext = await browser.newContext();
  const technicianPage = await technicianContext.newPage();
  await technicianPage.goto(invitationLink);
  await technicianPage.evaluate(() =>
    window.history.replaceState({}, "", "/invitations/accept"),
  );
  await technicianPage.getByLabel("Ad", { exact: true }).fill("Özgür");
  await technicianPage.getByLabel("Soyad", { exact: true }).fill("Şahin");
  await technicianPage.getByLabel("Parola").fill(technicianPassword);
  await technicianPage.getByRole("button", { name: "Daveti kabul et" }).click();
  await technicianPage.waitForURL(/\/appointments$/);

  await page.getByRole("link", { name: "Randevular" }).click();
  await page
    .getByRole("link", { name: /Yeni Randevu|Randevu Oluştur/ })
    .first()
    .click();
  await page.getByLabel("Başlangıç").fill(futureLocalDateTime());
  await page.getByRole("button", { name: "Randevuyu oluştur" }).click();
  await expect(page).toHaveURL(/\/appointments\/[0-9a-f-]+/);
  const appointmentId = page.url().match(/\/appointments\/([0-9a-f-]+)/)?.[1];
  expect(appointmentId).toBeTruthy();

  await page.getByLabel("Teknisyen").selectOption({ label: "Özgür Şahin" });
  await page.getByRole("button", { name: "Atamayı kaydet" }).click();
  await expect(page.getByText("Teknisyen atandı")).toBeVisible();
  await page.getByRole("button", { name: "Onaylandı" }).click();
  await expect(page.getByText("Randevu durumu güncellendi")).toBeVisible();

  await technicianPage.goto(`/appointments/${appointmentId}`);
  await technicianPage.getByRole("button", { name: "Devam ediyor" }).click();
  await expect(
    technicianPage.getByText("Randevu durumu güncellendi"),
  ).toBeVisible();
  await technicianPage.getByRole("button", { name: "Tamamlandı" }).click();
  await expect(
    technicianPage.getByText("Randevu durumu güncellendi"),
  ).toBeVisible();

  await markReminderFailed(appointmentId!);
  await page.goto("/reminders?status=Failed");
  await expect(page.getByText("SMTP bağlantısı kurulamadı.")).toBeVisible();
  await page.getByRole("button", { name: "Tekrar dene" }).click();
  await expect(
    page.getByText("Hatırlatma yeniden kuyruğa alındı"),
  ).toBeVisible();

  const ownerToken = await loginThroughApi(organization);
  const otherOrganization = uniqueOrganization("diger-tenant");
  const otherOwner = await registerThroughApi(otherOrganization);
  const crossTenantResponse = await fetch(
    `${process.env.SERVICEPILOT_E2E_API_URL ?? "http://127.0.0.1:15267"}/api/customers/${customerId}`,
    {
      body: JSON.stringify({
        type: "Individual",
        firstName: "Yetkisiz",
        lastName: "Değişiklik",
        companyName: null,
        contactPerson: null,
        email: null,
        phone: null,
      }),
      headers: {
        Authorization: `Bearer ${otherOwner.accessToken}`,
        "Content-Type": "application/json",
      },
      method: "PUT",
    },
  );
  expect(crossTenantResponse.status).toBe(404);

  const unchangedCustomer = await apiRequest<{ firstName: string }>(
    `/api/customers/${customerId}`,
    { expectedStatus: 200, token: ownerToken.accessToken },
  );
  expect(unchangedCustomer.firstName).toBe("Ayşe");

  const otherContext = await browser.newContext();
  const otherPage = await otherContext.newPage();
  await loginThroughUi(
    otherPage,
    otherOrganization.slug,
    otherOrganization.email,
    otherOrganization.password,
  );
  await otherPage.goto(`/customers/${customerId}`);
  await expect(
    otherPage.getByRole("heading", { name: "Kayıt bulunamadı" }),
  ).toBeVisible();

  await otherContext.close();
  await technicianContext.close();
});
