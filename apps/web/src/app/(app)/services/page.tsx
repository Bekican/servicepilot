import type { Metadata } from "next";
import { Clock3, Wrench } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  createServiceAction,
  setServiceStatusAction,
  updateServiceAction,
} from "@/features/services/actions";
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
                <form
                  action={updateServiceAction.bind(null, service.id)}
                  className="grid items-end gap-4 sm:grid-cols-[minmax(0,1fr)_180px_auto]"
                >
                  <div className="space-y-2">
                    <Label htmlFor={`name-${service.id}`}>Hizmet adı</Label>
                    <Input
                      defaultValue={service.name}
                      disabled={!canWrite || !service.isActive}
                      id={`name-${service.id}`}
                      name="name"
                      required
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor={`duration-${service.id}`}>
                      Süre (dakika)
                    </Label>
                    <Input
                      defaultValue={Number(service.defaultDurationMinutes)}
                      disabled={!canWrite || !service.isActive}
                      id={`duration-${service.id}`}
                      min={5}
                      name="defaultDurationMinutes"
                      required
                      type="number"
                    />
                  </div>
                  {canWrite ? (
                    <Button
                      disabled={!service.isActive}
                      type="submit"
                      variant="outline"
                    >
                      Kaydet
                    </Button>
                  ) : null}
                </form>
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
                    <form
                      action={setServiceStatusAction.bind(
                        null,
                        service.id,
                        !service.isActive,
                      )}
                    >
                      <Button size="sm" type="submit" variant="ghost">
                        {service.isActive ? "Pasifleştir" : "Aktifleştir"}
                      </Button>
                    </form>
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
              <form action={createServiceAction} className="space-y-5">
                <div className="space-y-2">
                  <Label htmlFor="name">Hizmet adı</Label>
                  <Input
                    id="name"
                    name="name"
                    placeholder="Kombi Bakımı"
                    required
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="defaultDurationMinutes">
                    Varsayılan süre
                  </Label>
                  <div className="relative">
                    <Clock3 className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                    <Input
                      className="pl-9"
                      defaultValue={60}
                      id="defaultDurationMinutes"
                      min={5}
                      name="defaultDurationMinutes"
                      required
                      type="number"
                    />
                  </div>
                </div>
                <Button className="w-full" type="submit">
                  Hizmeti oluştur
                </Button>
              </form>
            </CardContent>
          </Card>
        ) : null}
      </div>
    </>
  );
}
