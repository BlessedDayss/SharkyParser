import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
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

  content = signal<string>('');

  ngOnInit() {
    this.changelogService.getChangelog().subscribe({
      next: (text) => this.content.set(text),
      error: () => this.content.set('# Changelog\n\nFailed to load changelog.')
    });
  }
}
