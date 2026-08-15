import Link from "next/link";
import { PasswordResetRequestForm } from "@/features/auth/password-reset-form";

export default function PasswordResetRequestPage() {
  return (
    <div className="bg-card w-full max-w-md rounded-2xl border p-7 shadow-sm sm:p-9">
      <h1 className="text-2xl font-semibold">Parolanızı yenileyin</h1>
      <p className="text-muted-foreground mt-2 mb-7 text-sm">
        Organizasyon kısa adınızı ve hesabınızın e-posta adresini yazın.
      </p>
      <PasswordResetRequestForm />
      <Link
        className="text-primary mt-5 block text-center text-sm"
        href="/login"
      >
        Girişe dön
      </Link>
    </div>
  );
}
