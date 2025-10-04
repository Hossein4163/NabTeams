export type Role = 'participant' | 'judge' | 'mentor' | 'investor' | 'admin';

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export interface SendMessagePayload {
  userId: string;
  content: string;
}

export interface SendMessageResponse {
  messageId: string;
  status: 'Published' | 'Held' | 'Blocked';
  moderationRisk: number;
  moderationTags: string[];
  moderationNotes?: string | null;
  penaltyPoints: number;
  softWarn: boolean;
  rateLimitMessage?: string | null;
}

export interface MessageModel {
  id: string;
  channel: Role;
  senderUserId: string;
  content: string;
  createdAt: string;
  status: 'Published' | 'Held' | 'Blocked';
  moderationRisk: number;
  moderationTags: string[];
  moderationNotes?: string | null;
  penaltyPoints: number;
}

export interface SupportAnswer {
  answer: string;
  sources: string[];
  confidence: number;
  escalateToHuman: boolean;
}

export interface KnowledgeBaseItem {
  id: string;
  title: string;
  body: string;
  audience: string;
  tags: string[];
  updatedAt: string;
}

export async function fetchMessages(role: Role): Promise<MessageModel[]> {
  const res = await fetch(`${API_BASE}/api/chat/${role}/messages`, {
    cache: 'no-store'
  });
  if (!res.ok) {
    throw new Error('دریافت پیام‌ها با خطا مواجه شد');
  }
  const data = await res.json();
  return data.messages ?? [];
}

export async function sendMessage(role: Role, payload: SendMessagePayload): Promise<SendMessageResponse> {
  const res = await fetch(`${API_BASE}/api/chat/${role}/messages`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  const data = await res.json();
  if (!res.ok && res.status !== 202 && res.status !== 403 && res.status !== 429) {
    throw new Error(data?.message ?? 'ارسال پیام ناموفق بود');
  }
  return data as SendMessageResponse;
}

export async function askSupport(payload: { userId: string; role: Role; question: string }): Promise<SupportAnswer> {
  const res = await fetch(`${API_BASE}/api/support/query`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    throw new Error('دریافت پاسخ پشتیبانی ناموفق بود');
  }

  return res.json();
}

export async function fetchKnowledgeBase(): Promise<KnowledgeBaseItem[]> {
  const res = await fetch(`${API_BASE}/api/knowledge-base`, { cache: 'no-store' });
  if (!res.ok) {
    throw new Error('خواندن دانش‌پایه ناموفق بود');
  }
  return res.json();
}

export async function upsertKnowledgeBaseItem(item: Partial<KnowledgeBaseItem> & { title: string; body: string; audience?: string; tags?: string[] }): Promise<KnowledgeBaseItem> {
  const res = await fetch(`${API_BASE}/api/knowledge-base`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(item)
  });

  if (!res.ok) {
    throw new Error('ذخیره دانش‌پایه ناموفق بود');
  }

  return res.json();
}

export async function deleteKnowledgeBaseItem(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/api/knowledge-base/${id}`, {
    method: 'DELETE'
  });

  if (!res.ok) {
    throw new Error('حذف دانش‌پایه ناموفق بود');
  }
}
