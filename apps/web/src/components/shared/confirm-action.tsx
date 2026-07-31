"use client";

import { useState, useTransition } from "react";
import { LoaderCircle } from "lucide-react";

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";

export function ConfirmAction({
  action,
  confirmLabel = "Onayla",
  description,
  disabled = false,
  pendingLabel = "İşleniyor…",
  title,
  triggerLabel,
  triggerSize = "default",
  triggerVariant = "destructive",
}: {
  action: () => Promise<void>;
  confirmLabel?: string;
  description: string;
  disabled?: boolean;
  pendingLabel?: string;
  title: string;
  triggerLabel: string;
  triggerSize?: React.ComponentProps<typeof Button>["size"];
  triggerVariant?: React.ComponentProps<typeof Button>["variant"];
}) {
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();

  function confirm() {
    startTransition(async () => {
      await action();
      setOpen(false);
    });
  }

  return (
    <AlertDialog onOpenChange={setOpen} open={open}>
      <AlertDialogTrigger asChild>
        <Button
          disabled={disabled}
          size={triggerSize}
          type="button"
          variant={triggerVariant}
        >
          {triggerLabel}
        </Button>
      </AlertDialogTrigger>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={pending}>Vazgeç</AlertDialogCancel>
          <AlertDialogAction
            disabled={pending}
            onClick={(event) => {
              event.preventDefault();
              confirm();
            }}
            variant="destructive"
          >
            {pending ? (
              <LoaderCircle className="animate-spin" aria-hidden="true" />
            ) : null}
            {pending ? pendingLabel : confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
