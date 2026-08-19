import type { KnowledgeCitation } from "@/lib/api/types";

export type KnowledgeChatMessage = {
  role: "user" | "assistant";
  content: string;
  insufficientEvidence?: boolean;
  citations?: KnowledgeCitation[];
};

export type KnowledgeConversationState = {
  messages: KnowledgeChatMessage[];
  error?: string;
  supportCode?: string;
};

export const initialKnowledgeConversationState: KnowledgeConversationState = {
  messages: [],
};
