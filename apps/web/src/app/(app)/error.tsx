"use client";

import { useEffect } from "react";
import { AlertTriangle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";

export default function AppError({
  error,
  unstable_retry,
}: {
  error: Error & { digest?: string };
  unstable_retry: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <Card className="mx-auto mt-16 max-w-lg">
      <CardContent className="flex flex-col items-center py-10 text-center">
        <span className="grid size-12 place-items-center rounded-full bg-red-50 text-red-700">
          <AlertTriangle />
        </span>
        <h1 className="mt-5 text-xl font-semibold">Ekran yüklenemedi</h1>
        <p className="text-muted-foreground mt-2 text-sm">
          API bağlantısı veya beklenmeyen bir işlem hatası oluştu.
        </p>
        <Button className="mt-6" onClick={() => unstable_retry()}>
          Tekrar dene
        </Button>
      </CardContent>
    </Card>
  );
}
