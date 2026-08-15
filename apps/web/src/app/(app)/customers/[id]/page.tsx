import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { MapPin } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { ConfirmAction } from "@/components/shared/confirm-action";
import { PendingButton } from "@/components/shared/pending-button";
import { PageHeader } from "@/components/shared/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  activateAddressAction,
  activateCustomerAction,
  deactivateAddressAction,
  deactivateCustomerAction,
  setPrimaryAddressAction,
  updateCustomerAction,
} from "@/features/customers/actions";
import { AddressForm } from "@/features/customers/address-form";
import { CustomerForm } from "@/features/customers/customer-form";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Customer } from "@/lib/api/types";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Müşteri detayı" };

export default async function CustomerDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{
    error?: string;
    success?: string;
    supportCode?: string;
  }>;
}) {
  const { id } = await params;
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const { data, response } = await client.GET("/api/customers/{id}", {
    params: { path: { id } },
  });

  if (response.status === 404 || !data) notFound();
  const customer = data as Customer;
  const canWrite = session.capabilities.includes("ManageCustomers");

  return (
    <>
      <PageHeader
        action={
          canWrite ? (
            customer.isActive ? (
              <ConfirmAction
                action={deactivateCustomerAction.bind(null, id)}
                confirmLabel="Müşteriyi pasifleştir"
                description="Müşteri yeni işlemlerde kullanılamayacak; geçmiş randevu ve servis kayıtları korunacak."
                title="Müşteri pasifleştirilsin mi?"
                triggerLabel="Pasifleştir"
              />
            ) : (
              <form action={activateCustomerAction.bind(null, id)}>
                <PendingButton pendingLabel="Etkinleştiriliyor…" type="submit">
                  Yeniden etkinleştir
                </PendingButton>
              </form>
            )
          ) : null
        }
        description={`${customer.customerNumber} · ${
          customer.isActive ? "Aktif" : "Pasif"
        }`}
        title={
          customer.type === "Company"
            ? (customer.companyName ?? "Kurumsal müşteri")
            : `${customer.firstName ?? ""} ${customer.lastName ?? ""}`
        }
      />
      <ActionMessage
        error={query.error}
        success={query.success}
        supportCode={query.supportCode}
      />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.4fr)_minmax(340px,0.8fr)]">
        <Card>
          <CardHeader>
            <CardTitle>Müşteri bilgileri</CardTitle>
          </CardHeader>
          <CardContent>
            {canWrite && customer.isActive ? (
              <CustomerForm
                action={updateCustomerAction.bind(null, id)}
                customer={customer}
                submitLabel="Değişiklikleri kaydet"
              />
            ) : (
              <p className="text-muted-foreground text-sm">
                Bu müşteri kaydı salt okunur durumda.
              </p>
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Adresler</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {customer.addresses.map((address) => (
                <div className="rounded-lg border p-4" key={address.id}>
                  <div className="flex items-start gap-3">
                    <MapPin className="text-muted-foreground mt-0.5 size-4" />
                    <div className="min-w-0 flex-1">
                      <p className="font-medium">
                        {address.label ?? "Adres"}
                        {address.isPrimary ? (
                          <span className="text-primary ml-2 text-xs">
                            Birincil
                          </span>
                        ) : null}
                        {!address.isActive ? (
                          <span className="text-muted-foreground ml-2 text-xs">
                            Pasif
                          </span>
                        ) : null}
                      </p>
                      <p className="text-muted-foreground mt-1 text-sm">
                        {address.line1}
                        {address.line2 ? `, ${address.line2}` : ""}
                        <br />
                        {address.postalCode} {address.city} /{" "}
                        {address.countryCode}
                      </p>
                      {canWrite && customer.isActive ? (
                        <div className="mt-3 flex gap-2">
                          {address.isActive && !address.isPrimary ? (
                            <form
                              action={setPrimaryAddressAction.bind(
                                null,
                                id,
                                address.id,
                              )}
                            >
                              <PendingButton
                                pendingLabel="İşleniyor…"
                                size="sm"
                                type="submit"
                                variant="outline"
                              >
                                Birincil yap
                              </PendingButton>
                            </form>
                          ) : null}
                          {address.isActive ? (
                            <ConfirmAction
                              action={deactivateAddressAction.bind(
                                null,
                                id,
                                address.id,
                              )}
                              confirmLabel="Adresi pasifleştir"
                              description="Adres aktif müşteri adresleri arasından kaldırılacak."
                              title="Adres pasifleştirilsin mi?"
                              triggerLabel="Pasifleştir"
                              triggerSize="sm"
                              triggerVariant="ghost"
                            />
                          ) : (
                            <form
                              action={activateAddressAction.bind(
                                null,
                                id,
                                address.id,
                              )}
                            >
                              <PendingButton
                                pendingLabel="Etkinleştiriliyor…"
                                size="sm"
                                type="submit"
                                variant="outline"
                              >
                                Yeniden etkinleştir
                              </PendingButton>
                            </form>
                          )}
                        </div>
                      ) : null}
                    </div>
                  </div>
                </div>
              ))}
              {!customer.addresses.length ? (
                <p className="text-muted-foreground text-sm">
                  Henüz adres bulunmuyor.
                </p>
              ) : null}
            </CardContent>
          </Card>

          {canWrite && customer.isActive ? (
            <Card>
              <CardHeader>
                <CardTitle>Adres ekle</CardTitle>
              </CardHeader>
              <CardContent>
                <AddressForm customerId={id} />
              </CardContent>
            </Card>
          ) : null}
        </div>
      </div>
    </>
  );
}
