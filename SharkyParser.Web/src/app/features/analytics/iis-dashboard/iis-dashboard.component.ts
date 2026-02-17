import { Component, computed, input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { IisLogStatistics } from '../../../core/models/parse-result';

@Component({
    selector: 'app-iis-dashboard',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './iis-dashboard.component.html',
    styleUrls: ['./iis-dashboard.component.scss']
})
export class IisDashboardComponent {
    private readonly router = inject(Router);
    stats = input.required<IisLogStatistics>();

    // 1. Requests Per Minute Chart Data
    rpmChart = computed(() => {
        const raw = this.stats().requestsPerMinute;
        if (!raw) return { points: '', dates: [], pointObjects: [] };

        const entries = Object.entries(raw)
            .map(([k, v]) => ({ time: new Date(k).getTime(), count: v }))
            .sort((a, b) => a.time - b.time);

        if (entries.length === 0) return { points: '', dates: [], pointObjects: [] };

        const minTime = entries[0].time;
        const maxTime = entries[entries.length - 1].time;
        const timeRange = maxTime - minTime || 1;

        const maxCount = Math.max(...entries.map(e => e.count), 1);

        // Generate SVG points & clickable objects
        const pointObjects = entries.map(e => {
            const x = ((e.time - minTime) / timeRange) * 1000;
            const y = 200 - ((e.count / maxCount) * 180);
            return { x, y, time: e.time, count: e.count };
        });

        const points = pointObjects.map(p => `${p.x},${p.y}`).join(' ');

        // Generate axis labels
        const dates = [];
        if (entries.length > 0) {
            dates.push(new Date(minTime));
            dates.push(new Date(minTime + timeRange * 0.25));
            dates.push(new Date(minTime + timeRange * 0.5));
            dates.push(new Date(minTime + timeRange * 0.75));
            dates.push(new Date(maxTime));
        }

        return { points, dates, maxCount, pointObjects };
    });

    onPointClick(timestamp: number) {
        // Navigate to logs filtered by this minute
        // We'll pass the timestamp ISO string. The filtering logic will need to handle per-minute matching.
        const iso = new Date(timestamp).toISOString();
        this.router.navigate(['/logs'], { queryParams: { time: iso } });
    }

    // 2. Top IPs
    topIpsChart = computed(() => {
        const raw = this.stats().topIps;
        if (!raw) return [];
        const entries = Object.entries(raw)
            .map(([ip, count]) => ({ ip, count }))
            .sort((a, b) => b.count - a.count);

        if (entries.length === 0) return [];
        const max = entries[0].count;

        return entries.map(e => ({
            ...e,
            percent: (e.count / max) * 100
        }));
    });

    // 3. Response Time Distribution
    distributionChart = computed(() => {
        const raw = this.stats().responseTimeDistribution;
        // Fixed order
        const keys = ["< 200ms", "200-500ms", "500-1000ms", "1000-2000ms", "2000-5000ms", "> 5000ms"];
        const entries = keys.map(k => ({ label: k, count: raw?.[k] || 0 }));
        const max = Math.max(...entries.map(e => e.count), 1);

        return entries.map(e => ({
            ...e,
            heightPercent: (e.count / max) * 100
        }));
    });

    slowestRequests = computed(() => {
        return this.stats().slowestRequests || [];
    });
}
