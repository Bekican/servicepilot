import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { Customer } from "@/lib/api/types";

export function CustomerForm({
  action,
  customer,
  submitLabel,
}: {
  action: (formData: FormData) => void | Promise<void>;
  customer?: Customer;
  submitLabel: string;
}) {
  return (
    <form action={action} className="space-y-7">
      <div className="space-y-2">
        <Label htmlFor="type">Müşteri türü</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          defaultValue={customer?.type ?? "Individual"}
          id="type"
          name="type"
        >
          <option value="Individual">Bireysel</option>
          <option value="Company">Kurumsal</option>
        </select>
        <p className="text-muted-foreground text-xs">
          Bireysel müşteri için ad/soyad, kurumsal müşteri için şirket adı
          zorunludur.
        </p>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        <FormField
          defaultValue={customer?.firstName ?? ""}
          label="Ad"
          name="firstName"
        />
        <FormField
          defaultValue={customer?.lastName ?? ""}
          label="Soyad"
          name="lastName"
        />
        <FormField
          defaultValue={customer?.companyName ?? ""}
          label="Şirket adı"
          name="companyName"
        />
        <FormField
          defaultValue={customer?.contactPerson ?? ""}
          label="İletişim kişisi"
          name="contactPerson"
        />
        <FormField
          defaultValue={customer?.email ?? ""}
          label="E-posta"
          name="email"
          type="email"
        />
        <FormField
          defaultValue={customer?.phone ?? ""}
          label="Telefon"
          name="phone"
          placeholder="+905551112233"
        />
      </div>

      <div className="flex justify-end">
        <Button type="submit">{submitLabel}</Button>
      </div>
    </form>
  );
}

function FormField({
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
