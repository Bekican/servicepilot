"use client";

import { useState } from "react";
import { Check, Copy } from "lucide-react";

import { Button } from "@/components/ui/button";
import { isValidCorrelationId } from "@/lib/observability/correlation-id";

export function SupportCode({ value }: { value?: string }) {
  const [copied, setCopied] = useState(false);
  const safeValue = isValidCorrelationId(value) ? value : undefined;

  if (!safeValue) return null;

  async function copy() {
    await navigator.clipboard.writeText(safeValue!);
    setCopied(true);
  }

  return (
    <span className="mt-1 flex items-center gap-2 text-xs">
      <span>Destek kodu: {safeValue}</span>
      <Button
        aria-label="Destek kodunu kopyala"
        className="h-6 gap-1 px-2 text-xs"
        onClick={copy}
        size="sm"
        type="button"
        variant="ghost"
      >
        {copied ? <Check aria-hidden="true" /> : <Copy aria-hidden="true" />}
        {copied ? "Kopyalandı" : "Kopyala"}
      </Button>
    </span>
  );
}
