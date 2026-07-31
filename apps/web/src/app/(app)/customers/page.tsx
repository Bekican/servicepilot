import type { Metadata } from "next";
import Link from "next/link";
import { Plus, Search } from "lucide-react";

import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
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
import type { Customer } from "@/lib/api/types";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Müşteriler" };

export default async function CustomersPage({
  searchParams,
}: {
  searchParams: Promise<{ search?: string; inactive?: string }>;
}) {
  const session = await requireSession();
  const query = await searchParams;
  const includeInactive = query.inactive === "true";
  const client = await createServerApiClient();
  const { data } = await client.GET("/api/customers", {
    params: { query: { includeInactive } },
  });
  const search = query.search?.trim().toLocaleLowerCase("tr-TR") ?? "";
  const customers = ((data ?? []) as Customer[]).filter((customer) => {
    const name =
      customer.type === "Company"
        ? customer.companyName
        : `${customer.firstName ?? ""} ${customer.lastName ?? ""}`;
    return `${customer.customerNumber} ${name} ${customer.email ?? ""}`
      .toLocaleLowerCase("tr-TR")
      .includes(search);
  });

  return (
    <>
      <PageHeader
        action={
          session.capabilities.includes("ManageCustomers") ? (
            <Button asChild>
              <Link href="/customers/new">
                <Plus />
                Yeni Müşteri
              </Link>
            </Button>
          ) : null
        }
        description="Bireysel ve kurumsal müşteri kayıtlarını yönetin."
        title="Müşteriler"
      />

      <form className="mb-5 flex max-w-xl gap-2">
        <div className="relative flex-1">
          <Search className="text-muted-foreground absolute top-1/2 left-3 size-4 -translate-y-1/2" />
          <Input
            aria-label="Müşteri ara"
            className="pl-9"
            defaultValue={query.search}
            name="search"
            placeholder="Numara, ad veya e-posta ara..."
          />
        </div>
        <Button type="submit" variant="outline">
          Ara
        </Button>
        <Button asChild variant="ghost">
          <Link
            href={includeInactive ? "/customers" : "/customers?inactive=true"}
          >
            {includeInactive ? "Aktifleri göster" : "Pasifleri dahil et"}
          </Link>
        </Button>
      </form>

      {customers.length ? (
        <Card className="overflow-hidden py-0">
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Numara</TableHead>
                  <TableHead>Müşteri</TableHead>
                  <TableHead>Tür</TableHead>
                  <TableHead>E-posta</TableHead>
                  <TableHead>Telefon</TableHead>
                  <TableHead>Durum</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {customers.map((customer) => (
                  <TableRow key={customer.id}>
                    <TableCell className="font-mono text-xs">
                      {customer.customerNumber}
                    </TableCell>
                    <TableCell>
                      <Link
                        className="hover:text-primary font-medium"
                        href={`/customers/${customer.id}`}
                      >
                        {customer.type === "Company"
                          ? customer.companyName
                          : `${customer.firstName} ${customer.lastName}`}
                      </Link>
                    </TableCell>
                    <TableCell>
                      {customer.type === "Company" ? "Kurumsal" : "Bireysel"}
                    </TableCell>
                    <TableCell>{customer.email ?? "—"}</TableCell>
                    <TableCell>{customer.phone ?? "—"}</TableCell>
                    <TableCell>
                      <span
                        className={
                          customer.isActive
                            ? "text-emerald-700"
                            : "text-muted-foreground"
                        }
                      >
                        {customer.isActive ? "Aktif" : "Pasif"}
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : (
        <EmptyState
          action={
            session.capabilities.includes("ManageCustomers") ? (
              <Button asChild>
                <Link href="/customers/new">İlk müşteriyi oluştur</Link>
              </Button>
            ) : null
          }
          description="Arama koşullarına uygun bir müşteri bulunamadı."
          title="Müşteri bulunamadı"
        />
      )}
    </>
  );
}
