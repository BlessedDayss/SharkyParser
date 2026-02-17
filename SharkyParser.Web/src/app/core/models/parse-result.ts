import { LogEntry } from './log-entry';

export interface LogColumn {
  name: string;
  header: string;
  description?: string;
  isPredefined: boolean;
}

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
  columns: LogColumn[];
  statistics: LogStatistics;
}
