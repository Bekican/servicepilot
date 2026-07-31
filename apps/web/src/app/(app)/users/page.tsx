import type { Metadata } from "next";
import { MailPlus, RefreshCw, ShieldCheck } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { ConfirmAction } from "@/components/shared/confirm-action";
import { PageHeader } from "@/components/shared/page-header";
import { PendingButton } from "@/components/shared/pending-button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  changeUserStatusAction,
  resendInvitationAction,
} from "@/features/users/actions";
import { InvitationForm } from "@/features/users/invitation-form";
import { UserRoleForm } from "@/features/users/user-role-form";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Invitation, User } from "@/lib/api/types";
import { formatDate } from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Kullanıcılar" };

export default async function UsersPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string; success?: string }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const [usersResult, invitationsResult] = await Promise.all([
    client.GET("/api/users"),
    client.GET("/api/users/invitations"),
  ]);
  const users = (usersResult.data ?? []) as User[];
  const invitations = (invitationsResult.data ?? []) as Invitation[];

  return (
    <>
      <PageHeader
        description="Roller, kullanıcı durumu ve tek kullanımlık e-posta davetleri."
        title="Kullanıcılar"
      />
      <ActionMessage error={query.error} success={query.success} />

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1.4fr)_380px]">
        <Card className="overflow-hidden py-0">
          <CardHeader className="border-b py-5">
            <CardTitle>Aktif ve geçmiş kullanıcılar</CardTitle>
          </CardHeader>
          <CardContent className="divide-y p-0">
            {users.map((user) => (
              <div
                className="grid gap-4 px-6 py-5 md:grid-cols-[minmax(0,1fr)_180px_130px] md:items-center"
                key={user.id}
              >
                <div>
                  <p className="font-medium">
                    {user.firstName} {user.lastName}
                    {user.id === session.userId ? (
                      <span className="text-primary ml-2 text-xs">Siz</span>
                    ) : null}
                  </p>
                  <p className="text-muted-foreground text-sm">{user.email}</p>
                </div>
                <UserRoleForm
                  currentRole={user.role}
                  disabled={user.id === session.userId}
                  userId={user.id}
                />
                {user.isActive ? (
                  <ConfirmAction
                    action={changeUserStatusAction.bind(null, user.id, false)}
                    confirmLabel="Kullanıcıyı pasifleştir"
                    description="Kullanıcı giriş yapamayacak; geçmiş işlem ve audit kayıtları korunacak."
                    disabled={user.id === session.userId}
                    title="Kullanıcı pasifleştirilsin mi?"
                    triggerLabel="Pasifleştir"
                    triggerSize="sm"
                    triggerVariant="outline"
                  />
                ) : (
                  <form
                    action={changeUserStatusAction.bind(null, user.id, true)}
                  >
                    <PendingButton
                      className="w-full"
                      pendingLabel="İşleniyor…"
                      size="sm"
                      type="submit"
                    >
                      Aktifleştir
                    </PendingButton>
                  </form>
                )}
              </div>
            ))}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MailPlus className="text-primary size-5" />
                Kullanıcı davet et
              </CardTitle>
            </CardHeader>
            <CardContent>
              <InvitationForm />
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <ShieldCheck className="text-primary size-5" />
                Bekleyen davetler
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {invitations.map((invitation) => (
                <div className="rounded-lg border p-4" key={invitation.id}>
                  <p className="truncate font-medium">{invitation.email}</p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {invitation.role} ·{" "}
                    {formatDate(invitation.expiresAtUtc, session.timeZoneId)}{" "}
                    tarihine kadar
                  </p>
                  <form
                    action={resendInvitationAction.bind(null, invitation.id)}
                    className="mt-3"
                  >
                    <PendingButton
                      pendingLabel="Gönderiliyor…"
                      size="sm"
                      type="submit"
                      variant="outline"
                    >
                      <RefreshCw />
                      Yeniden gönder
                    </PendingButton>
                  </form>
                </div>
              ))}
              {!invitations.length ? (
                <p className="text-muted-foreground text-sm">
                  Bekleyen bir davet bulunmuyor.
                </p>
              ) : null}
            </CardContent>
          </Card>
        </div>
      </div>
    </>
  );
}
