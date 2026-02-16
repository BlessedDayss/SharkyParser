import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FileSelectionService {
  pendingFile = signal<File | null>(null);

  private readonly openPickerSubject = new Subject<void>();
  openPicker$ = this.openPickerSubject.asObservable();

  requestOpenPicker() {
    this.openPickerSubject.next();
  }

  setFile(file: File) {
    this.pendingFile.set(file);
  }

  takeFile(): File | null {
    const file = this.pendingFile();
    this.pendingFile.set(null);
    return file;
  }
}
