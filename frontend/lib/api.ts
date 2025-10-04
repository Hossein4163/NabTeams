export type Role = 'participant' | 'judge' | 'mentor' | 'investor' | 'admin';

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

export interface SessionUserInfo {
  id?: string | null;
  email?: string | null;
  name?: string | null;
  roles?: string[] | null;
}

export interface AuthContext {
  accessToken?: string;
  sessionUser?: SessionUserInfo | null;
}

type FetchOptions = RequestInit & AuthContext;

async function apiFetch<T>(path: string, options: FetchOptions = {}): Promise<T> {
  const { accessToken, sessionUser, headers, ...init } = options;
  const combinedHeaders = new Headers(headers);
  combinedHeaders.set('Content-Type', combinedHeaders.get('Content-Type') ?? 'application/json');
  applyAuthHeaders(combinedHeaders, { accessToken, sessionUser });

  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: combinedHeaders,
    cache: 'no-store'
  });

  if (!response.ok) {
    const body = await safeReadJson(response);
    throw new Error((body as any)?.message ?? 'خطا در ارتباط با سرور');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function safeReadJson(response: Response) {
  try {
    return await response.json();
  } catch {
    return null;
  }
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

export type AppealStatus = 'Pending' | 'Accepted' | 'Rejected';

export interface Appeal {
  id: string;
  messageId: string;
  channel: Role;
  userId: string;
  submittedAt: string;
  reason: string;
  status: AppealStatus;
  resolutionNotes?: string | null;
  reviewedBy?: string | null;
  reviewedAt?: string | null;
}

export async function fetchMessages(role: Role, auth?: AuthContext): Promise<MessageModel[]> {
  const data = await apiFetch<{ messages: MessageModel[] }>(`/api/chat/${role}/messages`, auth);
  return data.messages ?? [];
}

export async function sendMessage(role: Role, content: string, auth?: AuthContext): Promise<SendMessageResponse> {
  const headers = new Headers({ 'Content-Type': 'application/json' });
  applyAuthHeaders(headers, auth);
  const response = await fetch(`${API_BASE}/api/chat/${role}/messages`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ content }),
    cache: 'no-store'
  });

  const body = await safeReadJson(response);
  if (response.ok || response.status === 202 || response.status === 403 || response.status === 429) {
    return body as SendMessageResponse;
  }

  throw new Error((body as any)?.message ?? 'ارسال پیام ناموفق بود');
}

export async function askSupport(payload: { role: Role; question: string }, auth?: AuthContext): Promise<SupportAnswer> {
  return apiFetch<SupportAnswer>(`/api/support/query`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export async function fetchKnowledgeBase(auth?: AuthContext): Promise<KnowledgeBaseItem[]> {
  return apiFetch<KnowledgeBaseItem[]>(`/api/knowledge-base`, auth);
}

export async function upsertKnowledgeBaseItem(
  item: Partial<KnowledgeBaseItem> & { title: string; body: string; audience?: string; tags?: string[] },
  auth?: AuthContext
): Promise<KnowledgeBaseItem> {
  return apiFetch<KnowledgeBaseItem>(`/api/knowledge-base`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(item)
  });
}

export async function deleteKnowledgeBaseItem(id: string, auth?: AuthContext): Promise<void> {
  await apiFetch<void>(`/api/knowledge-base/${id}`, {
    method: 'DELETE',
    ...auth
  });
}

export async function fetchAppeals(auth?: AuthContext): Promise<Appeal[]> {
  return apiFetch<Appeal[]>(`/api/appeals`, auth);
}

export async function createAppeal(payload: { messageId: string; reason: string }, auth?: AuthContext): Promise<Appeal> {
  return apiFetch<Appeal>(`/api/appeals`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export async function queryAppeals(
  auth: AuthContext | undefined,
  filters: { role?: Role; status?: AppealStatus } = {}
): Promise<Appeal[]> {
  const search = new URLSearchParams();
  if (filters.role) {
    search.set('role', filters.role);
  }
  if (filters.status) {
    search.set('status', filters.status);
  }
  const query = search.toString();
  return apiFetch<Appeal[]>(`/api/appeals/admin${query ? `?${query}` : ''}`, auth);
}

export async function resolveAppeal(
  id: string,
  payload: { status: AppealStatus; notes?: string },
  auth?: AuthContext
): Promise<Appeal> {
  return apiFetch<Appeal>(`/api/appeals/${id}/decision`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export async function fetchDiscipline(role: Role, auth?: AuthContext): Promise<any> {
  return apiFetch(`/api/discipline/${role}/me`, auth);
}

function applyAuthHeaders(headers: Headers, auth?: AuthContext) {
  if (!auth) {
    return;
  }

  if (auth.accessToken) {
    headers.set('Authorization', `Bearer ${auth.accessToken}`);
    return;
  }

  const user = auth.sessionUser;
  if (!user) {
    return;
  }

  const debugId = user.id ?? user.email ?? user.name ?? null;
  if (debugId) {
    headers.set('X-Debug-User', String(debugId));
  }

  if (user.email) {
    headers.set('X-Debug-Email', String(user.email));
  }

  const roles = Array.isArray(user.roles) ? user.roles.filter(Boolean) : [];
  if (roles.length > 0) {
    headers.set('X-Debug-Roles', roles.join(','));
  }
}
