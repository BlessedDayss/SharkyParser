import { Injectable, signal } from '@angular/core';
import { LogEntry } from '../models/log-entry';
import { LogStatistics, LogColumn } from '../models/parse-result';

@Injectable({ providedIn: 'root' })
export class LogDataService {
  entries = signal<LogEntry[]>([]);
  columns = signal<LogColumn[]>([]);
  statistics = signal<LogStatistics | null>(null);
  sourceFile = signal<File | null>(null);
  sourceLogType = signal<string>('Installation');
  fileId = signal<string | null>(null);

  setData(entries: LogEntry[], columns: LogColumn[], statistics: LogStatistics, file?: File, logType?: string, fileId?: string) {
    this.entries.set(entries);
    this.columns.set(columns);
    this.statistics.set(statistics);
    if (file) this.sourceFile.set(file);
    if (logType) this.sourceLogType.set(logType);
    if (fileId) this.fileId.set(fileId);
  }

  /**
   * Build full log context string for AI Agent.
   * Includes statistics header + ALL entries (up to a token-friendly limit).
   */
  buildAiContext(): string | undefined {
    const entries = this.entries();
    const stats = this.statistics();
    if (!entries?.length) return undefined;

    const lines: string[] = [];

    // Header with statistics
    if (stats) {
      lines.push(`=== LOG ANALYSIS CONTEXT ===`);
      lines.push(`File: ${this.sourceFile()?.name ?? 'unknown'}`);
      lines.push(`Total Entries: ${stats.total}`);
      lines.push(`Errors: ${stats.errors} | Warnings: ${stats.warnings} | Info: ${stats.info}`);
      lines.push(`Health: ${stats.isHealthy ? 'Healthy' : 'Unhealthy'}`);
      lines.push(`===========================\n`);
    }

    // Include ALL entries (capped at ~200 to stay within token limits)
    const maxEntries = Math.min(entries.length, 200);

    // First prioritize errors and warnings
    const errors = entries.filter(e => e.level?.toUpperCase() === 'ERROR');
    const warnings = entries.filter(e => e.level?.toUpperCase() === 'WARN' || e.level?.toUpperCase() === 'WARNING');
    const others = entries.filter(e => {
      const lvl = e.level?.toUpperCase();
      return lvl !== 'ERROR' && lvl !== 'WARN' && lvl !== 'WARNING';
    });

    if (errors.length > 0) {
      lines.push(`--- ERRORS (${errors.length}) ---`);
      for (const e of errors.slice(0, 50)) {
        lines.push(`[${e.timestamp}] ${e.level}: ${e.message}`);
        const stackTrace = e.fields['StackTrace'];
        if (stackTrace) {
          lines.push(`  Stack: ${stackTrace.substring(0, 300)}`);
        }
      }
      lines.push('');
    }

    if (warnings.length > 0) {
      lines.push(`--- WARNINGS (${warnings.length}) ---`);
      for (const e of warnings.slice(0, 50)) {
        lines.push(`[${e.timestamp}] ${e.level}: ${e.message}`);
      }
      lines.push('');
    }

    // Rest of entries (INFO, DEBUG, etc.)
    lines.push(`--- ALL LOG ENTRIES (showing ${Math.min(others.length, maxEntries)} of ${others.length}) ---`);
    for (const e of others.slice(0, maxEntries)) {
      lines.push(`[${e.timestamp}] ${e.level}: ${e.message}`);
    }

    return lines.join('\n');
  }
}
