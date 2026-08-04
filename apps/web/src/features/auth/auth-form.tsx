"use client";

import { useActionState, useEffect, useRef } from "react";
import Link from "next/link";
import { useFormStatus } from "react-dom";
import { ArrowRight, LoaderCircle } from "lucide-react";

import { FormError } from "@/components/shared/form-error";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  acceptInvitationAction,
  loginAction,
  registerAction,
  type AuthActionState,
} from "@/lib/auth/actions";

type AuthMode = "login" | "register" | "invitation";

function SubmitButton({ mode }: { mode: AuthMode }) {
  const { pending } = useFormStatus();
  const labels: Record<AuthMode, string> = {
    login: "Giriş yap",
    register: "Organizasyonu oluştur",
    invitation: "Daveti kabul et",
  };

  return (
    <Button className="h-11 w-full" disabled={pending} type="submit">
      {pending ? <LoaderCircle className="animate-spin" /> : null}
      {labels[mode]}
      {!pending ? <ArrowRight /> : null}
    </Button>
  );
}

export function AuthForm({
  mode,
  invitationToken,
}: {
  mode: AuthMode;
  invitationToken?: string;
}) {
  const action =
    mode === "login"
      ? loginAction
      : mode === "register"
        ? registerAction
        : acceptInvitationAction;
  const [state, formAction] = useActionState<AuthActionState, FormData>(
    action,
    {},
  );
  const timeZoneInput = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (timeZoneInput.current) {
      timeZoneInput.current.value =
        Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
    }
  }, []);

  return (
    <form action={formAction} className="space-y-5">
      <FormError message={state.error} supportCode={state.supportCode} />

      {mode === "register" ? (
        <>
          <Field
            autoComplete="organization"
            label="Organizasyon adı"
            name="organizationName"
            placeholder="Örn. Atlas Teknik"
          />
          <Field
            autoCapitalize="none"
            label="Organizasyon adresi"
            name="organizationSlug"
            placeholder="atlas-teknik"
          />
        </>
      ) : null}

      {mode === "login" ? (
        <Field
          autoCapitalize="none"
          label="Organizasyon adresi"
          name="organizationSlug"
          placeholder="atlas-teknik"
        />
      ) : null}

      {mode !== "login" ? (
        <div className="grid gap-4 sm:grid-cols-2">
          <Field
            autoComplete="given-name"
            label="Ad"
            name="firstName"
            placeholder="Bekir"
          />
          <Field
            autoComplete="family-name"
            label="Soyad"
            name="lastName"
            placeholder="Çakmak"
          />
        </div>
      ) : null}

      {mode !== "invitation" ? (
        <Field
          autoCapitalize="none"
          autoComplete="email"
          label="E-posta"
          name="email"
          placeholder="owner@firma.com"
          type="email"
        />
      ) : null}

      <Field
        autoComplete={mode === "login" ? "current-password" : "new-password"}
        label="Parola"
        name="password"
        placeholder="En az 8 karakter"
        type="password"
      />

      {mode === "register" ? (
        <input
          defaultValue="UTC"
          name="timeZoneId"
          ref={timeZoneInput}
          type="hidden"
        />
      ) : null}
      {mode === "invitation" ? (
        <input name="token" type="hidden" value={invitationToken ?? ""} />
      ) : null}

      <SubmitButton mode={mode} />

      {mode === "login" ? (
        <p className="text-muted-foreground text-center text-sm">
          Henüz hesabınız yok mu?{" "}
          <Link
            className="text-primary font-medium hover:underline"
            href="/register"
          >
            Organizasyon oluşturun
          </Link>
        </p>
      ) : null}
      {mode === "register" ? (
        <p className="text-muted-foreground text-center text-sm">
          Zaten hesabınız var mı?{" "}
          <Link
            className="text-primary font-medium hover:underline"
            href="/login"
          >
            Giriş yapın
          </Link>
        </p>
      ) : null}
    </form>
  );
}

function Field({
  label,
  name,
  ...props
}: React.ComponentProps<typeof Input> & {
  label: string;
  name: string;
}) {
  return (
    <div className="space-y-2">
      <Label htmlFor={name}>{label}</Label>
      <Input className="h-11" id={name} name={name} required {...props} />
    </div>
  );
}
