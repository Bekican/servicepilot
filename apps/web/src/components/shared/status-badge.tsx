import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

const statusLabels: Record<string, string> = {
  Scheduled: "Planlandı",
  Confirmed: "Onaylandı",
  InProgress: "Devam ediyor",
  Completed: "Tamamlandı",
  Cancelled: "İptal edildi",
  Pending: "Bekliyor",
  Processing: "İşleniyor",
  Sent: "Gönderildi",
  Failed: "Başarısız",
  Skipped: "Atlandı",
};

const statusStyles: Record<string, string> = {
  Scheduled: "border-slate-200 bg-slate-100 text-slate-700",
  Confirmed: "border-blue-200 bg-blue-50 text-blue-700",
  InProgress: "border-orange-200 bg-orange-50 text-orange-800",
  Completed: "border-emerald-200 bg-emerald-50 text-emerald-700",
  Cancelled: "border-red-200 bg-red-50 text-red-700",
  Pending: "border-slate-200 bg-slate-100 text-slate-700",
  Processing: "border-orange-200 bg-orange-50 text-orange-800",
  Sent: "border-emerald-200 bg-emerald-50 text-emerald-700",
  Failed: "border-red-200 bg-red-50 text-red-700",
  Skipped: "border-slate-200 bg-slate-100 text-slate-600",
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <Badge
      className={cn(
        "rounded-md font-medium",
        statusStyles[status] ?? "bg-muted text-muted-foreground",
      )}
      variant="outline"
    >
      {statusLabels[status] ?? status}
    </Badge>
  );
}

export function statusLabel(status: string) {
  return statusLabels[status] ?? status;
}
