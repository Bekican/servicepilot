import { redirect } from "next/navigation";

import { getSession, hasCapability } from "@/lib/auth/session";

export default async function HomePage() {
  const session = await getSession();

  if (!session) {
    redirect("/login");
  }

  redirect(
    hasCapability(session, "ViewDashboard") ? "/dashboard" : "/appointments",
  );
}
