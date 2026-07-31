import type { Metadata } from "next";

import { Toaster } from "@/components/ui/sonner";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "ServicePilot",
    template: "%s | ServicePilot",
  },
  description: "Teknik servis operasyon yönetimi",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html className="h-full antialiased" lang="tr">
      <body className="bg-background text-foreground min-h-full">
        {children}
        <Toaster richColors position="top-right" />
      </body>
    </html>
  );
}
