import type { Metadata } from "next";

import { AuthForm } from "@/features/auth/auth-form";

export const metadata: Metadata = { title: "Giriş" };

export default function LoginPage() {
  return (
    <div className="bg-card w-full max-w-md rounded-2xl border p-7 shadow-sm sm:p-9">
      <div className="mb-8 space-y-2">
        <p className="text-primary text-sm font-medium">Tekrar hoş geldiniz</p>
        <h2 className="text-3xl font-semibold tracking-tight">
          Hesabınıza giriş yapın
        </h2>
        <p className="text-muted-foreground text-sm leading-6">
          Organizasyon adresiniz ve kullanıcı bilgilerinizle devam edin.
        </p>
      </div>
      <AuthForm mode="login" />
    </div>
  );
}
