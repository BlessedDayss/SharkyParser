import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FileSelectionService } from '../../core/services/file-selection.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  private router = inject(Router);
  private fileSelection = inject(FileSelectionService);

  onGetStarted() {
    this.router.navigate(['/logs']);
  }

  onOpenLog() {
    this.fileSelection.requestOpenPicker();
  }
}
