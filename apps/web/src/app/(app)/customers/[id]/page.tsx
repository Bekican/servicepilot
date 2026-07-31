import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { MapPin } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  addAddressAction,
  deactivateAddressAction,
  deactivateCustomerAction,
  setPrimaryAddressAction,
  updateCustomerAction,
} from "@/features/customers/actions";
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
  searchParams: Promise<{ error?: string; success?: string }>;
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
          canWrite && customer.isActive ? (
            <form action={deactivateCustomerAction.bind(null, id)}>
              <Button type="submit" variant="destructive">
                Pasifleştir
              </Button>
            </form>
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
      <ActionMessage error={query.error} success={query.success} />

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
              {customer.addresses
                .filter((address) => address.isActive)
                .map((address) => (
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
                        </p>
                        <p className="text-muted-foreground mt-1 text-sm">
                          {address.line1}
                          {address.line2 ? `, ${address.line2}` : ""}
                          <br />
                          {address.postalCode} {address.city} /{" "}
                          {address.countryCode}
                        </p>
                        {canWrite ? (
                          <div className="mt-3 flex gap-2">
                            {!address.isPrimary ? (
                              <form
                                action={setPrimaryAddressAction.bind(
                                  null,
                                  id,
                                  address.id,
                                )}
                              >
                                <Button
                                  size="sm"
                                  type="submit"
                                  variant="outline"
                                >
                                  Birincil yap
                                </Button>
                              </form>
                            ) : null}
                            <form
                              action={deactivateAddressAction.bind(
                                null,
                                id,
                                address.id,
                              )}
                            >
                              <Button size="sm" type="submit" variant="ghost">
                                Pasifleştir
                              </Button>
                            </form>
                          </div>
                        ) : null}
                      </div>
                    </div>
                  </div>
                ))}
              {!customer.addresses.some((address) => address.isActive) ? (
                <p className="text-muted-foreground text-sm">
                  Aktif adres bulunmuyor.
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
                <form
                  action={addAddressAction.bind(null, id)}
                  className="space-y-4"
                >
                  <AddressField
                    label="Etiket"
                    name="label"
                    placeholder="Ev, İş..."
                  />
                  <AddressField label="Adres satırı" name="line1" required />
                  <AddressField label="Adres satırı 2" name="line2" />
                  <div className="grid grid-cols-2 gap-3">
                    <AddressField label="Şehir" name="city" required />
                    <AddressField label="Bölge" name="region" />
                    <AddressField label="Posta kodu" name="postalCode" />
                    <AddressField
                      defaultValue="TR"
                      label="Ülke kodu"
                      maxLength={2}
                      name="countryCode"
                      required
                    />
                  </div>
                  <label className="flex items-center gap-2 text-sm">
                    <input name="isPrimary" type="checkbox" />
                    Birincil adres yap
                  </label>
                  <Button type="submit">Adresi ekle</Button>
                </form>
              </CardContent>
            </Card>
          ) : null}
        </div>
      </div>
    </>
  );
}

function AddressField({
  label,
  name,
  ...props
}: React.ComponentProps<typeof Input> & { label: string; name: string }) {
  return (
    <div className="space-y-2">
      <Label htmlFor={name}>{label}</Label>
      <Input id={name} name={name} {...props} />
    </div>
  );
}
