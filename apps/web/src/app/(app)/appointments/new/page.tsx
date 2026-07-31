import type { Metadata } from "next";

import { ActionMessage } from "@/components/shared/action-message";
import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AppointmentForm } from "@/features/appointments/appointment-form";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Customer, Service, Technician } from "@/lib/api/types";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Yeni Randevu" };

export default async function NewAppointmentPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const [customersResult, servicesResult, techniciansResult] =
    await Promise.all([
      client.GET("/api/customers"),
      client.GET("/api/services"),
      client.GET("/api/technicians"),
    ]);
  const customers = (customersResult.data ?? []).filter(
    (customer) => customer.isActive,
  ) as Customer[];
  const services = (servicesResult.data ?? []).filter(
    (service) => service.isActive,
  ) as Service[];
  const technicians = (techniciansResult.data ?? []) as Technician[];
  const ready = customers.length > 0 && services.length > 0;

  return (
    <>
      <PageHeader
        description="Müşteri, hizmet, zaman ve opsiyonel teknisyen seçimi."
        title="Yeni Randevu"
      />
      <ActionMessage error={query.error} />
      {ready ? (
        <Card className="max-w-2xl">
          <CardHeader>
            <CardTitle>Randevu bilgileri</CardTitle>
          </CardHeader>
          <CardContent>
            <AppointmentForm
              customers={customers}
              services={services}
              technicians={technicians}
              timeZone={session.timeZoneId}
            />
          </CardContent>
        </Card>
      ) : (
        <EmptyState
          description="Randevu oluşturmadan önce en az bir aktif müşteri ve hizmet gerekir."
          title="Ön koşullar eksik"
        />
      )}
    </>
  );
}
