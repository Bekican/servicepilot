import { Wrench } from "lucide-react";

export default function PublicLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <main className="grid min-h-screen lg:grid-cols-[minmax(0,1fr)_minmax(440px,0.72fr)]">
      <section className="hidden bg-[#292d38] p-12 text-white lg:flex lg:flex-col lg:justify-between">
        <div className="flex items-center gap-3 text-2xl font-semibold">
          <span className="bg-primary grid size-10 place-items-center rounded-xl">
            <Wrench className="size-5" />
          </span>
          ServicePilot
        </div>
        <div className="max-w-xl space-y-5">
          <p className="text-sm font-medium tracking-[0.22em] text-blue-300 uppercase">
            Operasyon kontrol merkezi
          </p>
          <h1 className="text-5xl leading-tight font-semibold tracking-tight">
            Servis ekibinizin her günü tek ekranda.
          </h1>
          <p className="max-w-lg text-lg leading-8 text-white/65">
            Müşterileri, hizmetleri, teknisyenleri ve randevuları güvenli bir
            tenant yapısı içinde yönetin.
          </p>
        </div>
        <p className="text-sm text-white/45">ServicePilot MVP · 2026</p>
      </section>
      <section className="flex items-center justify-center bg-[#f7f7fb] px-5 py-12">
        {children}
      </section>
    </main>
  );
}
