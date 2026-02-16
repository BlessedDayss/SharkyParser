import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of } from 'rxjs';

export interface ChatMessage {
  role: 'user' | 'agent';
  text: string;
  timestamp: Date;
}

interface AgentResponse {
  response: string;
}

export interface AuthStatusResponse {
  authenticated: boolean;
  message: string;
}

export interface DeviceCodeResponse {
  userCode: string;
  verificationUri: string;
  expiresIn: number;
  interval: number;
}

export interface PollResponse {
  status: 'success' | 'pending' | 'expired' | 'denied' | 'error';
  message: string;
}

@Injectable({ providedIn: 'root' })
export class AiAgentService {
  private readonly http = inject(HttpClient);

  chat(message: string, logContext?: string): Observable<string> {
    return this.http
      .post<AgentResponse>('/api/agent/chat', { message, logContext })
      .pipe(map(res => res.response));
  }

  getAuthStatus(): Observable<AuthStatusResponse> {
    return this.http.get<AuthStatusResponse>('/api/agent/auth/status').pipe(
      catchError(() => of({ authenticated: false, message: 'Backend is not reachable.' }))
    );
  }

  startDeviceFlow(): Observable<DeviceCodeResponse> {
    return this.http.post<DeviceCodeResponse>('/api/agent/auth/device-code', {});
  }

  pollForToken(): Observable<PollResponse> {
    return this.http.post<PollResponse>('/api/agent/auth/poll', {});
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/agent/auth/logout', {});
  }
}
