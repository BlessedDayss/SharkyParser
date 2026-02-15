import { Component, inject, signal, computed, ElementRef, ViewChild, AfterViewChecked, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { marked } from 'marked';
import { AiAgentService, ChatMessage, DeviceCodeResponse } from '../../core/services/ai-agent.service';
import { LogDataService } from '../../core/services/log-data.service';

export interface DisplayMessage extends ChatMessage {
  html?: SafeHtml;
}

@Component({
  selector: 'app-ai-agent',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-agent.component.html',
  styleUrl: './ai-agent.component.scss'
})
export class AiAgentComponent implements AfterViewChecked, OnInit, OnDestroy {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef<HTMLDivElement>;

  private agentService = inject(AiAgentService);
  private logData = inject(LogDataService);
  private sanitizer = inject(DomSanitizer);

  // ── Auth state ─────────────────────────────────────────
  isAuthenticated = signal(false);
  authLoading = signal(true);
  deviceCode = signal<DeviceCodeResponse | null>(null);
  authMessage = signal('');
  authFlowActive = signal(false);
  private pollTimer: boolean | null = null;

  // ── Chat state ─────────────────────────────────────────
  messages = signal<DisplayMessage[]>([]);
  inputText = signal('');
  isTyping = signal(false);

  hasLogData = computed(() => {
    const entries = this.logData.entries();
    return entries && entries.length > 0;
  });

  logFileName = computed(() => this.logData.sourceFile()?.name ?? null);

  logSummary = computed(() => {
    const stats = this.logData.statistics();
    const entries = this.logData.entries();
    if (!stats || !entries?.length) return null;
    return {
      total: stats.total,
      errors: stats.errors,
      warnings: stats.warnings,
      info: stats.info,
      entryCount: entries.length
    };
  });

  quickActions = [
    'Summarize all errors',
    'Find recurring patterns',
    'Show error timeline',
    'Explain top warnings'
  ];

  private shouldScroll = false;

  ngOnInit() {
    this.checkAuthStatus();
  }

  ngOnDestroy() {
    this.stopPolling();
  }

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  // ── Auth methods ───────────────────────────────────────

  checkAuthStatus() {
    this.authLoading.set(true);
    this.agentService.getAuthStatus().subscribe({
      next: (res) => {
        this.isAuthenticated.set(res.authenticated);
        this.authLoading.set(false);
        if (res.authenticated && this.messages().length === 0) {
          const hasLogs = this.logData.entries().length > 0;
          const fileName = this.logData.sourceFile()?.name;
          const greeting = hasLogs
            ? `Hello! I\'m the SharkyParser AI Agent.\n\nI can see you have **${fileName}** loaded with **${this.logData.entries().length} entries**. Ask me anything — or use the quick actions on the right.`
            : 'Hello! I\'m the SharkyParser AI Agent. I can analyze your logs, find patterns, summarize errors, and more.\n\nLoad a log file and ask me anything — or use the quick actions on the right.';
          this.messages.set([this.makeAgentMsg(greeting)]);
        }
      },
      error: () => {
        this.isAuthenticated.set(false);
        this.authLoading.set(false);
      }
    });
  }

  startAuth() {
    this.authFlowActive.set(true);
    this.authMessage.set('Requesting code from GitHub...');

    this.agentService.startDeviceFlow().subscribe({
      next: (res) => {
        this.deviceCode.set(res);
        this.authMessage.set('');
        this.startPolling(res.interval);
      },
      error: () => {
        this.authMessage.set('Failed to start authentication. Is the backend running?');
        this.authFlowActive.set(false);
      }
    });
  }

  copyCode() {
    const code = this.deviceCode()?.userCode;
    if (code) {
      navigator.clipboard.writeText(code);
    }
  }

  openGitHub() {
    const uri = this.deviceCode()?.verificationUri;
    if (uri) {
      window.open(uri, '_blank');
    }
  }

  logout() {
    this.agentService.logout().subscribe(() => {
      this.isAuthenticated.set(false);
      this.deviceCode.set(null);
      this.authFlowActive.set(false);
      this.messages.set([]);
    });
  }

  private startPolling(interval: number) {
    const pollMs = Math.max(interval, 5) * 1000;
    this.pollTimer = true; // flag to allow stopping

    const doPoll = () => {
      if (!this.pollTimer) return;

      this.agentService.pollForToken().subscribe({
        next: (res) => {
          if (res.status === 'success') {
            this.pollTimer = null;
            this.isAuthenticated.set(true);
            this.authFlowActive.set(false);
            this.deviceCode.set(null);
            this.messages.set([
              this.makeAgentMsg('Authenticated successfully! I\'m ready to analyze your logs.\n\nLoad a log file and ask me anything — or use the quick actions on the right.')
            ]);
          } else if (res.status === 'expired' || res.status === 'denied' || res.status === 'error') {
            this.pollTimer = null;
            this.authMessage.set(res.message);
            this.authFlowActive.set(false);
            this.deviceCode.set(null);
          } else {
            // 'pending' — schedule next poll
            setTimeout(doPoll, pollMs);
          }
        },
        error: () => {
          this.pollTimer = null;
          this.authMessage.set('Lost connection to the backend.');
          this.authFlowActive.set(false);
        }
      });
    };

    setTimeout(doPoll, pollMs);
  }

  private stopPolling() {
    this.pollTimer = null;
  }

  // ── Chat methods ───────────────────────────────────────

  sendMessage() {
    const text = this.inputText().trim();
    if (!text || this.isTyping()) return;

    const userMsg: DisplayMessage = { role: 'user', text, timestamp: new Date() };
    this.messages.update(msgs => [...msgs, userMsg]);
    this.inputText.set('');
    this.isTyping.set(true);
    this.shouldScroll = true;

    const logContext = this.logData.buildAiContext();

    this.agentService.chat(text, logContext).subscribe({
      next: (response) => {
        this.messages.update(msgs => [...msgs, this.makeAgentMsg(response)]);
        this.isTyping.set(false);
        this.shouldScroll = true;
      },
      error: () => {
        this.messages.update(msgs => [...msgs, this.makeAgentMsg(
          'Sorry, I couldn\'t process your request. Make sure the backend is running.'
        )]);
        this.isTyping.set(false);
        this.shouldScroll = true;
      }
    });
  }

  onQuickAction(action: string) {
    this.inputText.set(action);
    this.sendMessage();
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private makeAgentMsg(text: string): DisplayMessage {
    const html = this.sanitizer.bypassSecurityTrustHtml(
      marked.parse(text, { async: false }) as string
    );
    return { role: 'agent', text, timestamp: new Date(), html };
  }

  private scrollToBottom() {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
