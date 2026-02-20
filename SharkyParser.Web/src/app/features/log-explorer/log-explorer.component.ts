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
  private readonly TEAMCITY_SCAN_LIMIT_BYTES = 2 * 1024 * 1024;
  private teamCityScanToken = 0;
  private pendingTeamCityFile: File | null = null;
  private pendingTeamCityType: string | null = null;

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
  showTeamCityBlockModal = signal(false);
  teamCityBlockModalLoading = signal(false);
  teamCityBlockModalError = signal<string | null>(null);
  detectedTeamCityBlocks = signal<string[]>([]);
  selectedTeamCityBlocks = signal<string[]>([]);
  teamCityBlockSearch = signal('');
  filteredTeamCityBlocks = computed(() => {
    const search = this.teamCityBlockSearch().trim().toLowerCase();
    const blocks = this.detectedTeamCityBlocks();

    if (!search) return blocks;
    return blocks.filter(block => block.toLowerCase().includes(search));
  });

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

      if (this.isTeamCityType(logType)) {
        void this.openTeamCityBlockModal(file, logType);
        return;
      }

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

    if (sourceFile) {
      this.parseFile(sourceFile, this.selectedLogType(), blocks);
      return;
    }

    if (!fileId) {
      this.error.set('No TeamCity source file available. Please upload a file again.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.logService.getEntries(fileId, blocks).pipe(take(1)).subscribe({
      next: result => {
        this.entries.set(result.entries);
        this.columns.set(result.columns);
        this.statistics.set(result.statistics);
        this.logData.setData(
          result.entries,
          result.columns,
          result.statistics,
          undefined,
          this.selectedLogType(),
          result.fileId
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to apply TeamCity block filter');
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

  onTeamCityBlockToggle(block: string, event: Event) {
    const checked = (event.target as HTMLInputElement | null)?.checked ?? false;
    const next = new Set(this.selectedTeamCityBlocks());
    if (checked) {
      next.add(block);
    } else {
      next.delete(block);
    }

    this.selectedTeamCityBlocks.set([...next]);
  }

  selectAllTeamCityBlocks() {
    this.selectedTeamCityBlocks.set([...this.detectedTeamCityBlocks()]);
  }

  clearSelectedTeamCityBlocks() {
    this.selectedTeamCityBlocks.set([]);
  }

  applySelectedTeamCityBlocks() {
    const blocks = [...this.selectedTeamCityBlocks()];
    this.teamCityBlocksInput.set(blocks.join('\n'));
    this.parsePendingTeamCityFile(blocks);
  }

  parseTeamCityWithoutBlocks() {
    this.teamCityBlocksInput.set('');
    this.parsePendingTeamCityFile();
  }

  private parsePendingTeamCityFile(blocks?: string[]) {
    const file = this.pendingTeamCityFile;
    const logType = this.pendingTeamCityType ?? 'TeamCity';
    this.resetTeamCityModalState();

    if (!file) {
      this.error.set('TeamCity file is missing. Please select file again.');
      return;
    }

    this.parseFile(file, logType, blocks);
  }

  private async openTeamCityBlockModal(file: File, logType: string) {
    this.pendingTeamCityFile = file;
    this.pendingTeamCityType = logType;
    this.teamCityBlockModalError.set(null);
    this.detectedTeamCityBlocks.set([]);
    this.selectedTeamCityBlocks.set(this.getTeamCityBlocksFor(logType) ?? []);
    this.teamCityBlockSearch.set('');
    this.showTeamCityBlockModal.set(true);
    this.teamCityBlockModalLoading.set(true);

    const scanToken = ++this.teamCityScanToken;

    try {
      const detectedBlocks = await this.detectTeamCityBlocks(file);
      if (scanToken !== this.teamCityScanToken) return;

      this.detectedTeamCityBlocks.set(detectedBlocks);
    } catch {
      if (scanToken !== this.teamCityScanToken) return;

      this.teamCityBlockModalError.set(
        'Failed to detect TeamCity blocks automatically. Continue without filter or select manually later.'
      );
    } finally {
      if (scanToken === this.teamCityScanToken) {
        this.teamCityBlockModalLoading.set(false);
      }
    }
  }

  private resetTeamCityModalState() {
    this.showTeamCityBlockModal.set(false);
    this.teamCityBlockModalLoading.set(false);
    this.teamCityBlockModalError.set(null);
    this.detectedTeamCityBlocks.set([]);
    this.selectedTeamCityBlocks.set([]);
    this.teamCityBlockSearch.set('');
    this.pendingTeamCityFile = null;
    this.pendingTeamCityType = null;
  }

  private async detectTeamCityBlocks(file: File): Promise<string[]> {
    const snippet = await file.slice(0, this.TEAMCITY_SCAN_LIMIT_BYTES).text();
    const unique = new Set<string>();

    for (const line of snippet.split(/\r?\n/)) {
      const serviceBlock = this.tryExtractTeamCityServiceBlock(line);
      if (serviceBlock) unique.add(serviceBlock);

      const context = this.tryParseTeamCityContextLine(line);
      if (!context) continue;

      if (context.bracket) unique.add(context.bracket);

      const boundaryBlock = this.tryExtractTeamCityBoundary(context.message);
      if (boundaryBlock) unique.add(boundaryBlock);
    }

    return [...unique].sort((a, b) => a.localeCompare(b));
  }

  private tryExtractTeamCityServiceBlock(line: string): string | null {
    const match = line.match(/^##teamcity\[blockOpened\s+.*name='((?:[^'|]|\|.)*)'/i);
    if (!match?.[1]) return null;
    const value = this.unescapeTeamCityValue(match[1]).trim();
    return value || null;
  }

  private tryParseTeamCityContextLine(line: string): { bracket: string | null; message: string } | null {
    const match = line.match(
      /^\[(?:\d{2}:\d{2}:\d{2}|\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\](?:[WEi]:)?(?<tail>.*)$/
    );
    const rawTail = match?.groups?.['tail'];
    if (rawTail === undefined) return null;

    let index = 0;

    while (index < rawTail.length && rawTail[index] === ' ') index++;
    if (index < rawTail.length && rawTail[index] === ':') index++;
    while (index < rawTail.length && (rawTail[index] === ' ' || rawTail[index] === '\t')) index++;

    let bracket: string | null = null;
    if (index < rawTail.length && rawTail[index] === '[') {
      const closing = rawTail.indexOf(']', index + 1);
      if (closing > index) {
        bracket = rawTail.substring(index + 1, closing).trim() || null;
        index = closing + 1;
      }
    }

    return {
      bracket,
      message: rawTail.substring(index).trim()
    };
  }

  private tryExtractTeamCityBoundary(message: string): string | null {
    if (!message) return null;

    const colonIndex = message.indexOf(':');
    if (colonIndex > 0) {
      const candidate = message.substring(0, colonIndex).trim();
      if (this.isValidTeamCityBlockName(candidate)) return candidate;
    }

    const durationMatch = message.match(/^(.+?)\s\([^)]+\)$/);
    const durationCandidate = durationMatch?.[1]?.trim();
    if (durationCandidate && this.isValidTeamCityBlockName(durationCandidate)) {
      return durationCandidate;
    }

    return null;
  }

  private isValidTeamCityBlockName(candidate: string): boolean {
    if (candidate.length < 2 || candidate.length > 120) return false;
    return /[A-Za-z0-9]/.test(candidate);
  }

  private unescapeTeamCityValue(value: string): string {
    return value
      .replace(/\|'/g, '\'')
      .replace(/\|n/g, '\n')
      .replace(/\|r/g, '\r')
      .replace(/\|\|/g, '|')
      .replace(/\|\[/g, '[')
      .replace(/\|\]/g, ']');
  }

  private isTeamCityType(logType: string): boolean {
    return logType.trim().toLowerCase() === 'teamcity';
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
