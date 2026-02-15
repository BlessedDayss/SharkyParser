import { LogEntry } from './log-entry';

export interface LogStatistics {
  total: number;
  errors: number;
  warnings: number;
  info: number;
  debug: number;
  isHealthy: boolean;
}

export interface ParseResult {
  entries: LogEntry[];
  statistics: LogStatistics;
}
