import { PackageOpen } from "lucide-react";

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string;
  description: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="bg-card flex min-h-64 flex-col items-center justify-center rounded-xl border border-dashed px-6 text-center">
      <span className="bg-muted mb-4 grid size-11 place-items-center rounded-full">
        <PackageOpen className="text-muted-foreground size-5" />
      </span>
      <h2 className="font-semibold">{title}</h2>
      <p className="text-muted-foreground mt-1 max-w-md text-sm">
        {description}
      </p>
      {action ? <div className="mt-5">{action}</div> : null}
    </div>
  );
}
