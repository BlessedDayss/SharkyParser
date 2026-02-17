import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ParseResult } from '../models/parse-result';
import { LogEntry } from '../models/log-entry';
import { LogType } from '../models/log-type';

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
      .post<{ entries: LogEntry[]; columns: any[]; statistics: any }>('/api/logs/parse', formData)
      .pipe(
        map((res) => ({
          entries: res.entries.map((e, i) => ({ ...e, id: i })),
          columns: res.columns,
          statistics: res.statistics
        }))
      );
  }
}
