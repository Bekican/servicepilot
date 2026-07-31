import type { Metadata } from "next";

import { AuthForm } from "@/features/auth/auth-form";

export const metadata: Metadata = { title: "Daveti kabul et" };

export default async function AcceptInvitationPage({
  searchParams,
}: {
  searchParams: Promise<{ token?: string }>;
}) {
  const { token } = await searchParams;

  return (
    <div className="bg-card w-full max-w-md rounded-2xl border p-7 shadow-sm sm:p-9">
      <div className="mb-8 space-y-2">
        <p className="text-primary text-sm font-medium">Ekibe katılın</p>
        <h2 className="text-3xl font-semibold tracking-tight">
          Hesabınızı tamamlayın
        </h2>
        <p className="text-muted-foreground text-sm leading-6">
          Adınızı ve yalnızca sizin bildiğiniz parolayı belirleyin.
        </p>
      </div>
      {token ? (
        <AuthForm invitationToken={token} mode="invitation" />
      ) : (
        <p className="border-destructive/20 bg-destructive/5 text-destructive rounded-lg border p-4 text-sm">
          Davet bağlantısında token bulunamadı.
        </p>
      )}
    </div>
  );
}
