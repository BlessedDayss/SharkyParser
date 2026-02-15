import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, catchError, of, delay, switchMap } from 'rxjs';

export interface ChatMessage {
  role: 'user' | 'agent';
  text: string;
  timestamp: Date;
}

interface AgentResponse {
  response: string;
}

@Injectable({ providedIn: 'root' })
export class AiAgentService {
  private http = inject(HttpClient);

  chat(message: string, logContext?: string): Observable<string> {
    return this.http
      .post<AgentResponse>('/api/agent/chat', { message, logContext })
      .pipe(
        map(res => res.response),
        catchError(() =>
          of('').pipe(
            delay(1500),
            switchMap(() =>
              of('AI Agent is not yet connected to the backend. Make sure the Copilot SDK is configured and the API is running.\n\nThis feature is coming soon.')
            )
          )
        )
      );
  }
}
