import { Component, ViewChild, ElementRef, inject, signal, OnInit } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { FileSelectionService } from './core/services/file-selection.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  private http = inject(HttpClient);
  private router = inject(Router);
  private fileSelection = inject(FileSelectionService);

  backendStatus = signal<string>('Checking...');
  backendStatusClass = signal<string>('');
  sidebarHovered = signal<boolean>(false);
  sidebarPinned = signal<boolean>(this.getPinnedState());

  get sidebarVisible() {
    return this.sidebarHovered() || this.sidebarPinned();
  }

  toggleSidebarPin() {
    const next = !this.sidebarPinned();
    this.sidebarPinned.set(next);
    localStorage.setItem('sharky-sidebar-pinned', String(next));
  }

  private getPinnedState(): boolean {
    return localStorage.getItem('sharky-sidebar-pinned') === 'true';
  }

  onOpenLog() {
    this.router.navigate(['/logs']);
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.fileSelection.setFile(file);
      this.router.navigate(['/logs']);
    }
    input.value = '';
  }

  ngOnInit() {
    this.fileSelection.openPicker$.subscribe(() => {
      this.router.navigate(['/logs']);
      this.fileInput.nativeElement.click();
    });

    this.http.get<{ status: string }>('/api/logs/health').subscribe({
      next: () => {
        this.backendStatus.set('Active');
        this.backendStatusClass.set('status-ok');
      },
      error: () => {
        this.backendStatus.set('Not Found');
        this.backendStatusClass.set('status-error');
      }
    });
  }
}
