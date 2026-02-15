import { Injectable, signal } from '@angular/core';
import { LogEntry } from '../models/log-entry';
import { LogStatistics } from '../models/parse-result';

@Injectable({ providedIn: 'root' })
export class LogDataService {
  entries = signal<LogEntry[]>([]);
  statistics = signal<LogStatistics | null>(null);

  setData(entries: LogEntry[], statistics: LogStatistics) {
    this.entries.set(entries);
    this.statistics.set(statistics);
  }
}
