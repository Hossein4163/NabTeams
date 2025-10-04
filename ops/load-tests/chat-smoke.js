import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Trend } from 'k6/metrics';

export const options = {
  vus: Number(__ENV.VUS ?? 5),
  duration: __ENV.DURATION ?? '1m',
  thresholds: {
    http_req_duration: ['p(95)<800'],
    'checks{type:live}': ['rate>0.95'],
    'checks{type:chat}': ['rate>0.9']
  }
};

const baseUrl = __ENV.BASE_URL ?? 'http://localhost:5000';
const chatRole = __ENV.CHAT_ROLE ?? 'participant';
const debugUser = __ENV.DEBUG_USER ?? 'loadtest-user@nabteams.local';
const debugRoles = __ENV.DEBUG_ROLES ?? `${chatRole}`;
const bearerToken = __ENV.BEARER_TOKEN;

const chatLatency = new Trend('chat_latency');
const blockedMessages = new Counter('chat_blocked');

function buildHeaders() {
  const headers = {
    'Content-Type': 'application/json'
  };

  if (bearerToken) {
    headers.Authorization = `Bearer ${bearerToken}`;
    return headers;
  }

  headers['X-Debug-UserId'] = debugUser;
  headers['X-Debug-Email'] = debugUser;
  headers['X-Debug-Name'] = 'LoadTest User';
  headers['X-Debug-Roles'] = debugRoles;
  return headers;
}

export default function () {
  const live = http.get(`${baseUrl}/health/live`, { timeout: '5s' });
  check(live, {
    'live check ok': (res) => res.status === 200
  }, { type: 'live' });

  const payload = JSON.stringify({
    content: `پیام آزمایشی بارگذاری - ${Math.random().toString(16).slice(2)}`
  });

  const response = http.post(`${baseUrl}/api/chat/${chatRole}/messages`, payload, {
    headers: buildHeaders(),
    timeout: '10s'
  });

  chatLatency.add(response.timings.duration);

  let body;
  try {
    body = response.json();
  } catch (error) {
    body = {};
  }

  const accepted = response.status === 202 || response.status === 200;
  const limited = response.status === 429;
  const blocked = response.status === 202 && body.status === 'Blocked';

  if (blocked) {
    blockedMessages.add(1);
  }

  check(response, {
    'chat accepted or rate limited': () => accepted || limited,
    'chat payload <= limit': () => String(body?.moderationNotes ?? '').length < 1024
  }, { type: 'chat' });

  sleep(Number(__ENV.SLEEP ?? 1));
}
