import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ParseResult } from '../models/parse-result';
import { LogEntry } from '../models/log-entry';
import { LogType } from '../models/log-type';

export interface FileRecordDto {
  id: string;
  fileName: string;
  fileSize: number;
  logType: string;
  uploadedAt: string;
}

interface ApiParseResult {
  fileId: string;
  entries: LogEntry[];
  columns: any[];
  statistics: any;
}

@Injectable({ providedIn: 'root' })
export class LogService {
  private readonly http = inject(HttpClient);

  getLogTypes(): Observable<LogType[]> {
    return this.http.get<LogType[]>('/api/logs/types');
  }

  parse(file: File, logType: string): Observable<ParseResult> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('logType', logType);

    return this.http
      .post<ApiParseResult>('/api/logs/parse', formData)
      .pipe(map(res => this.mapResult(res)));
  }

  getHistory(): Observable<FileRecordDto[]> {
    return this.http.get<FileRecordDto[]>('/api/logs/history');
  }

  getEntries(id: string): Observable<ParseResult> {
    return this.http
      .get<ApiParseResult>(`/api/logs/${id}/entries`)
      .pipe(map(res => this.mapResult(res)));
  }

  private mapResult(res: ApiParseResult): ParseResult {
    return {
      fileId: res.fileId,
      entries: res.entries.map((e, i) => ({ ...e, id: i })),
      columns: res.columns,
      statistics: res.statistics
    };
  }
}
