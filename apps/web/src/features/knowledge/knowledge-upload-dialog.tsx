"use client";

import { useActionState, useState } from "react";
import { FileUp, LockKeyhole, Users } from "lucide-react";

import { FieldError } from "@/components/shared/field-error";
import { FormError } from "@/components/shared/form-error";
import { PendingButton } from "@/components/shared/pending-button";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  uploadKnowledgeDocumentAction,
  type KnowledgeUploadState,
} from "@/features/knowledge/actions";

const documentTypes = [
  ["TechnicalProcedure", "Teknik servis prosedürü"],
  ["Manual", "Cihaz / ürün kılavuzu"],
  ["CustomerServiceReport", "Müşteri servis raporu"],
  ["Warranty", "Garanti belgesi"],
  ["Other", "Diğer teknik belge"],
] as const;

const scopes = [
  ["Shared", "Tüm ekip", "Teknisyenler dahil tüm aktif kullanıcılar"],
  ["Operations", "Operasyon", "Yönetim ve planlama ekibi"],
  ["Management", "Yönetim", "Yalnızca Owner ve Admin kullanıcıları"],
] as const;

export function KnowledgeUploadDialog() {
  const [state, action] = useActionState(
    uploadKnowledgeDocumentAction,
    {} as KnowledgeUploadState,
  );
  const [documentType, setDocumentType] = useState("TechnicalProcedure");
  const [accessScope, setAccessScope] = useState("Shared");

  function changeDocumentType(value: string) {
    setDocumentType(value);
    if (value === "CustomerServiceReport" && accessScope === "Shared") {
      setAccessScope("Operations");
    }
  }

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button className="h-9">
          <FileUp />
          Belge yükle
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Bilgi kaynağı ekle</DialogTitle>
          <DialogDescription>
            Asistan bu PDF&apos;yi sayfa sayfa işler ve cevaplarında kaynak
            olarak gösterir.
          </DialogDescription>
        </DialogHeader>
        <form action={action} className="space-y-5">
          <FormError
            id="knowledge-upload-error"
            message={state.error}
            supportCode={state.supportCode}
          />
          <div className="space-y-2">
            <Label htmlFor="knowledge-file">PDF dosyası</Label>
            <Input
              accept="application/pdf,.pdf"
              id="knowledge-file"
              name="file"
              required
              type="file"
            />
            <p className="text-muted-foreground text-xs">
              En fazla 20 MB. Metin içeren ve taranmış PDF&apos;ler desteklenir.
            </p>
            <FieldError id="knowledge-file-error" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="knowledge-document-type">Belge türü</Label>
            <select
              className="bg-background h-10 w-full rounded-lg border px-3 text-sm"
              id="knowledge-document-type"
              name="documentType"
              onChange={(event) => changeDocumentType(event.target.value)}
              value={documentType}
            >
              {documentTypes.map(([value, label]) => (
                <option key={value} value={value}>
                  {label}
                </option>
              ))}
            </select>
          </div>
          <fieldset className="space-y-2">
            <legend className="text-sm font-medium">
              Kimler kullanabilir?
            </legend>
            <div className="grid gap-2">
              {scopes.map(([value, title, description]) => {
                const disabled =
                  documentType === "CustomerServiceReport" &&
                  value === "Shared";
                return (
                  <label
                    className="has-checked:border-foreground has-checked:bg-muted/60 flex cursor-pointer gap-3 rounded-lg border p-3 transition-colors has-disabled:cursor-not-allowed has-disabled:opacity-50"
                    key={value}
                  >
                    <input
                      checked={accessScope === value}
                      disabled={disabled}
                      name="accessScope"
                      onChange={() => setAccessScope(value)}
                      type="radio"
                      value={value}
                    />
                    {value === "Shared" ? (
                      <Users className="mt-0.5 size-4 shrink-0" />
                    ) : (
                      <LockKeyhole className="mt-0.5 size-4 shrink-0" />
                    )}
                    <span>
                      <span className="block text-sm font-medium">{title}</span>
                      <span className="text-muted-foreground block text-xs">
                        {description}
                      </span>
                    </span>
                  </label>
                );
              })}
            </div>
            {documentType === "CustomerServiceReport" ? (
              <p className="text-muted-foreground text-xs">
                Müşteri raporları teknisyenlere açık “Tüm ekip” kapsamında
                paylaşılmaz.
              </p>
            ) : null}
          </fieldset>
          <PendingButton
            className="h-10 w-full"
            pendingLabel="Belge yükleniyor…"
            type="submit"
          >
            <FileUp />
            Yükle ve işlemeyi başlat
          </PendingButton>
        </form>
      </DialogContent>
    </Dialog>
  );
}
