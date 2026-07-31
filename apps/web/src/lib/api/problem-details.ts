import type { ProblemDetails } from "@/lib/api/types";

const translatedMessages: Record<string, string> = {
  "Authentication.InvalidCredentials":
    "Organizasyon, e-posta veya parola hatalı.",
  "Authentication.OrganizationSlugAlreadyExists":
    "Bu organizasyon adresi daha önce kullanılmış.",
  "Authentication.EmailAlreadyExists":
    "Bu e-posta organizasyonda zaten kayıtlı.",
  "Authentication.InvalidTimeZone": "Saat dilimi geçerli değil.",
  "Customer.EmailAlreadyExists": "Bu e-posta başka bir müşteride kullanılıyor.",
  "Customer.PhoneAlreadyExists": "Bu telefon başka bir müşteride kullanılıyor.",
  "Service.NameAlreadyExists": "Bu hizmet adı zaten kullanılıyor.",
  "Appointment.TechnicianOverlap":
    "Seçilen teknisyenin bu saat aralığında başka bir randevusu var.",
  "Appointment.InvalidTransition":
    "Bu randevu için seçilen durum değişikliği yapılamaz.",
  "Appointment.InvalidData":
    "Randevu zamanı geçerli değil veya geçmişte kalıyor.",
  "User.SelfModificationNotAllowed":
    "Kendi rolünüzü veya durumunuzu değiştiremezsiniz.",
  "User.LastActiveOwner": "Son aktif Owner pasifleştirilemez.",
};

export function problemMessage(
  problem: ProblemDetails | undefined,
  fallback = "İşlem tamamlanamadı. Lütfen tekrar deneyin.",
) {
  if (!problem) {
    return fallback;
  }

  if (problem.title && translatedMessages[problem.title]) {
    return translatedMessages[problem.title];
  }

  return problem.detail || fallback;
}
