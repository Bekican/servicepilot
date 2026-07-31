"use client";

import { useActionState, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { CalendarClock } from "lucide-react";

import { FormError } from "@/components/shared/form-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { Customer, Service, Technician } from "@/lib/api/types";
import { zonedLocalDateTimeToIso } from "@/lib/date";
import { initialActionState } from "@/lib/action-state";

import { createAppointmentAction } from "./actions";

function customerName(customer: Customer) {
  return customer.type === "Company"
    ? customer.companyName
    : `${customer.firstName ?? ""} ${customer.lastName ?? ""}`.trim();
}

function initialDateTime() {
  const value = new Date(Date.now() + 60 * 60 * 1000);
  value.setMinutes(Math.ceil(value.getMinutes() / 15) * 15, 0, 0);
  const offset = value.getTimezoneOffset() * 60_000;
  return new Date(value.getTime() - offset).toISOString().slice(0, 16);
}

export function AppointmentForm({
  customers,
  services,
  technicians,
  timeZone,
}: {
  customers: Customer[];
  services: Service[];
  technicians: Technician[];
  timeZone: string;
}) {
  const [state, formAction] = useActionState(
    createAppointmentAction,
    initialActionState,
  );
  const router = useRouter();

  useEffect(() => {
    if (state.redirectTo) router.push(state.redirectTo);
  }, [router, state.redirectTo]);
  const [serviceId, setServiceId] = useState(services[0]?.id ?? "");
  const [startLocal, setStartLocal] = useState(initialDateTime);
  const service = services.find((item) => item.id === serviceId);
  const duration = Number(service?.defaultDurationMinutes ?? 60);
  const instants = useMemo(() => {
    if (!startLocal) return null;
    try {
      const startAt = zonedLocalDateTimeToIso(startLocal, timeZone);
      const endAt = new Date(
        new Date(startAt).getTime() + duration * 60_000,
      ).toISOString();
      return { startAt, endAt };
    } catch {
      return null;
    }
  }, [duration, startLocal, timeZone]);

  return (
    <form
      action={formAction}
      aria-describedby={state.error ? "appointment-form-error" : undefined}
      className="space-y-6"
      key={state.values ? JSON.stringify(state.values) : "initial"}
    >
      <FormError id="appointment-form-error" message={state.error} />
      <input name="startAt" type="hidden" value={instants?.startAt ?? ""} />
      <input name="endAt" type="hidden" value={instants?.endAt ?? ""} />

      <div className="space-y-2">
        <Label htmlFor="customerId">Müşteri</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          defaultValue={state.values?.customerId ?? customers[0]?.id}
          id="customerId"
          name="customerId"
          required
        >
          {customers.map((customer) => (
            <option key={customer.id} value={customer.id}>
              {customer.customerNumber} · {customerName(customer)}
            </option>
          ))}
        </select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="serviceId">Hizmet</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          defaultValue={state.values?.technicianUserId ?? ""}
          id="serviceId"
          name="serviceId"
          onChange={(event) => setServiceId(event.target.value)}
          required
          value={serviceId}
        >
          {services.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} · {Number(item.defaultDurationMinutes)} dakika
            </option>
          ))}
        </select>
      </div>

      <div className="space-y-2">
        <Label htmlFor="startLocal">Başlangıç</Label>
        <Input
          id="startLocal"
          min={new Date().toISOString().slice(0, 16)}
          onChange={(event) => setStartLocal(event.target.value)}
          required
          type="datetime-local"
          value={startLocal}
        />
        <p className="text-muted-foreground text-xs">
          Saat dilimi: {timeZone}. Bitiş, hizmet süresine göre otomatik
          hesaplanır.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="technicianUserId">Teknisyen (opsiyonel)</Label>
        <select
          className="bg-background h-10 w-full rounded-md border px-3 text-sm"
          id="technicianUserId"
          name="technicianUserId"
        >
          <option value="">Daha sonra ata</option>
          {technicians.map((technician) => (
            <option key={technician.id} value={technician.id}>
              {technician.firstName} {technician.lastName}
            </option>
          ))}
        </select>
      </div>

      <div className="bg-muted/40 rounded-lg border p-4 text-sm">
        <CalendarClock className="text-primary mr-2 inline size-4" />
        Tahmini süre: <strong>{duration} dakika</strong>
      </div>

      <PendingButton
        className="w-full"
        disabled={!customers.length || !services.length || !instants}
        pendingLabel="Oluşturuluyor…"
        type="submit"
      >
        Randevuyu oluştur
      </PendingButton>
    </form>
  );
}
