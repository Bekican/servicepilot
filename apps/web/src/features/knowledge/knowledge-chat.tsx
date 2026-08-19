"use client";

import { useActionState, useEffect, useRef } from "react";
import { Bot, Send, Sparkles, User } from "lucide-react";
import { useFormStatus } from "react-dom";

import { FormError } from "@/components/shared/form-error";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { askKnowledgeAction } from "@/features/knowledge/actions";
import { KnowledgeSourceCard } from "@/features/knowledge/knowledge-source-card";
import {
  initialKnowledgeConversationState,
  type KnowledgeChatMessage,
} from "@/features/knowledge/types";
import { cn } from "@/lib/utils";

const suggestions = [
  "Bu arıza için kontrol sırası nedir?",
  "Bakım sırasında hangi güvenlik adımları uygulanmalı?",
  "Garanti kapsamı hangi durumlarda geçerlidir?",
];

export function KnowledgeChat({
  readyDocumentCount,
}: {
  readyDocumentCount: number;
}) {
  const [state, action, isPending] = useActionState(
    askKnowledgeAction,
    initialKnowledgeConversationState,
  );
  const formRef = useRef<HTMLFormElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const endRef = useRef<HTMLDivElement>(null);
  const previousMessageCount = useRef(0);

  useEffect(() => {
    if (state.messages.length > previousMessageCount.current) {
      formRef.current?.reset();
      endRef.current?.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
    previousMessageCount.current = state.messages.length;
  }, [state.messages.length]);

  function useSuggestion(question: string) {
    if (!textareaRef.current) return;
    textareaRef.current.value = question;
    textareaRef.current.focus();
  }

  return (
    <div className="flex min-h-[650px] flex-col">
      <div
        aria-live="polite"
        className="min-h-0 flex-1 space-y-5 overflow-y-auto px-5 py-6 sm:px-7"
      >
        {!state.messages.length ? (
          <Welcome
            readyDocumentCount={readyDocumentCount}
            onSelect={useSuggestion}
          />
        ) : (
          state.messages.map((message, index) => (
            <Message key={`${message.role}-${index}`} message={message} />
          ))
        )}
        <PendingAnswer pending={isPending} />
        <div ref={endRef} />
      </div>

      <div className="bg-background border-t p-4 sm:p-5">
        <FormError
          id="knowledge-chat-error"
          message={state.error}
          supportCode={state.supportCode}
        />
        <form action={action} className="mt-3" ref={formRef}>
          <div className="focus-within:border-ring focus-within:ring-ring/30 rounded-xl border bg-white p-2 shadow-sm focus-within:ring-3">
            <Textarea
              aria-describedby="knowledge-question-help"
              className="max-h-36 min-h-20 resize-none border-0 px-2 shadow-none focus-visible:ring-0"
              disabled={readyDocumentCount === 0}
              maxLength={2000}
              name="question"
              onKeyDown={(event) => {
                if (event.key === "Enter" && !event.shiftKey) {
                  event.preventDefault();
                  formRef.current?.requestSubmit();
                }
              }}
              placeholder={
                readyDocumentCount
                  ? "Teknik belgelerinize bir soru sorun…"
                  : "Soru sormak için önce hazır bir belge gerekiyor"
              }
              ref={textareaRef}
              required
            />
            <div className="flex items-center justify-between gap-3 px-2 pb-1">
              <p
                className="text-muted-foreground text-xs"
                id="knowledge-question-help"
              >
                Enter ile gönder · Shift+Enter ile yeni satır
              </p>
              <AskButton disabled={readyDocumentCount === 0} />
            </div>
          </div>
        </form>
        <p className="text-muted-foreground mt-2 text-center text-[11px]">
          Yanıtlar yalnızca şirketinizin erişebildiğiniz belgelerinden üretilir.
        </p>
      </div>
    </div>
  );
}

function Welcome({
  readyDocumentCount,
  onSelect,
}: {
  readyDocumentCount: number;
  onSelect: (question: string) => void;
}) {
  return (
    <div className="mx-auto flex max-w-xl flex-col items-center py-12 text-center">
      <span className="mb-5 grid size-14 place-items-center rounded-2xl bg-violet-100 text-violet-700">
        <Sparkles className="size-6" />
      </span>
      <h2 className="text-xl font-semibold">
        Teknik bilginiz, saniyeler içinde
      </h2>
      <p className="text-muted-foreground mt-2 max-w-md text-sm leading-6">
        Bir prosedürü, servis raporunu veya kılavuzu tek tek aramak yerine
        sorunuzu yazın. Yanıtın hangi belge ve sayfadan geldiğini birlikte
        görün.
      </p>
      {readyDocumentCount ? (
        <div className="mt-7 grid w-full gap-2 sm:grid-cols-3">
          {suggestions.map((question) => (
            <button
              className="hover:bg-muted rounded-xl border p-3 text-left text-xs leading-5 transition-colors"
              key={question}
              onClick={() => onSelect(question)}
              type="button"
            >
              {question}
            </button>
          ))}
        </div>
      ) : (
        <div className="mt-7 rounded-xl border border-dashed px-5 py-4 text-sm">
          Sağdaki <strong>Belge yükle</strong> düğmesiyle ilk PDF&apos;yi
          ekleyin.
        </div>
      )}
    </div>
  );
}

function Message({ message }: { message: KnowledgeChatMessage }) {
  const assistant = message.role === "assistant";
  return (
    <div className={cn("flex gap-3", !assistant && "justify-end")}>
      {assistant ? (
        <span className="grid size-8 shrink-0 place-items-center rounded-full bg-violet-100 text-violet-700">
          <Bot className="size-4" />
        </span>
      ) : null}
      <div className={cn("max-w-[88%]", !assistant && "order-first")}>
        <div
          className={cn(
            "rounded-2xl px-4 py-3 text-sm leading-6 whitespace-pre-wrap",
            assistant
              ? "rounded-tl-sm bg-slate-100 text-slate-900"
              : "bg-slate-900 text-white",
          )}
        >
          {message.content}
        </div>
        {assistant && message.citations?.length ? (
          <div className="mt-3 grid gap-2 sm:grid-cols-2">
            {message.citations.map((citation) => (
              <KnowledgeSourceCard
                citation={citation}
                key={citation.sourceId}
              />
            ))}
          </div>
        ) : null}
      </div>
      {!assistant ? (
        <span className="grid size-8 shrink-0 place-items-center rounded-full bg-slate-900 text-white">
          <User className="size-4" />
        </span>
      ) : null}
    </div>
  );
}

function AskButton({ disabled }: { disabled: boolean }) {
  const { pending } = useFormStatus();
  return (
    <Button
      aria-label="Soruyu gönder"
      disabled={disabled || pending}
      size="icon-lg"
      type="submit"
    >
      <Send className={pending ? "animate-pulse" : undefined} />
    </Button>
  );
}

function PendingAnswer({ pending }: { pending: boolean }) {
  if (!pending) return null;
  return (
    <div className="flex gap-3" role="status">
      <span className="grid size-8 shrink-0 place-items-center rounded-full bg-violet-100 text-violet-700">
        <Bot className="size-4" />
      </span>
      <div className="rounded-2xl rounded-tl-sm bg-slate-100 px-4 py-3 text-sm">
        <span className="flex items-center gap-1.5">
          <span className="size-1.5 animate-bounce rounded-full bg-slate-400" />
          <span className="size-1.5 animate-bounce rounded-full bg-slate-400 [animation-delay:120ms]" />
          <span className="size-1.5 animate-bounce rounded-full bg-slate-400 [animation-delay:240ms]" />
          <span className="text-muted-foreground ml-1">
            Belgelerde aranıyor
          </span>
        </span>
      </div>
    </div>
  );
}
