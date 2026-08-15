import type { Metadata } from "next";
import Link from "next/link";
import {
  AlertTriangle,
  BellRing,
  CalendarDays,
  CirclePlay,
  Plus,
  UserRoundPlus,
  UsersRound,
} from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
import { StatusBadge, statusLabel } from "@/components/shared/status-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { createServerApiClient } from "@/lib/api/server-client";
import type {
  AppointmentPage,
  DashboardSummary,
  ReminderPage,
} from "@/lib/api/types";
import {
  dateKey,
  formatDate,
  formatTime,
  zonedLocalDateTimeToIso,
} from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Genel Bakış" };

const asNumber = (value: number | string) => Number(value);

export default async function DashboardPage() {
  const session = await requireSession();
  const client = await createServerApiClient();
  const today = dateKey(new Date().toISOString(), session.timeZoneId);
  const tomorrow = new Date(`${today}T12:00:00Z`);
  tomorrow.setUTCDate(tomorrow.getUTCDate() + 1);
  const from = zonedLocalDateTimeToIso(`${today}T00:00`, session.timeZoneId);
  const to = zonedLocalDateTimeToIso(
    `${tomorrow.toISOString().slice(0, 10)}T00:00`,
    session.timeZoneId,
  );
  const [summaryResult, appointmentsResult, remindersResult] =
    await Promise.all([
      client.GET("/api/dashboard/summary"),
      client.GET("/api/appointments", {
        params: { query: { from, to, page: 1, pageSize: 100 } },
      }),
      client.GET("/api/reminders", {
        params: { query: { status: "Failed", page: 1, pageSize: 2 } },
      }),
    ]);

  const summary = summaryResult.data as DashboardSummary | undefined;

  if (!summary) {
    throw new Error("Dashboard özeti alınamadı.");
  }

  const appointmentsPage = appointmentsResult.data as
    AppointmentPage | undefined;
  const failedRemindersPage = remindersResult.data as ReminderPage | undefined;
  const appointments = appointmentsPage?.items ?? [];
  const failedReminders = failedRemindersPage?.items ?? [];

  const todayAppointments = appointments;
  const unassigned = todayAppointments.filter(
    (appointment) =>
      !appointment.technicianUserId &&
      appointment.status !== "Cancelled" &&
      appointment.status !== "Completed",
  );

  const statusCounts = [
    ["Scheduled", asNumber(summary.scheduledAppointmentCount)],
    ["Confirmed", asNumber(summary.confirmedAppointmentCount)],
    ["InProgress", asNumber(summary.inProgressAppointmentCount)],
    ["Completed", asNumber(summary.completedAppointmentCount)],
    ["Cancelled", asNumber(summary.cancelledAppointmentCount)],
  ] as const;
  const totalAppointments = statusCounts.reduce(
    (total, [, count]) => total + count,
    0,
  );

  return (
    <>
      <PageHeader
        action={
          session.capabilities.includes("ManageAppointments") ? (
            <Button asChild>
              <Link href="/appointments/new">
                <Plus />
                Yeni Randevu
              </Link>
            </Button>
          ) : null
        }
        description={`${formatDate(`${summary.date}T12:00:00Z`, "UTC")} · ${summary.timeZoneId}`}
        title="Genel Bakış"
      />

      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard
          accent="blue"
          icon={CalendarDays}
          label="Bugünkü randevular"
          value={totalAppointments}
        />
        <MetricCard
          accent="orange"
          icon={CirclePlay}
          label="Devam eden"
          value={asNumber(summary.inProgressAppointmentCount)}
        />
        <MetricCard
          accent="slate"
          icon={UsersRound}
          label="Aktif müşteriler"
          value={asNumber(summary.activeCustomerCount)}
        />
        <MetricCard
          accent="red"
          icon={BellRing}
          label="Başarısız hatırlatma"
          value={asNumber(summary.failedReminderCount)}
        />
      </section>

      <section className="mt-6 grid gap-6 xl:grid-cols-[minmax(0,2.15fr)_minmax(330px,1fr)]">
        <Card className="overflow-hidden py-0">
          <CardHeader className="border-b bg-[#f7f7ff] py-5">
            <CardTitle>Bugünün Randevuları</CardTitle>
          </CardHeader>
          <CardContent className="p-0">
            {todayAppointments.length ? (
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Saat</TableHead>
                      <TableHead>Müşteri</TableHead>
                      <TableHead>Hizmet</TableHead>
                      <TableHead>Teknisyen</TableHead>
                      <TableHead className="text-right">Durum</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {todayAppointments.slice(0, 7).map((appointment) => (
                      <TableRow key={appointment.id}>
                        <TableCell className="text-muted-foreground font-mono">
                          {formatTime(
                            appointment.startAtUtc,
                            summary.timeZoneId,
                          )}
                        </TableCell>
                        <TableCell>
                          <Link
                            className="hover:text-primary font-medium"
                            href={`/appointments/${appointment.id}`}
                          >
                            {appointment.customerDisplayName}
                          </Link>
                        </TableCell>
                        <TableCell>{appointment.serviceName}</TableCell>
                        <TableCell>
                          {appointment.technicianDisplayName ?? "Atanmamış"}
                        </TableCell>
                        <TableCell className="text-right">
                          <StatusBadge status={appointment.status} />
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            ) : (
              <EmptyState
                description="Bugün için planlanmış bir randevu bulunmuyor."
                title="Gününüz açık"
              />
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader className="border-b">
              <CardTitle className="flex items-center gap-2">
                <AlertTriangle className="size-5 text-red-600" />
                Dikkat Gerekenler
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {failedReminders.slice(0, 2).map((reminder) => (
                <AttentionItem
                  description={reminder.lastError ?? "Gönderim tamamlanamadı."}
                  href="/reminders"
                  key={reminder.id}
                  title="Hatırlatma gönderilemedi"
                  tone="red"
                />
              ))}
              {unassigned.slice(0, 2).map((appointment) => (
                <AttentionItem
                  description={`${formatTime(
                    appointment.startAtUtc,
                    summary.timeZoneId,
                  )} randevusu için teknisyen seçilmedi.`}
                  href={`/appointments/${appointment.id}`}
                  key={appointment.id}
                  title="Atanmamış randevu"
                  tone="orange"
                />
              ))}
              {!failedReminders.length && !unassigned.length ? (
                <p className="rounded-lg bg-emerald-50 p-4 text-sm text-emerald-800">
                  Her şey yolunda. Müdahale gerektiren bir işlem bulunmuyor.
                </p>
              ) : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="border-b">
              <CardTitle>Randevu Dağılımı</CardTitle>
            </CardHeader>
            <CardContent className="space-y-5">
              {statusCounts.map(([status, count]) => {
                const percentage = totalAppointments
                  ? Math.round((count / totalAppointments) * 100)
                  : 0;
                return (
                  <div className="space-y-2" key={status}>
                    <div className="flex justify-between text-sm">
                      <span>{statusLabel(status)}</span>
                      <span className="font-medium">{count}</span>
                    </div>
                    <div className="bg-muted h-2 rounded-full">
                      <div
                        className="bg-primary h-full rounded-full"
                        style={{ width: `${percentage}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-muted-foreground text-sm tracking-wider uppercase">
                Hızlı İşlemler
              </CardTitle>
            </CardHeader>
            <CardContent className="grid grid-cols-2 gap-3">
              <Button asChild className="h-20 flex-col gap-2" variant="outline">
                <Link href="/customers/new">
                  <UserRoundPlus />
                  Yeni Müşteri
                </Link>
              </Button>
              <Button asChild className="h-20 flex-col gap-2" variant="outline">
                <Link href="/appointments/new">
                  <CalendarDays />
                  Randevu Oluştur
                </Link>
              </Button>
            </CardContent>
          </Card>
        </div>
      </section>
    </>
  );
}

function MetricCard({
  label,
  value,
  icon: Icon,
  accent,
}: {
  label: string;
  value: number;
  icon: React.ComponentType<{ className?: string }>;
  accent: "blue" | "orange" | "red" | "slate";
}) {
  const colors = {
    blue: "text-blue-600 bg-blue-50",
    orange: "text-orange-700 bg-orange-50",
    red: "text-red-700 bg-red-50",
    slate: "text-slate-600 bg-slate-100",
  };

  return (
    <Card>
      <CardContent className="flex items-start justify-between">
        <div>
          <p className="text-muted-foreground text-xs font-medium tracking-[0.08em] uppercase">
            {label}
          </p>
          <p className="mt-4 text-4xl font-semibold tracking-tight">{value}</p>
        </div>
        <span
          className={`grid size-10 place-items-center rounded-lg ${colors[accent]}`}
        >
          <Icon className="size-5" />
        </span>
      </CardContent>
    </Card>
  );
}

function AttentionItem({
  title,
  description,
  href,
  tone,
}: {
  title: string;
  description: string;
  href: string;
  tone: "red" | "orange";
}) {
  return (
    <Link
      className={
        tone === "red"
          ? "block rounded-lg border border-red-200 bg-red-50/60 p-4 hover:bg-red-50"
          : "block rounded-lg border border-orange-200 bg-orange-50/70 p-4 hover:bg-orange-50"
      }
      href={href}
    >
      <p
        className={
          tone === "red"
            ? "font-semibold text-red-800"
            : "font-semibold text-orange-900"
        }
      >
        {title}
      </p>
      <p className="text-muted-foreground mt-1 line-clamp-2 text-sm">
        {description}
      </p>
    </Link>
  );
}
