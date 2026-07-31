"use client";

import { useRef, useState } from "react";

import { PendingButton } from "@/components/shared/pending-button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

import { changeUserRoleAction } from "./actions";

const roles = ["Owner", "Admin", "Dispatcher", "Technician"];

export function UserRoleForm({
  currentRole,
  disabled,
  userId,
}: {
  currentRole: string;
  disabled: boolean;
  userId: string;
}) {
  const formRef = useRef<HTMLFormElement>(null);
  const approved = useRef(false);
  const [selectedRole, setSelectedRole] = useState(currentRole);
  const [confirmOpen, setConfirmOpen] = useState(false);

  return (
    <>
      <form
        action={changeUserRoleAction.bind(null, userId)}
        className="flex gap-2"
        onSubmit={(event) => {
          const demotesOwner =
            currentRole === "Owner" && selectedRole !== "Owner";
          if (demotesOwner && !approved.current) {
            event.preventDefault();
            setConfirmOpen(true);
          }
          approved.current = false;
        }}
        ref={formRef}
      >
        <select
          aria-label="Kullanıcı rolü"
          className="bg-background h-9 w-full rounded-md border px-3 text-sm"
          disabled={disabled}
          name="role"
          onChange={(event) => setSelectedRole(event.target.value)}
          value={selectedRole}
        >
          {roles.map((role) => (
            <option key={role}>{role}</option>
          ))}
        </select>
        <PendingButton
          aria-label="Rolü kaydet"
          disabled={disabled || selectedRole === currentRole}
          pendingLabel="Kaydediliyor…"
          size="sm"
          type="submit"
          variant="outline"
        >
          Kaydet
        </PendingButton>
      </form>

      <AlertDialog onOpenChange={setConfirmOpen} open={confirmOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Owner rolü değiştirilsin mi?</AlertDialogTitle>
            <AlertDialogDescription>
              Bu kullanıcı organizasyon yönetim yetkilerini kaybedecek. Son
              aktif Owner kuralı sunucu tarafından ayrıca korunur.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Vazgeç</AlertDialogCancel>
            <AlertDialogAction
              onClick={(event) => {
                event.preventDefault();
                approved.current = true;
                setConfirmOpen(false);
                formRef.current?.requestSubmit();
              }}
              variant="destructive"
            >
              Rolü değiştir
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
