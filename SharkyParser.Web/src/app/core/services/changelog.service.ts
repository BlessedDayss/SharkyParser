import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ChangelogService {
  private http = inject(HttpClient);

  getChangelog(): Observable<string> {
    return this.http.get('/api/changelog', { responseType: 'text' });
  }
}
