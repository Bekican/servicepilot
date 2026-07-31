import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CalendarClock, Clock3, UserRound, Wrench } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { ConfirmAction } from "@/components/shared/confirm-action";
import { PageHeader } from "@/components/shared/page-header";
import { PendingButton } from "@/components/shared/pending-button";
import { StatusBadge, statusLabel } from "@/components/shared/status-badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  assignTechnicianAction,
  transitionAppointmentAction,
} from "@/features/appointments/actions";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Appointment, Technician } from "@/lib/api/types";
import { formatDate, formatTime } from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Randevu Detayı" };

export default async function AppointmentDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ error?: string; success?: string }>;
}) {
  const { id } = await params;
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const result = await client.GET("/api/appointments/{id}", {
    params: { path: { id } },
  });
  if (!result.data) notFound();
  const appointment = result.data as Appointment;
  const techniciansResult = appointment.canAssignTechnician
    ? await client.GET("/api/technicians")
    : { data: [] as Technician[] };
  const technicians = (techniciansResult.data ?? []) as Technician[];

  return (
    <>
      <PageHeader
        action={<StatusBadge status={appointment.status} />}
        description={`${appointment.customerNumber} · ${session.timeZoneId}`}
        title={appointment.customerDisplayName}
      />
      <ActionMessage error={query.error} success={query.success} />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.4fr)_380px]">
        <Card>
          <CardHeader>
            <CardTitle>Randevu özeti</CardTitle>
          </CardHeader>
          <CardContent className="grid gap-5 sm:grid-cols-2">
            <Detail
              icon={CalendarClock}
              label="Tarih"
              value={formatDate(appointment.startAtUtc, session.timeZoneId)}
            />
            <Detail
              icon={Clock3}
              label="Saat"
              value={`${formatTime(
                appointment.startAtUtc,
                session.timeZoneId,
              )} – ${formatTime(appointment.endAtUtc, session.timeZoneId)}`}
            />
            <Detail
              icon={Wrench}
              label="Hizmet"
              value={appointment.serviceName}
            />
            <Detail
              icon={UserRound}
              label="Teknisyen"
              value={appointment.technicianDisplayName ?? "Henüz atanmadı"}
            />
          </CardContent>
        </Card>

        <div className="space-y-6">
          {appointment.canAssignTechnician ? (
            <Card>
              <CardHeader>
                <CardTitle>Teknisyen ata</CardTitle>
              </CardHeader>
              <CardContent>
                <form
                  action={assignTechnicianAction.bind(null, appointment.id)}
                  className="space-y-4"
                >
                  <label className="sr-only" htmlFor="technicianUserId">
                    Teknisyen
                  </label>
                  <select
                    className="bg-background h-10 w-full rounded-md border px-3 text-sm"
                    defaultValue={appointment.technicianUserId ?? ""}
                    id="technicianUserId"
                    name="technicianUserId"
                    required
                  >
                    <option disabled value="">
                      Teknisyen seçin
                    </option>
                    {technicians.map((technician) => (
                      <option key={technician.id} value={technician.id}>
                        {technician.firstName} {technician.lastName}
                      </option>
                    ))}
                  </select>
                  <PendingButton
                    className="w-full"
                    pendingLabel="Atanıyor…"
                    type="submit"
                    variant="outline"
                  >
                    Atamayı kaydet
                  </PendingButton>
                </form>
              </CardContent>
            </Card>
          ) : null}

          {appointment.allowedTransitions.length ? (
            <Card>
              <CardHeader>
                <CardTitle>Sonraki adım</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                {appointment.allowedTransitions.map((status) =>
                  status === "Cancelled" ? (
                    <ConfirmAction
                      action={transitionAppointmentAction.bind(
                        null,
                        appointment.id,
                        status,
                      )}
                      confirmLabel="Randevuyu iptal et"
                      description="Randevu terminal duruma geçecek ve teknisyen zaman aralığı yeniden kullanılabilir olacak."
                      key={status}
                      title="Randevu iptal edilsin mi?"
                      triggerLabel={statusLabel(status)}
                    />
                  ) : (
                    <form
                      action={transitionAppointmentAction.bind(
                        null,
                        appointment.id,
                        status,
                      )}
                      key={status}
                    >
                      <PendingButton
                        className="w-full"
                        pendingLabel="Güncelleniyor…"
                        type="submit"
                      >
                        {statusLabel(status)}
                      </PendingButton>
                    </form>
                  ),
                )}
              </CardContent>
            </Card>
          ) : null}
        </div>
      </div>
    </>
  );
}

function Detail({
  icon: Icon,
  label,
  value,
}: {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  value: string;
}) {
  return (
    <div className="rounded-xl border p-4">
      <Icon className="text-primary size-5" />
      <p className="text-muted-foreground mt-3 text-xs tracking-wider uppercase">
        {label}
      </p>
      <p className="mt-1 font-medium">{value}</p>
    </div>
  );
}
