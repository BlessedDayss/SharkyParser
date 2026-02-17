import { LogEntry } from './log-entry';

export interface LogColumn {
  name: string;
  header: string;
  description?: string;
  isPredefined: boolean;
}

export interface SlowRequestStats {
  url: string;
  method: string | null;
  durationMs: number;
  timestamp: string;
  statusCode: number;
}

export interface IisLogStatistics {
  requestsPerMinute: { [key: string]: number };
  topIps: { [key: string]: number };
  slowestRequests: SlowRequestStats[];
  responseTimeDistribution: { [key: string]: number };
}

export interface LogStatistics {
  total: number;
  errors: number;
  warnings: number;
  info: number;
  debug: number;
  isHealthy: boolean;
  extendedData?: string;
  iisStatistics?: IisLogStatistics | null;
}

export interface ParseResult {
  fileId: string;
  entries: LogEntry[];
  columns: LogColumn[];
  statistics: LogStatistics;
}
