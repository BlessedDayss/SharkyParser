export interface LogEntry {
  id: number;
  timestamp: string;
  level: string;
  message: string;
  lineNumber?: number;
  filePath?: string;
  rawData?: string;
  fields: Record<string, string>;
}
