import Link from "next/link";

import { Button } from "@/components/ui/button";

export default function NotFound() {
  return (
    <main className="grid min-h-screen place-items-center bg-[#f7f7fb] p-6 text-center">
      <div>
        <p className="text-primary text-sm font-semibold">404</p>
        <h1 className="mt-2 text-3xl font-semibold">Kayıt bulunamadı</h1>
        <p className="text-muted-foreground mt-3">
          Kaynak silinmiş, pasifleştirilmiş veya erişim alanınızın dışında
          olabilir.
        </p>
        <Button asChild className="mt-7">
          <Link href="/">Genel bakışa dön</Link>
        </Button>
      </div>
    </main>
  );
}
