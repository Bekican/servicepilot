import type { ProblemDetails } from "@/lib/api/types";
import { isValidCorrelationId } from "@/lib/observability/correlation-id";

const translatedMessages: Record<string, string> = {
  "Request.ValidationFailed": "Gönderilen bilgileri kontrol edin.",
  "Authentication.Required": "Devam etmek için tekrar giriş yapın.",
  "Authorization.Forbidden": "Bu işlem için yetkiniz bulunmuyor.",
  "Http.NotFound": "İstenen kayıt veya sayfa bulunamadı.",
  "Http.RateLimitExceeded":
    "Çok fazla istek gönderildi. Lütfen biraz sonra tekrar deneyin.",
  "System.Unexpected": "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
  "System.UnmappedError": "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
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

const translatedFieldErrors: Record<string, string> = {
  Required: "Bu alan zorunludur.",
  Invalid: "Bu alan geçerli değil.",
  InvalidEmail: "Geçerli bir e-posta adresi yazın.",
  InvalidPhone: "0555… veya +90555… biçiminde bir telefon yazın.",
  InvalidSlug: "Yalnız küçük harf, rakam ve isteğe bağlı tire kullanın.",
  TooShort: "Girilen değer çok kısa.",
  TooLong: "Girilen değer çok uzun.",
};

function fieldErrors(problem: ProblemDetails | undefined) {
  if (!problem?.errors) return undefined;

  return Object.fromEntries(
    Object.entries(problem.errors).flatMap(([field, codes]) => {
      const code = codes[0];
      return code
        ? [[field, translatedFieldErrors[code] ?? "Bu alanı kontrol edin."]]
        : [];
    }),
  );
}

export function problemMessage(
  problem: ProblemDetails | undefined,
  fallback = "İşlem tamamlanamadı. Lütfen tekrar deneyin.",
) {
  if (!problem) {
    return fallback;
  }

  const code = problem.code ?? problem.title;

  if (code && translatedMessages[code]) {
    return translatedMessages[code];
  }

  return problem.detail || fallback;
}

export function problemPresentation(
  problem: ProblemDetails | undefined,
  fallback?: string,
) {
  return {
    message: problemMessage(problem, fallback),
    supportCode: isValidCorrelationId(problem?.correlationId)
      ? problem.correlationId
      : undefined,
  };
}

export function problemActionState(
  problem: ProblemDetails | undefined,
  fallback?: string,
) {
  const presentation = problemPresentation(problem, fallback);
  return {
    error: problem?.errors ? undefined : presentation.message,
    fieldErrors: fieldErrors(problem),
    supportCode: presentation.supportCode,
  };
}

export function problemSearchParams(
  problem: ProblemDetails | undefined,
  fallback?: string,
) {
  const presentation = problemPresentation(problem, fallback);
  const params = new URLSearchParams({ error: presentation.message });
  if (presentation.supportCode) {
    params.set("supportCode", presentation.supportCode);
  }
  return params.toString();
}
