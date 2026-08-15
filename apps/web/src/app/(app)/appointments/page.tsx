import type { Metadata } from "next";
import Link from "next/link";
import { Plus } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
import { PaginationNav } from "@/components/shared/pagination-nav";
import { StatusBadge } from "@/components/shared/status-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { createServerApiClient } from "@/lib/api/server-client";
import type { AppointmentPage, Technician } from "@/lib/api/types";
import {
  dateKey,
  formatDate,
  formatTime,
  zonedLocalDateTimeToIso,
} from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Randevular" };

export default async function AppointmentsPage({
  searchParams,
}: {
  searchParams: Promise<{
    date?: string;
    status?: string;
    technicianId?: string;
    page?: string;
  }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const canManage = session.capabilities.includes("ManageAppointments");
  const page = Math.max(1, Number(query.page) || 1);
  const today = dateKey(new Date().toISOString(), session.timeZoneId);
  const from = query.date
    ? zonedLocalDateTimeToIso(`${query.date}T00:00`, session.timeZoneId)
    : zonedLocalDateTimeToIso(`${today}T00:00`, session.timeZoneId);
  const nextDate = query.date ? new Date(`${query.date}T12:00:00Z`) : null;
  nextDate?.setUTCDate(nextDate.getUTCDate() + 1);
  const dayAfter = nextDate
    ? zonedLocalDateTimeToIso(
        nextDate.toISOString().slice(0, 10) + "T00:00",
        session.timeZoneId,
      )
    : undefined;
  const [appointmentsResult, techniciansResult] = await Promise.all([
    client.GET("/api/appointments", {
      params: {
        query: {
          status: query.status || undefined,
          technicianId: query.technicianId || undefined,
          from,
          to: dayAfter,
          page,
          pageSize: 20,
        },
      },
    }),
    canManage
      ? client.GET("/api/technicians")
      : Promise.resolve({ data: [] as Technician[] }),
  ]);
  const appointmentPage = appointmentsResult.data as
    AppointmentPage | undefined;
  const appointments = appointmentPage?.items ?? [];
  const technicians = (techniciansResult.data ?? []) as Technician[];

  return (
    <>
      <PageHeader
        action={
          canManage ? (
            <Button asChild>
              <Link href="/appointments/new">
                <Plus />
                Yeni Randevu
              </Link>
            </Button>
          ) : null
        }
        description={`Operasyon takvimi · ${session.timeZoneId}`}
        title="Randevular"
      />

      <form className="bg-card mb-5 grid gap-3 rounded-xl border p-4 sm:grid-cols-3 lg:grid-cols-4">
        <Input
          aria-label="Randevu tarihi"
          defaultValue={query.date}
          name="date"
          type="date"
        />
        <select
          aria-label="Randevu durumu"
          className="bg-background h-10 rounded-md border px-3 text-sm"
          defaultValue={query.status ?? ""}
          name="status"
        >
          <option value="">Tüm durumlar</option>
          <option value="Scheduled">Planlandı</option>
          <option value="Confirmed">Onaylandı</option>
          <option value="InProgress">Devam ediyor</option>
          <option value="Completed">Tamamlandı</option>
          <option value="Cancelled">İptal edildi</option>
        </select>
        {canManage ? (
          <select
            aria-label="Teknisyen filtresi"
            className="bg-background h-10 rounded-md border px-3 text-sm"
            defaultValue={query.technicianId ?? ""}
            name="technicianId"
          >
            <option value="">Tüm teknisyenler</option>
            {technicians.map((technician) => (
              <option key={technician.id} value={technician.id}>
                {technician.firstName} {technician.lastName}
              </option>
            ))}
          </select>
        ) : null}
        <Button type="submit" variant="outline">
          Filtrele
        </Button>
      </form>

      {appointments.length ? (
        <Card className="overflow-hidden py-0">
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tarih / Saat</TableHead>
                  <TableHead>Müşteri</TableHead>
                  <TableHead>Hizmet</TableHead>
                  <TableHead>Teknisyen</TableHead>
                  <TableHead>Durum</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {appointments.map((appointment) => (
                  <TableRow key={appointment.id}>
                    <TableCell>
                      <Link
                        className="hover:text-primary font-medium"
                        href={`/appointments/${appointment.id}`}
                      >
                        {formatDate(appointment.startAtUtc, session.timeZoneId)}{" "}
                        {formatTime(appointment.startAtUtc, session.timeZoneId)}
                      </Link>
                    </TableCell>
                    <TableCell>{appointment.customerDisplayName}</TableCell>
                    <TableCell>{appointment.serviceName}</TableCell>
                    <TableCell>
                      {appointment.technicianDisplayName ?? "Atanmamış"}
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={appointment.status} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : (
        <EmptyState
          description="Seçilen filtrelere uygun randevu bulunamadı."
          title="Randevu bulunamadı"
        />
      )}
      <PaginationNav
        label="Randevu sayfaları"
        page={page}
        pathname="/appointments"
        query={query}
        totalPages={Number(appointmentPage?.totalPages ?? 0)}
      />
    </>
  );
}
