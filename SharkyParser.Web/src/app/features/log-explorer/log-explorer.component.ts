import { Component, inject, signal, computed, OnInit, OnDestroy, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LogService } from '../../core/services/log.service';
import { FileSelectionService } from '../../core/services/file-selection.service';
import { LogDataService } from '../../core/services/log-data.service';
import { LogEntry } from '../../core/models/log-entry';
import { LogStatistics } from '../../core/models/parse-result';

@Component({
  selector: 'app-log-explorer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './log-explorer.component.html',
  styleUrl: './log-explorer.component.scss'
})
export class LogExplorerComponent implements OnInit {
  private logService = inject(LogService);
  private fileSelection = inject(FileSelectionService);
  private logData = inject(LogDataService);
  private router = inject(Router);

  entries = signal<LogEntry[]>([]);
  statistics = signal<LogStatistics | null>(null);
  searchTerm = signal('');
  levelFilter = signal('ALL');
  selectedLogType = signal('Installation');
  loading = signal(false);
  error = signal<string | null>(null);
  selectedEntry = signal<LogEntry | null>(null);
  showModal = signal(false);
  fileName = signal<string>('No file selected');
  showAiPrompt = signal(false);
  aiPromptDismissed = signal(false);

  filteredEntries = computed(() => {
    const e = this.entries();
    const search = this.searchTerm().toLowerCase();
    const level = this.levelFilter();
    return e.filter((entry) => {
      const matchesSearch = !search || entry.message.toLowerCase().includes(search);
      const matchesLevel = level === 'ALL' || entry.level.toUpperCase() === level.toUpperCase();
      return matchesSearch && matchesLevel;
    });
  });

  // Watch for file changes reactively — handles the case when
  // the component is already mounted and a new file is selected
  private fileWatcher = effect(() => {
    const file = this.fileSelection.pendingFile();
    if (file) {
      this.fileSelection.takeFile(); // consume it
      this.parseFile(file);
    }
  });

  ngOnInit() {
    // pendingFile effect above handles everything now
  }

  onFileDropped(file: File) {
    this.parseFile(file);
  }

  parseFile(file: File) {
    this.fileName.set(file.name);
    this.loading.set(true);
    this.error.set(null);
    const logType = this.selectedLogType();
    this.logService.parse(file, logType).subscribe({
      next: (result) => {
        this.entries.set(result.entries);
        this.statistics.set(result.statistics);
        this.logData.setData(result.entries, result.statistics, file, logType);
        this.loading.set(false);
        // Show AI prompt after a short delay once parsing completes
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

  setLevelFilter(level: string) {
    this.levelFilter.set(level);
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
}
