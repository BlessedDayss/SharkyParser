import { Component, inject, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { LogService } from '../../core/services/log.service';
import { FileSelectionService } from '../../core/services/file-selection.service';
import { LogDataService } from '../../core/services/log-data.service';
import { SqlFilterService } from '../../core/services/sql-filter.service';
import { LogEntry } from '../../core/models/log-entry';
import { LogStatistics, LogColumn } from '../../core/models/parse-result';
import { switchMap, take } from 'rxjs/operators';
import { of } from 'rxjs';

@Component({
  selector: 'app-log-explorer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './log-explorer.component.html',
  styleUrl: './log-explorer.component.scss'
})
export class LogExplorerComponent implements OnInit, OnDestroy {
  private readonly logService = inject(LogService);
  private readonly fileSelection = inject(FileSelectionService);
  private readonly logData = inject(LogDataService);
  private readonly sqlFilter = inject(SqlFilterService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  entries = signal<LogEntry[]>(this.logData.entries() || []);
  columns = signal<LogColumn[]>(this.logData.columns() || []);
  statistics = signal<LogStatistics | null>(this.logData.statistics() || null);
  searchTerm = signal('');
  levelFilter = signal('ALL');
  sqlQuery = signal('');
  sqlPendingQuery = signal('');
  sqlError = signal<string | null>(null);
  showSqlBar = signal(false);
  selectedLogType = signal('Installation');
  loading = signal(false);
  error = signal<string | null>(null);
  selectedEntry = signal<LogEntry | null>(null);
  showModal = signal(false);
  fileName = signal<string>('No file selected');
  showAiPrompt = signal(false);
  aiPromptDismissed = signal(false);
  teamCityBlocksInput = signal('');

  timeFilter = signal<string | null>(null);
  isTeamCity = computed(() => this.selectedLogType().toLowerCase() === 'teamcity');

  // ── Infinite scroll ──────────────────────────────────────────
  private readonly PAGE_SIZE = 100;
  displayLimit = signal(100);

  filteredEntries = computed(() => {
    const e = this.entries();
    const search = this.searchTerm().toLowerCase();
    const level = this.levelFilter();
    const time = this.timeFilter();
    const sql = this.sqlQuery().trim();

    // Pre-apply SQL filter first (most selective)
    const sqlFiltered = sql ? this.sqlFilter.filter(e, sql) : e;

    return sqlFiltered.filter((entry) => {
      const matchesSearch = !search || entry.message.toLowerCase().includes(search);
      const matchesLevel = level === 'ALL' || entry.level.toUpperCase() === level.toUpperCase();

      let matchesTime = true;
      if (time) {
        const entryTime = new Date(entry.timestamp).getTime();
        const targetTime = new Date(time).getTime();
        const entryMinute = Math.floor(entryTime / 60000);
        const targetMinute = Math.floor(targetTime / 60000);
        matchesTime = entryMinute === targetMinute;
      }

      return matchesSearch && matchesLevel && matchesTime;
    });
  });

  visibleEntries = computed(() => this.filteredEntries().slice(0, this.displayLimit()));
  hasMore = computed(() => this.displayLimit() < this.filteredEntries().length);

  private filterResetWatcher = effect(() => {
    // Access reactive dependencies so this effect re-runs on change
    this.searchTerm();
    this.levelFilter();
    this.sqlQuery();
    this.timeFilter();
    // Reset scroll page when any filter changes
    this.displayLimit.set(this.PAGE_SIZE);
  });

  private fileWatcher = effect(() => {
    const file = this.fileSelection.getPendingFile();
    const logType = this.fileSelection.getPendingLogType();

    if (file && logType) {
      this.fileSelection.clear();
      this.selectedLogType.set(logType);
      this.parseFile(file, logType);
    }
  });

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const time = params['time'] ?? null;
      this.timeFilter.set(time);

      // When arriving from the dashboard with a time filter but no in-memory entries,
      // load the most recent file from the database automatically.
      if (time && this.entries().length === 0) {
        this.loadLatestFromDb();
      }
    });
  }

  ngOnDestroy() {
    // cleanup if needed
  }

  private loadLatestFromDb() {
    const knownId = this.logData.fileId();

    // If we already know the ID of the file being viewed — use it directly.
    const source$ = knownId
      ? this.logService.getEntries(knownId, this.getTeamCityBlocksFor(this.selectedLogType()))
      : this.logService.getHistory().pipe(
        take(1),
        switchMap(history => {
          if (history.length === 0) return of(null);
          const latest = history[0];
          this.fileName.set(latest.fileName);
          this.selectedLogType.set(latest.logType);
          return this.logService.getEntries(latest.id, this.getTeamCityBlocksFor(latest.logType));
        })
      );

    this.loading.set(true);
    this.error.set(null);

    source$.pipe(take(1)).subscribe({
      next: result => {
        if (result) {
          this.entries.set(result.entries);
          this.columns.set(result.columns);
          this.statistics.set(result.statistics);
          this.logData.setData(result.entries, result.columns, result.statistics, undefined, undefined, result.fileId);
        } else {
          this.error.set('No previously uploaded files found. Please upload a log file first.');
        }
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load log entries from database.');
        this.loading.set(false);
      }
    });
  }

  onFileDropped(file: File) {
    this.parseFile(file, this.selectedLogType());
  }

  parseFile(file: File, logType: string, blocks?: string[]) {
    this.fileName.set(file.name);
    this.loading.set(true);
    this.error.set(null);
    const effectiveBlocks = blocks ?? this.getTeamCityBlocksFor(logType);

    this.logService.parse(file, logType, effectiveBlocks).subscribe({
      next: (result) => {
        console.log('Parse result:', result);
        console.log('Columns received:', result.columns);
        this.entries.set(result.entries);
        this.columns.set(result.columns);
        this.statistics.set(result.statistics);
        this.logData.setData(result.entries, result.columns, result.statistics, file, logType, result.fileId);
        this.loading.set(false);
        if (!this.aiPromptDismissed()) {
          setTimeout(() => this.showAiPrompt.set(true), 1200);
        }
      },
      error: (err) => {
        this.error.set(err.error?.message || 'Failed to parse log file');
        this.loading.set(false);
      }
    });
  }

  applyTeamCityBlocks() {
    if (!this.isTeamCity()) return;

    const blocks = this.getTeamCityBlocksFor('TeamCity');
    const sourceFile = this.logData.sourceFile();
    const fileId = this.logData.fileId();

    if (sourceFile && sourceFile.name === this.fileName()) {
      this.parseFile(sourceFile, this.selectedLogType(), blocks);
      return;
    }

    if (!fileId) {
      this.error.set('No source file available to apply TeamCity blocks.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.logService.getEntries(fileId, blocks).pipe(take(1)).subscribe({
      next: result => {
        this.entries.set(result.entries);
        this.columns.set(result.columns);
        this.statistics.set(result.statistics);
        this.logData.setData(result.entries, result.columns, result.statistics, undefined, this.selectedLogType(), result.fileId);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load log entries with TeamCity block filter.');
        this.loading.set(false);
      }
    });
  }

  getFieldValue(entry: LogEntry, colName: string): string {
    if (colName === 'Timestamp') return this.formatTimestamp(entry.timestamp);
    if (colName === 'Level') return entry.level;
    if (colName === 'Message') return entry.message;
    return entry.fields[colName] || '';
  }

  isPredefined(colName: string): boolean {
    return ['Timestamp', 'Level', 'Message'].includes(colName);
  }

  setLevelFilter(level: string) {
    this.levelFilter.set(level);
  }

  onSqlInput(value: string) {
    this.sqlPendingQuery.set(value);
    if (!value.trim()) {
      this.sqlQuery.set('');
      this.sqlError.set(null);
    }
    // Don't validate on every keystroke — errors only appear on Run / Enter
  }

  runSql() {
    const value = this.sqlPendingQuery().trim();
    if (!value) {
      this.sqlQuery.set('');
      this.sqlError.set(null);
      return;
    }
    const result = this.sqlFilter.validate(value);
    if (result.ok) {
      this.sqlQuery.set(value);
      this.sqlError.set(null);
    } else {
      this.sqlError.set(result.error ?? 'Invalid query');
    }
  }

  onSqlKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.runSql();
    }
  }

  onTableScroll(event: Event) {
    const el = event.target as HTMLElement;
    const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 200;
    if (nearBottom && this.hasMore()) {
      this.displayLimit.update(v => v + this.PAGE_SIZE);
    }
  }

  clearSql() {
    this.sqlQuery.set('');
    this.sqlPendingQuery.set('');
    this.sqlError.set(null);
  }

  toggleSqlBar() {
    this.showSqlBar.update(v => !v);
  }

  openModal(entry: LogEntry) {
    this.selectedEntry.set(entry);
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  goToAiAgent() {
    this.showAiPrompt.set(false);
    this.router.navigate(['/ai-agent']);
  }

  dismissAiPrompt() {
    this.showAiPrompt.set(false);
    this.aiPromptDismissed.set(true);
  }

  formatTimestamp(ts: string): string {
    const d = new Date(ts);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`;
  }

  private getTeamCityBlocksFor(logType: string): string[] | undefined {
    if (logType.toLowerCase() !== 'teamcity') return undefined;

    const unique = new Set<string>();
    for (const chunk of this.teamCityBlocksInput().split(/[,\r\n]+/)) {
      const value = chunk.trim();
      if (!value) continue;
      unique.add(value);
    }

    return [...unique];
  }
}
