import { Component, inject, signal, computed, ElementRef, ViewChild, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AiAgentService, ChatMessage } from '../../core/services/ai-agent.service';
import { LogDataService } from '../../core/services/log-data.service';

@Component({
  selector: 'app-ai-agent',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './ai-agent.component.html',
  styleUrl: './ai-agent.component.scss'
})
export class AiAgentComponent implements AfterViewChecked {
  @ViewChild('messagesContainer') private messagesContainer!: ElementRef<HTMLDivElement>;

  private agentService = inject(AiAgentService);
  private logData = inject(LogDataService);

  messages = signal<ChatMessage[]>([
    {
      role: 'agent',
      text: 'Hello! I\'m the SharkyParser AI Agent powered by GitHub Copilot. I can analyze your logs, find patterns, summarize errors, and more.\n\nLoad a log file and ask me anything — or use the quick actions on the right.',
      timestamp: new Date()
    }
  ]);

  inputText = signal('');
  isTyping = signal(false);

  hasLogData = computed(() => {
    const entries = this.logData.entries();
    return entries && entries.length > 0;
  });

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

  ngAfterViewChecked() {
    if (this.shouldScroll) {
      this.scrollToBottom();
      this.shouldScroll = false;
    }
  }

  sendMessage() {
    const text = this.inputText().trim();
    if (!text || this.isTyping()) return;

    const userMsg: ChatMessage = { role: 'user', text, timestamp: new Date() };
    this.messages.update(msgs => [...msgs, userMsg]);
    this.inputText.set('');
    this.isTyping.set(true);
    this.shouldScroll = true;

    const logContext = this.buildLogContext();

    this.agentService.chat(text, logContext).subscribe({
      next: (response) => {
        const agentMsg: ChatMessage = { role: 'agent', text: response, timestamp: new Date() };
        this.messages.update(msgs => [...msgs, agentMsg]);
        this.isTyping.set(false);
        this.shouldScroll = true;
      },
      error: () => {
        const agentMsg: ChatMessage = {
          role: 'agent',
          text: 'Sorry, I couldn\'t process your request. Make sure the backend is running and the Copilot SDK is configured.',
          timestamp: new Date()
        };
        this.messages.update(msgs => [...msgs, agentMsg]);
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

  private buildLogContext(): string | undefined {
    const entries = this.logData.entries();
    if (!entries?.length) return undefined;

    const sample = entries.slice(0, 50).map(e =>
      `[${e.timestamp}] ${e.level}: ${e.message}`
    ).join('\n');

    const stats = this.logData.statistics();
    const header = stats
      ? `Log Statistics: Total=${stats.total}, Errors=${stats.errors}, Warnings=${stats.warnings}, Info=${stats.info}\n---\n`
      : '';

    return header + sample;
  }

  private scrollToBottom() {
    try {
      const el = this.messagesContainer?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
