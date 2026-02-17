import { Component, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LogDataService } from '../../core/services/log-data.service';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.scss'
})
export class AnalyticsComponent {
  private readonly logData = inject(LogDataService);
  private readonly router = inject(Router);

  entries = this.logData.entries;
  statistics = this.logData.statistics;

  chartData = computed(() => {
    const e = this.entries();
    if (e.length === 0) return { volume: [], errors: [], max: 1 };
    const byHour = new Map<number, { total: number; errors: number }>();
    for (let h = 0; h < 24; h++) byHour.set(h, { total: 0, errors: 0 });
    for (const entry of e) {
      const d = new Date(entry.timestamp);
      const h = d.getHours();
      const bin = byHour.get(h)!;
      bin.total++;
      if (['ERROR', 'FATAL'].includes(entry.level.toUpperCase())) bin.errors++;
    }
    const volume = Array.from(byHour.entries()).map(([, v]) => v.total);
    const max = Math.max(...volume, 1);
    return {
      volume,
      errors: Array.from(byHour.entries()).map(([, v]) => v.errors),
      max
    };
  });

  topSources = computed(() => {
    const e = this.entries();
    const map = new Map<string, number>();
    for (const entry of e) {
      const s = entry.fields['Source'] || entry.fields['Component'] || 'Unknown';
      map.set(s, (map.get(s) ?? 0) + 1);
    }
    return Array.from(map.entries())
      .sort((a, b) => b[1] - a[1])
      .slice(0, 5)
      .map(([name, count]) => ({ name, count }));
  });

  goSelectFile() {
    this.router.navigate(['/logs']);
  }
}
