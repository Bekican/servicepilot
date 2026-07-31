import type { Metadata } from "next";
import Link from "next/link";
import { RefreshCw } from "lucide-react";

import { ActionMessage } from "@/components/shared/action-message";
import { EmptyState } from "@/components/shared/empty-state";
import { PageHeader } from "@/components/shared/page-header";
import { PendingButton } from "@/components/shared/pending-button";
import { StatusBadge } from "@/components/shared/status-badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { retryReminderAction } from "@/features/reminders/actions";
import { createServerApiClient } from "@/lib/api/server-client";
import type { Reminder } from "@/lib/api/types";
import { formatDate, formatTime } from "@/lib/date";
import { requireSession } from "@/lib/auth/session";

export const metadata: Metadata = { title: "Hatırlatmalar" };

export default async function RemindersPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; error?: string; success?: string }>;
}) {
  const query = await searchParams;
  const session = await requireSession();
  const client = await createServerApiClient();
  const { data } = await client.GET("/api/reminders", {
    params: { query: { status: query.status } },
  });
  const reminders = (data ?? []) as Reminder[];

  return (
    <>
      <PageHeader
        action={
          <div className="flex gap-2">
            <Button asChild variant={!query.status ? "default" : "outline"}>
              <Link href="/reminders">Tümü</Link>
            </Button>
            <Button
              asChild
              variant={query.status === "Failed" ? "default" : "outline"}
            >
              <Link href="/reminders?status=Failed">Başarısız</Link>
            </Button>
          </div>
        }
        description="Worker gönderimleri, denemeler ve kullanıcıya görünür hatalar."
        title="Hatırlatmalar"
      />
      <ActionMessage error={query.error} success={query.success} />

      {reminders.length ? (
        <Card className="overflow-hidden py-0">
          <CardContent className="p-0">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Planlanan zaman</TableHead>
                  <TableHead>Randevu</TableHead>
                  <TableHead>Durum</TableHead>
                  <TableHead>Deneme</TableHead>
                  <TableHead>Son hata</TableHead>
                  <TableHead>
                    <span className="sr-only">İşlemler</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {reminders.map((reminder) => (
                  <TableRow key={reminder.id}>
                    <TableCell>
                      {formatDate(reminder.scheduledAtUtc, session.timeZoneId)}{" "}
                      {formatTime(reminder.scheduledAtUtc, session.timeZoneId)}
                    </TableCell>
                    <TableCell>
                      <Link
                        className="hover:text-primary font-medium"
                        href={`/appointments/${reminder.appointmentId}`}
                      >
                        Detaya git
                      </Link>
                    </TableCell>
                    <TableCell>
                      <StatusBadge status={reminder.status} />
                    </TableCell>
                    <TableCell>{Number(reminder.attemptCount)}</TableCell>
                    <TableCell className="text-muted-foreground max-w-sm truncate">
                      {reminder.lastError ?? "—"}
                    </TableCell>
                    <TableCell className="text-right">
                      {reminder.status === "Failed" ? (
                        <form
                          action={retryReminderAction.bind(null, reminder.id)}
                        >
                          <PendingButton
                            pendingLabel="Kuyruğa alınıyor…"
                            size="sm"
                            type="submit"
                            variant="outline"
                          >
                            <RefreshCw />
                            Tekrar dene
                          </PendingButton>
                        </form>
                      ) : null}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : (
        <EmptyState
          description="Seçilen durumda bir hatırlatma kaydı bulunmuyor."
          title="Hatırlatma bulunamadı"
        />
      )}
    </>
  );
}
