"use client";

import { ShieldCheck, Trash2 } from "lucide-react";

import { PendingButton } from "@/components/shared/pending-button";
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import {
  changeKnowledgeDocumentScopeAction,
  deleteKnowledgeDocumentAction,
} from "@/features/knowledge/actions";

const scopes = [
  ["Shared", "Tüm ekip"],
  ["Operations", "Operasyon"],
  ["Management", "Yönetim"],
] as const;

export function KnowledgeDocumentActions({
  accessScope,
  documentId,
  documentName,
  documentType,
}: {
  accessScope: string;
  documentId: string;
  documentName: string;
  documentType: string;
}) {
  const availableScopes = scopes.filter(
    ([scope]) => documentType !== "CustomerServiceReport" || scope !== "Shared",
  );

  return (
    <details className="group mt-3 border-t pt-3">
      <summary className="text-muted-foreground hover:text-foreground cursor-pointer text-xs font-medium">
        Erişim ve belge ayarları
      </summary>
      <div className="mt-3 space-y-2">
        <form
          action={changeKnowledgeDocumentScopeAction.bind(null, documentId)}
          className="flex items-center gap-2"
        >
          <label className="sr-only" htmlFor={`scope-${documentId}`}>
            {documentName} için erişim kapsamı
          </label>
          <select
            className="bg-background h-8 min-w-0 flex-1 rounded-lg border px-2 text-xs"
            defaultValue={accessScope}
            id={`scope-${documentId}`}
            name="accessScope"
          >
            {availableScopes.map(([value, label]) => (
              <option key={value} value={value}>
                {label}
              </option>
            ))}
          </select>
          <PendingButton
            aria-label={`${documentName} erişim kapsamını kaydet`}
            pendingLabel="Kaydediliyor…"
            size="sm"
            type="submit"
            variant="outline"
          >
            <ShieldCheck />
            Kaydet
          </PendingButton>
        </form>

        <AlertDialog>
          <AlertDialogTrigger asChild>
            <Button className="w-full" size="sm" variant="ghost">
              <Trash2 />
              Belgeyi kaldır
            </Button>
          </AlertDialogTrigger>
          <AlertDialogContent>
            <AlertDialogHeader>
              <AlertDialogTitle>Belge kaldırılsın mı?</AlertDialogTitle>
              <AlertDialogDescription>
                “{documentName}” artık listede, arama sonuçlarında ve asistan
                yanıtlarında kullanılmayacak. Bu işlem geri alınamaz.
              </AlertDialogDescription>
            </AlertDialogHeader>
            <AlertDialogFooter>
              <AlertDialogCancel>Vazgeç</AlertDialogCancel>
              <form
                action={deleteKnowledgeDocumentAction.bind(null, documentId)}
              >
                <PendingButton
                  className="w-full"
                  pendingLabel="Kaldırılıyor…"
                  type="submit"
                  variant="destructive"
                >
                  Belgeyi kaldır
                </PendingButton>
              </form>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      </div>
    </details>
  );
}
