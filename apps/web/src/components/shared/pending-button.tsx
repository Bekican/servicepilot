"use client";

import { LoaderCircle } from "lucide-react";
import { useFormStatus } from "react-dom";

import { Button } from "@/components/ui/button";

export function PendingButton({
  children,
  disabled,
  pendingLabel = "Kaydediliyor…",
  ...props
}: React.ComponentProps<typeof Button> & { pendingLabel?: string }) {
  const { pending } = useFormStatus();

  return (
    <Button aria-busy={pending} disabled={disabled || pending} {...props}>
      {pending ? (
        <LoaderCircle className="animate-spin" aria-hidden="true" />
      ) : null}
      {pending ? pendingLabel : children}
    </Button>
  );
}
