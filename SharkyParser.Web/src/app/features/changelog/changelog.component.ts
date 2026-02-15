import { Component, inject, signal, OnInit } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { CommonModule } from '@angular/common';
import { marked } from 'marked';
import { ChangelogService } from '../../core/services/changelog.service';

@Component({
  selector: 'app-changelog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './changelog.component.html',
  styleUrl: './changelog.component.scss'
})
export class ChangelogComponent implements OnInit {
  private changelogService = inject(ChangelogService);
  private sanitizer = inject(DomSanitizer);

  htmlContent = signal<SafeHtml>('');
  loading = signal<boolean>(true);

  ngOnInit() {
    this.changelogService.getChangelog().subscribe({
      next: (text) => {
        const html = marked.parse(text, { async: false }) as string;
        this.htmlContent.set(this.sanitizer.bypassSecurityTrustHtml(html));
        this.loading.set(false);
      },
      error: () => {
        const html = marked.parse('# Changelog\n\nFailed to load changelog.', { async: false }) as string;
        this.htmlContent.set(this.sanitizer.bypassSecurityTrustHtml(html));
        this.loading.set(false);
      }
    });
  }
}
