import type { Metadata } from "next";
import { Wrench } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { ConfirmAction } from "@/components/shared/confirm-action";
import { EmptyState } from "@/components/shared/empty-state";
import { PendingButton } from "@/components/shared/pending-button";
import { PageHeader } from "@/components/shared/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  createServiceAction,
  setServiceStatusAction,
  updateServiceAction,
} from "@/features/services/actions";
import { ServiceForm } from "@/features/services/service-form";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Service } from "@/lib/api/types";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Hizmetler" };

export default async function ServicesPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; success?: string }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const { data } = await client.GET("/api/services", {
    params: { query: { includeInactive: true } },
  });
  const services = (data ?? []) as Service[];
  const canWrite = session.capabilities.includes("ManageServices");

  return (
    <>
      <PageHeader
        description="Randevu süresini otomatik hesaplayan servis kataloğu."
        title="Hizmetler"
      />
      <ActionMessage error={query.error} success={query.success} />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_360px]">
        <div className="space-y-4">
          {services.map((service) => (
            <Card key={service.id}>
              <CardContent>
                <ServiceForm
                  action={updateServiceAction.bind(null, service.id)}
                  defaultDurationMinutes={Number(
                    service.defaultDurationMinutes,
                  )}
                  defaultName={service.name}
                  disabled={!canWrite || !service.isActive}
                  idSuffix={service.id}
                  mode="update"
                />
                <div className="mt-4 flex items-center justify-between border-t pt-4">
                  <span
                    className={
                      service.isActive
                        ? "text-sm text-emerald-700"
                        : "text-muted-foreground text-sm"
                    }
                  >
                    {service.isActive ? "Aktif hizmet" : "Pasif hizmet"}
                  </span>
                  {canWrite ? (
                    service.isActive ? (
                      <ConfirmAction
                        action={setServiceStatusAction.bind(
                          null,
                          service.id,
                          false,
                        )}
                        confirmLabel="Hizmeti pasifleştir"
                        description="Hizmet yeni randevularda seçilemeyecek; geçmiş kayıtlar korunacak."
                        title="Hizmet pasifleştirilsin mi?"
                        triggerLabel="Pasifleştir"
                        triggerSize="sm"
                        triggerVariant="ghost"
                      />
                    ) : (
                      <form
                        action={setServiceStatusAction.bind(
                          null,
                          service.id,
                          true,
                        )}
                      >
                        <PendingButton
                          pendingLabel="İşleniyor…"
                          size="sm"
                          type="submit"
                          variant="ghost"
                        >
                          Aktifleştir
                        </PendingButton>
                      </form>
                    )
                  ) : null}
                </div>
              </CardContent>
            </Card>
          ))}
          {!services.length ? (
            <EmptyState
              description="Randevularda kullanılacak ilk hizmeti oluşturun."
              title="Henüz hizmet yok"
            />
          ) : null}
        </div>

        {canWrite ? (
          <Card className="h-fit">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Wrench className="text-primary size-5" />
                Yeni hizmet
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ServiceForm action={createServiceAction} mode="create" />
            </CardContent>
          </Card>
        ) : null}
      </div>
    </>
  );
}
