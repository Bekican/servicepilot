import type { Metadata } from "next";
import { connection } from "next/server";

import { Toaster } from "@/components/ui/sonner";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ServicePilot",
    template: "%s | ServicePilot",
  },
  description: "Teknik servis operasyon yönetimi",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  await connection();

  return (
    <html className="h-full antialiased" lang="tr">
      <body className="bg-background text-foreground min-h-full">
        {children}
        <Toaster richColors position="top-right" />
      </body>
    </html>
  );
}
