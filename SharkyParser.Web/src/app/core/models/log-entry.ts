export interface LogEntry {
  id: number;
  timestamp: string;
  level: string;
  message: string;
  source: string;
  stackTrace: string;
  lineNumber?: number;
  filePath?: string;
}
