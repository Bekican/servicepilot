import type { Metadata } from "next";

import { AuthForm } from "@/features/auth/auth-form";

export const metadata: Metadata = { title: "Organizasyon oluştur" };

export default function RegisterPage() {
  return (
    <div className="bg-card w-full max-w-xl rounded-2xl border p-7 shadow-sm sm:p-9">
      <div className="mb-8 space-y-2">
        <p className="text-primary text-sm font-medium">
          ServicePilot’ı başlatın
        </p>
        <h2 className="text-3xl font-semibold tracking-tight">
          Organizasyonunuzu oluşturun
        </h2>
        <p className="text-muted-foreground text-sm leading-6">
          İlk hesap Owner rolüyle oluşturulur. Saat diliminiz otomatik önerilir.
        </p>
      </div>
      <AuthForm mode="register" />
    </div>
  );
}
