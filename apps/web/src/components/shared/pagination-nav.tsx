import Link from "next/link";

import { Button } from "@/components/ui/button";

type Query = Record<string, string | undefined>;

export function PaginationNav({
  label,
  page,
  pathname,
  query,
  totalPages,
}: {
  label: string;
  page: number;
  pathname: string;
  query: Query;
  totalPages: number;
}) {
  if (totalPages <= 1) return null;

  return (
    <nav aria-label={label} className="mt-5 flex items-center justify-between">
      {page > 1 ? (
        <Button asChild variant="outline">
          <Link
            href={{ pathname, query: { ...query, page: String(page - 1) } }}
          >
            Önceki
          </Link>
        </Button>
      ) : (
        <Button disabled variant="outline">
          Önceki
        </Button>
      )}
      <span className="text-muted-foreground text-sm">
        Sayfa {page} / {totalPages}
      </span>
      {page < totalPages ? (
        <Button asChild variant="outline">
          <Link
            href={{ pathname, query: { ...query, page: String(page + 1) } }}
          >
            Sonraki
          </Link>
        </Button>
      ) : (
        <Button disabled variant="outline">
          Sonraki
        </Button>
      )}
    </nav>
  );
}
