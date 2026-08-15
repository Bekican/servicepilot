import { PasswordResetCompleteForm } from "@/features/auth/password-reset-form";

export default async function PasswordResetPage({
  searchParams,
}: {
  searchParams: Promise<{ token?: string }>;
}) {
  const { token } = await searchParams;
  return (
    <div className="bg-card w-full max-w-md rounded-2xl border p-7 shadow-sm sm:p-9">
      <h1 className="text-2xl font-semibold">Yeni parola belirleyin</h1>
      <p className="text-muted-foreground mt-2 mb-7 text-sm">
        En az 8 karakterli yeni parolanızı yazın.
      </p>
      {token ? (
        <PasswordResetCompleteForm token={token} />
      ) : (
        <p className="text-destructive">Parola yenileme bağlantısı geçersiz.</p>
      )}
    </div>
  );
}
