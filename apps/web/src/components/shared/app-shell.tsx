"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  BellRing,
  CalendarDays,
  ChevronDown,
  Gauge,
  LogOut,
  Menu,
  PackageOpen,
  Users,
  UsersRound,
  Wrench,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import { logoutAction } from "@/lib/auth/actions";
import type { Session } from "@/lib/api/types";

const navigation = [
  {
    href: "/dashboard",
    label: "Genel Bakış",
    icon: Gauge,
    capability: "ViewDashboard",
  },
  {
    href: "/appointments",
    label: "Randevular",
    icon: CalendarDays,
    capability: "AccessSystem",
  },
  {
    href: "/customers",
    label: "Müşteriler",
    icon: UsersRound,
    capability: "AccessSystem",
  },
  {
    href: "/services",
    label: "Hizmetler",
    icon: Wrench,
    capability: "AccessSystem",
  },
  {
    href: "/reminders",
    label: "Hatırlatmalar",
    icon: BellRing,
    capability: "RetryReminders",
  },
  {
    href: "/users",
    label: "Kullanıcılar",
    icon: Users,
    capability: "ManageUsers",
  },
];

export function AppShell({
  session,
  children,
}: {
  session: Session;
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const visibleNavigation = navigation.filter((item) =>
    session.capabilities.includes(item.capability),
  );

  const navigationLinks = (closeOnNavigate = false) => (
    <nav className="space-y-1.5 px-3">
      {visibleNavigation.map((item) => {
        const active =
          pathname === item.href || pathname.startsWith(`${item.href}/`);
        const Icon = item.icon;

        const link = (
          <Link
            className={cn(
              "flex h-11 items-center gap-3 rounded-lg border-l-2 px-4 text-sm font-medium transition-colors",
              active
                ? "border-primary bg-white/6 text-white"
                : "border-transparent text-white/70 hover:bg-white/5 hover:text-white",
            )}
            href={item.href}
            key={item.href}
          >
            <Icon className="size-[18px]" />
            {item.label}
          </Link>
        );
        return closeOnNavigate ? (
          <SheetClose asChild key={item.href}>
            {link}
          </SheetClose>
        ) : (
          link
        );
      })}
    </nav>
  );

  const profile = (
    <div className="border-t border-white/10 p-4">
      <p className="truncate text-xs text-white/65">
        {session.organizationName}
      </p>
      <div className="mt-3 flex items-center gap-3">
        <div className="bg-primary grid size-9 shrink-0 place-items-center rounded-full text-sm font-semibold text-white">
          {session.firstName[0]}
          {session.lastName[0]}
        </div>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-white">
            {session.firstName} {session.lastName}
          </p>
          <p className="truncate text-xs text-white/65">{session.role}</p>
        </div>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-[#f7f7fb] lg:grid lg:grid-cols-[248px_minmax(0,1fr)]">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-[248px] flex-col bg-[#292d38] lg:flex">
        <div className="px-7 pt-8 pb-10">
          <Link
            className="text-[25px] font-semibold tracking-tight text-white"
            href="/"
          >
            ServicePilot
          </Link>
          <p className="mt-1 text-xs font-medium tracking-[0.12em] text-white/65">
            OPERASYON MERKEZİ
          </p>
        </div>
        <div className="flex-1">{navigationLinks()}</div>
        {profile}
      </aside>

      <div className="min-w-0 lg:col-start-2">
        <header className="bg-background/95 sticky top-0 z-30 flex h-16 items-center justify-between border-b px-4 backdrop-blur sm:px-6 lg:px-8">
          <div className="flex items-center gap-3 lg:hidden">
            <Sheet>
              <SheetTrigger asChild>
                <Button aria-label="Menüyü aç" size="icon" variant="outline">
                  <Menu />
                </Button>
              </SheetTrigger>
              <SheetContent
                className="w-[290px] border-0 bg-[#292d38] p-0"
                side="left"
              >
                <SheetTitle className="px-7 pt-7 pb-8 text-left text-xl text-white">
                  ServicePilot
                </SheetTitle>
                {navigationLinks(true)}
                <div className="absolute inset-x-0 bottom-0">{profile}</div>
              </SheetContent>
            </Sheet>
            <span className="font-semibold">ServicePilot</span>
          </div>

          <div className="hidden lg:block">
            <p className="text-sm font-medium">{session.organizationName}</p>
            <p className="text-muted-foreground text-xs">
              {session.timeZoneId}
            </p>
          </div>

          <div className="flex items-center gap-2">
            {session.capabilities.includes("ManageAppointments") ? (
              <Button asChild className="hidden sm:inline-flex">
                <Link href="/appointments/new">
                  <CalendarDays />
                  Yeni Randevu
                </Link>
              </Button>
            ) : null}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  aria-label={`Kullanıcı menüsü: ${session.firstName} ${session.lastName}`}
                  className="gap-2"
                  variant="ghost"
                >
                  <span className="hidden sm:inline">{session.firstName}</span>
                  <ChevronDown className="size-4" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-60">
                <DropdownMenuLabel>
                  <span className="block">
                    {session.firstName} {session.lastName}
                  </span>
                  <span className="text-muted-foreground block truncate text-xs font-normal">
                    {session.email}
                  </span>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem disabled>
                  <PackageOpen />
                  {session.organizationSlug}
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <form action={logoutAction}>
                  <DropdownMenuItem asChild>
                    <button className="w-full" type="submit">
                      <LogOut />
                      Çıkış yap
                    </button>
                  </DropdownMenuItem>
                </form>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>
        <main className="mx-auto w-full max-w-[1480px] p-4 sm:p-6 lg:p-8">
          {children}
        </main>
      </div>
    </div>
  );
}
