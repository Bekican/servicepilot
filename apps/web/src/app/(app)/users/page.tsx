import type { Metadata } from "next";
import { MailPlus, RefreshCw, ShieldCheck } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  changeUserRoleAction,
  changeUserStatusAction,
  createInvitationAction,
  resendInvitationAction,
} from "@/features/users/actions";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Invitation, User } from "@/lib/api/types";
import { formatDate } from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Kullanıcılar" };

const roles = ["Owner", "Admin", "Dispatcher", "Technician"];

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
                <form
                  action={changeUserRoleAction.bind(null, user.id)}
                  className="flex gap-2"
                >
                  <select
                    className="bg-background h-9 w-full rounded-md border px-3 text-sm"
                    defaultValue={user.role}
                    disabled={user.id === session.userId}
                    name="role"
                  >
                    {roles.map((role) => (
                      <option key={role}>{role}</option>
                    ))}
                  </select>
                  <Button
                    aria-label="Rolü kaydet"
                    disabled={user.id === session.userId}
                    size="sm"
                    type="submit"
                    variant="outline"
                  >
                    Kaydet
                  </Button>
                </form>
                <form
                  action={changeUserStatusAction.bind(
                    null,
                    user.id,
                    !user.isActive,
                  )}
                >
                  <Button
                    className="w-full"
                    disabled={user.id === session.userId}
                    size="sm"
                    type="submit"
                    variant={user.isActive ? "outline" : "default"}
                  >
                    {user.isActive ? "Pasifleştir" : "Aktifleştir"}
                  </Button>
                </form>
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
              <form action={createInvitationAction} className="space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="email">E-posta</Label>
                  <Input id="email" name="email" required type="email" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="role">Başlangıç rolü</Label>
                  <select
                    className="bg-background h-10 w-full rounded-md border px-3 text-sm"
                    defaultValue="Technician"
                    id="role"
                    name="role"
                  >
                    {roles.map((role) => (
                      <option key={role}>{role}</option>
                    ))}
                  </select>
                </div>
                <Button className="w-full" type="submit">
                  Davet gönder
                </Button>
              </form>
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
                    <Button size="sm" type="submit" variant="outline">
                      <RefreshCw />
                      Yeniden gönder
                    </Button>
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
