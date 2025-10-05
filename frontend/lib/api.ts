export type Role = 'participant' | 'judge' | 'mentor' | 'investor' | 'admin';

export const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';
export const MAX_MESSAGE_LENGTH = 2000;
export const MAX_SUPPORT_QUESTION_LENGTH = 1500;

export type RegistrationStatus =
  | 'Submitted'
  | 'Finalized'
  | 'Approved'
  | 'PaymentRequested'
  | 'PaymentCompleted'
  | 'Rejected'
  | 'Cancelled';

export type RegistrationPaymentStatus = 'Pending' | 'Completed' | 'Failed' | 'Cancelled';

export type NotificationChannel = 'Email' | 'Sms';

export type BusinessPlanReviewStatus = 'Pending' | 'Completed' | 'Failed';

export type IntegrationProviderType = 'Gemini' | 'PaymentGateway' | 'Sms' | 'Email';
export type OperationsChecklistStatus = 'Pending' | 'InProgress' | 'Completed';
export type AuditLogAction = string;

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
  const trimmed = content.trim();
  if (!trimmed) {
    throw new Error('متن پیام نمی‌تواند خالی باشد.');
  }
  if (trimmed.length > MAX_MESSAGE_LENGTH) {
    throw new Error(`حداکثر طول پیام ${MAX_MESSAGE_LENGTH} نویسه است.`);
  }

  const headers = new Headers({ 'Content-Type': 'application/json' });
  applyAuthHeaders(headers, auth);
  const response = await fetch(`${API_BASE}/api/chat/${role}/messages`, {
    method: 'POST',
    headers,
    body: JSON.stringify({ content: trimmed }),
    cache: 'no-store'
  });

  const body = await safeReadJson(response);
  if (response.ok || response.status === 202 || response.status === 403 || response.status === 429) {
    return body as SendMessageResponse;
  }

  throw new Error((body as any)?.message ?? 'ارسال پیام ناموفق بود');
}

export async function askSupport(payload: { role: Role; question: string }, auth?: AuthContext): Promise<SupportAnswer> {
  const trimmedQuestion = payload.question.trim();
  if (!trimmedQuestion) {
    throw new Error('سوال نمی‌تواند خالی باشد.');
  }
  if (trimmedQuestion.length > MAX_SUPPORT_QUESTION_LENGTH) {
    throw new Error(`حداکثر طول سوال ${MAX_SUPPORT_QUESTION_LENGTH} نویسه است.`);
  }

  return apiFetch<SupportAnswer>(`/api/support/query`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify({ role: payload.role, question: trimmedQuestion })
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

export type RegistrationDocumentCategory =
  | 'ProjectArchive'
  | 'TeamResume'
  | 'Presentation'
  | 'BusinessModel'
  | 'Other';

export type RegistrationLinkType = 'LinkedIn' | 'GitHub' | 'Website' | 'Demo' | 'Other';

export interface ParticipantTeamMemberInput {
  fullName: string;
  role: string;
  focusArea: string;
}

export interface ParticipantDocumentInput {
  category: RegistrationDocumentCategory;
  fileName: string;
  fileUrl: string;
}

export interface ParticipantLinkInput {
  type: RegistrationLinkType;
  label?: string | null;
  url: string;
}

export interface ParticipantRegistrationPayload {
  headFirstName: string;
  headLastName: string;
  nationalId: string;
  phoneNumber: string;
  email?: string | null;
  birthDate?: string | null;
  educationDegree: string;
  fieldOfStudy: string;
  teamName: string;
  hasTeam: boolean;
  teamCompleted: boolean;
  additionalNotes?: string | null;
  members: ParticipantTeamMemberInput[];
  documents: ParticipantDocumentInput[];
  links: ParticipantLinkInput[];
}

export interface ParticipantRegistrationResponse {
  id: string;
  headFirstName: string;
  headLastName: string;
  nationalId: string;
  phoneNumber: string;
  email: string | null;
  birthDate: string | null;
  educationDegree: string;
  fieldOfStudy: string;
  teamName: string;
  hasTeam: boolean;
  submittedAt: string;
  teamCompleted: boolean;
  additionalNotes: string | null;
  status: RegistrationStatus;
  finalizedAt: string | null;
  summaryFileUrl: string | null;
  members: Array<ParticipantTeamMemberInput & { id: string }>;
  documents: Array<ParticipantDocumentInput & { id: string }>;
  links: Array<ParticipantLinkInput & { id: string; label: string }>;
  payment: ParticipantPayment | null;
  notifications: ParticipantNotification[];
  businessPlanReviews: BusinessPlanReview[];
}

export interface ParticipantPayment {
  id: string;
  amount: number;
  currency: string;
  paymentUrl: string;
  status: RegistrationPaymentStatus;
  requestedAt: string;
  completedAt: string | null;
  gatewayReference: string | null;
}

export interface ParticipantNotification {
  id: string;
  channel: NotificationChannel;
  recipient: string;
  subject: string;
  message: string;
  sentAt: string;
}

export interface BusinessPlanReview {
  id: string;
  status: BusinessPlanReviewStatus;
  overallScore: number | null;
  summary: string;
  strengths: string;
  risks: string;
  recommendations: string;
  sourceDocumentUrl: string | null;
  model: string;
  createdAt: string;
}

export async function submitParticipantRegistration(
  payload: ParticipantRegistrationPayload,
  auth?: AuthContext
): Promise<ParticipantRegistrationResponse> {
  return apiFetch<ParticipantRegistrationResponse>(`/api/registrations/participants`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export async function updateParticipantRegistration(
  id: string,
  payload: ParticipantRegistrationPayload,
  auth?: AuthContext
): Promise<ParticipantRegistrationResponse> {
  return apiFetch<ParticipantRegistrationResponse>(`/api/registrations/participants/${id}`, {
    method: 'PUT',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export async function finalizeParticipantRegistration(
  id: string,
  summaryFileUrl?: string | null,
  auth?: AuthContext
): Promise<ParticipantRegistrationResponse> {
  return apiFetch<ParticipantRegistrationResponse>(`/api/registrations/participants/${id}/finalize`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify({ summaryFileUrl: summaryFileUrl ?? null })
  });
}

export async function getParticipantRegistration(
  id: string,
  auth?: AuthContext
): Promise<ParticipantRegistrationResponse> {
  return apiFetch<ParticipantRegistrationResponse>(`/api/registrations/participants/${id}/public`, {
    method: 'GET',
    ...auth
  });
}

export interface ParticipantApprovalRequest {
  amount: number;
  currency?: string;
  recipient: string;
  returnUrl: string;
}

export async function approveParticipantRegistration(
  id: string,
  payload: ParticipantApprovalRequest,
  auth?: AuthContext
): Promise<ParticipantRegistrationResponse> {
  return apiFetch<ParticipantRegistrationResponse>(`/api/registrations/participants/${id}/approve`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export interface IntegrationSetting {
  id: string;
  type: IntegrationProviderType;
  providerKey: string;
  displayName: string;
  isActive: boolean;
  configuration: string;
  createdAt: string;
  updatedAt: string;
}

export interface IntegrationSettingUpsertPayload {
  id?: string;
  type: IntegrationProviderType;
  providerKey: string;
  displayName?: string | null;
  configuration: string;
  activate?: boolean;
}

export async function listIntegrationSettings(
  auth?: AuthContext,
  type?: IntegrationProviderType
): Promise<IntegrationSetting[]> {
  const query = type ? `?type=${encodeURIComponent(type)}` : '';
  return apiFetch<IntegrationSetting[]>(`/api/admin/integrations${query}`, auth);
}

export async function upsertIntegrationSetting(
  payload: IntegrationSettingUpsertPayload,
  auth?: AuthContext
): Promise<IntegrationSetting> {
  return apiFetch<IntegrationSetting>(`/api/admin/integrations`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export async function activateIntegrationSetting(id: string, auth?: AuthContext): Promise<void> {
  await apiFetch<void>(`/api/admin/integrations/${id}/activate`, {
    method: 'POST',
    ...auth
  });
}

export async function deleteIntegrationSetting(id: string, auth?: AuthContext): Promise<void> {
  await apiFetch<void>(`/api/admin/integrations/${id}`, {
    method: 'DELETE',
    ...auth
  });
}

export interface AuditLogRecord {
  id: string;
  actorId: string;
  actorName: string;
  action: AuditLogAction;
  entityType: string;
  entityId: string;
  createdAt: string;
  metadata?: unknown;
}

export interface AuditLogFilters {
  entityType?: string;
  entityId?: string;
  skip?: number;
  take?: number;
}

export async function listAuditLogs(
  auth?: AuthContext,
  filters: AuditLogFilters = {}
): Promise<AuditLogRecord[]> {
  const params = new URLSearchParams();
  if (filters.entityType) {
    params.set('entityType', filters.entityType);
  }
  if (filters.entityId) {
    params.set('entityId', filters.entityId);
  }
  if (typeof filters.skip === 'number') {
    params.set('skip', String(filters.skip));
  }
  if (typeof filters.take === 'number') {
    params.set('take', String(filters.take));
  }

  const query = params.toString() ? `?${params.toString()}` : '';
  return apiFetch<AuditLogRecord[]>(`/api/admin/audit-logs${query}`, auth);
}

export interface OperationsChecklistItem {
  id: string;
  key: string;
  title: string;
  description: string;
  category: string;
  status: OperationsChecklistStatus;
  completedAt?: string | null;
  notes?: string | null;
  artifactUrl?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface OperationsChecklistUpdatePayload {
  status: OperationsChecklistStatus;
  notes?: string;
  artifactUrl?: string;
}

export async function listOperationsChecklist(auth?: AuthContext): Promise<OperationsChecklistItem[]> {
  return apiFetch<OperationsChecklistItem[]>(`/api/admin/operations-checklist`, auth);
}

export async function updateOperationsChecklistItem(
  id: string,
  payload: OperationsChecklistUpdatePayload,
  auth?: AuthContext
): Promise<OperationsChecklistItem> {
  return apiFetch<OperationsChecklistItem>(`/api/admin/operations-checklist/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload),
    ...(auth ?? {})
  });
}

export interface ParticipantPaymentCompletionRequest {
  gatewayReference?: string | null;
}

export async function completeParticipantPayment(
  id: string,
  payload: ParticipantPaymentCompletionRequest,
  auth?: AuthContext
): Promise<ParticipantRegistrationResponse> {
  return apiFetch<ParticipantRegistrationResponse>(`/api/registrations/participants/${id}/payments/complete`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export interface BusinessPlanAnalysisPayload {
  narrative: string;
  additionalContext?: string | null;
  attachmentUrls?: string[];
}

export async function requestBusinessPlanAnalysis(
  id: string,
  payload: BusinessPlanAnalysisPayload,
  auth?: AuthContext
): Promise<BusinessPlanReview> {
  const body = {
    narrative: payload.narrative,
    additionalContext: payload.additionalContext ?? null,
    attachmentUrls: payload.attachmentUrls ?? []
  };

  return apiFetch<BusinessPlanReview>(`/api/registrations/participants/${id}/analysis`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(body)
  });
}

export async function fetchBusinessPlanReviews(
  id: string,
  auth?: AuthContext
): Promise<BusinessPlanReview[]> {
  return apiFetch<BusinessPlanReview[]>(`/api/registrations/participants/${id}/analysis`, {
    method: 'GET',
    ...auth
  });
}

export interface ParticipantDocumentUploadResponse {
  category: RegistrationDocumentCategory;
  fileName: string;
  fileUrl: string;
}

export async function uploadParticipantDocument(
  file: File,
  category: RegistrationDocumentCategory,
  auth?: AuthContext
): Promise<ParticipantDocumentUploadResponse> {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('category', category);

  const headers = new Headers();
  applyAuthHeaders(headers, auth);
  headers.delete('Content-Type');

  const response = await fetch(`${API_BASE}/api/registrations/participants/uploads`, {
    method: 'POST',
    headers,
    body: formData,
    cache: 'no-store'
  });

  if (!response.ok) {
    const body = await safeReadJson(response);
    throw new Error((body as any)?.message ?? 'آپلود فایل ناموفق بود');
  }

  return (await response.json()) as ParticipantDocumentUploadResponse;
}

export interface JudgeRegistrationPayload {
  firstName: string;
  lastName: string;
  nationalId: string;
  phoneNumber: string;
  email?: string | null;
  birthDate?: string | null;
  fieldOfExpertise: string;
  highestDegree: string;
  biography?: string | null;
}

export interface JudgeRegistrationResponse {
  id: string;
  firstName: string;
  lastName: string;
  submittedAt: string;
  status: RegistrationStatus;
  finalizedAt: string | null;
}

export async function submitJudgeRegistration(
  payload: JudgeRegistrationPayload,
  auth?: AuthContext
): Promise<JudgeRegistrationResponse> {
  return apiFetch<JudgeRegistrationResponse>(`/api/registrations/judges`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
}

export interface InvestorRegistrationPayload {
  firstName: string;
  lastName: string;
  nationalId: string;
  phoneNumber: string;
  email?: string | null;
  additionalNotes?: string | null;
  interestAreas: string[];
}

export interface InvestorRegistrationResponse {
  id: string;
  firstName: string;
  lastName: string;
  submittedAt: string;
  interestAreas: string[];
  status: RegistrationStatus;
  finalizedAt: string | null;
}

export async function submitInvestorRegistration(
  payload: InvestorRegistrationPayload,
  auth?: AuthContext
): Promise<InvestorRegistrationResponse> {
  return apiFetch<InvestorRegistrationResponse>(`/api/registrations/investors`, {
    method: 'POST',
    ...auth,
    body: JSON.stringify(payload)
  });
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

export function buildDebugQuery(auth?: AuthContext): URLSearchParams {
  const params = new URLSearchParams();
  if (!auth || auth.accessToken) {
    return params;
  }

  const user = auth.sessionUser;
  if (!user) {
    return params;
  }

  const debugId = user.id ?? user.email ?? user.name ?? null;
  if (debugId) {
    params.set('debug_user', String(debugId));
  }

  if (user.email) {
    params.set('debug_email', String(user.email));
  }

  const roles = Array.isArray(user.roles) ? user.roles.filter(Boolean) : [];
  if (roles.length > 0) {
    params.set('debug_roles', roles.join(','));
  }

  return params;
}
