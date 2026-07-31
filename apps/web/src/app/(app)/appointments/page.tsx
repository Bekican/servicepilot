import type { Metadata } from "next";
import Link from "next/link";
import { Plus } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
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
import type { Appointment, Technician } from "@/lib/api/types";
import { formatDate, formatTime } from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Randevular" };

export default async function AppointmentsPage({
  searchParams,
}: {
  searchParams: Promise<{
    date?: string;
    status?: string;
    technicianId?: string;
  }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const canManage = session.capabilities.includes("ManageAppointments");
  const [appointmentsResult, techniciansResult] = await Promise.all([
    client.GET("/api/appointments", {
      params: {
        query: {
          status: query.status || undefined,
          technicianId: query.technicianId || undefined,
        },
      },
    }),
    canManage
      ? client.GET("/api/technicians")
      : Promise.resolve({ data: [] as Technician[] }),
  ]);
  let appointments = (appointmentsResult.data ?? []) as Appointment[];
  if (query.date) {
    appointments = appointments.filter(
      (appointment) =>
        new Intl.DateTimeFormat("en-CA", {
          timeZone: session.timeZoneId,
          year: "numeric",
          month: "2-digit",
          day: "2-digit",
        }).format(new Date(appointment.startAtUtc)) === query.date,
    );
  }
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
        <Input defaultValue={query.date} name="date" type="date" />
        <select
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
    </>
  );
}
