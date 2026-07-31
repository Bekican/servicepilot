import type { Metadata } from "next";

import { ActionMessage } from "@/components/shared/action-message";
import { PageHeader } from "@/components/shared/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { CustomerForm } from "@/features/customers/customer-form";
import { createCustomerAction } from "@/features/customers/actions";

export const metadata: Metadata = { title: "Yeni müşteri" };

export default async function NewCustomerPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const { error } = await searchParams;

  return (
    <>
      <PageHeader
        description="İletişim bilgileri opsiyoneldir; verildiğinde tenant içinde benzersizdir."
        title="Yeni Müşteri"
      />
      <ActionMessage error={error} />
      <Card className="max-w-4xl">
        <CardContent>
          <CustomerForm
            action={createCustomerAction}
            submitLabel="Müşteriyi oluştur"
          />
        </CardContent>
      </Card>
    </>
  );
}
