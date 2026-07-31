import { AppShell } from "@/components/shared/app-shell";
import { requireSession } from "@/lib/auth/session";

export default async function AuthenticatedLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const session = await requireSession();
  return <AppShell session={session}>{children}</AppShell>;
}
