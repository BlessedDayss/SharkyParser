import { Component, EventEmitter, Output, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LogService } from '../../core/services/log.service';
import { LogType } from '../../core/models/log-type';

@Component({
    selector: 'app-log-type-selection-dialog',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './log-type-selection-dialog.component.html',
    styleUrl: './log-type-selection-dialog.component.scss'
})
export class LogTypeSelectionDialogComponent implements OnInit {
    @Output() select = new EventEmitter<LogType>();
    @Output() close = new EventEmitter<void>();

    private readonly logService = inject(LogService);

    logTypes = signal<LogType[]>([]);
    loading = signal<boolean>(true);
    error = signal<string | null>(null);

    ngOnInit() {
        this.logService.getLogTypes().subscribe({
            next: (types) => {
                this.logTypes.set(types);
                this.loading.set(false);
            },
            error: (err) => {
                console.error('Failed to load log types', err);
                this.error.set('Failed to load log types. Please check your connection.');
                this.loading.set(false);
            }
        });
    }

    onSelect(type: LogType) {
        this.select.emit(type);
    }

    onClose() {
        this.close.emit();
    }
}
