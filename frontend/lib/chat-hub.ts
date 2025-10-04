import { HubConnection, HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import { API_BASE, AuthContext, MessageModel, Role, buildDebugQuery } from './api';

export type MessageListener = (message: MessageModel) => void;

export async function createChatConnection(
  role: Role,
  auth: AuthContext,
  listener: MessageListener
): Promise<HubConnection> {
  const baseUrl = API_BASE.endsWith('/') ? API_BASE.slice(0, -1) : API_BASE;
  const url = new URL(`${baseUrl}/hubs/chat`);
  url.searchParams.set('role', role);

  const debugParams = buildDebugQuery(auth);
  debugParams.forEach((value, key) => {
    url.searchParams.set(key, value);
  });

  const connection = new HubConnectionBuilder()
    .withUrl(url.toString(), {
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      accessTokenFactory: auth.accessToken ? () => auth.accessToken! : undefined,
      withCredentials: true
    })
    .withAutomaticReconnect({ nextRetryDelayInMilliseconds: () => 2000 })
    .configureLogging(LogLevel.Warning)
    .build();

  connection.on('MessageUpserted', (message: MessageModel) => {
    listener(message);
  });

  await connection.start();
  return connection;
}
