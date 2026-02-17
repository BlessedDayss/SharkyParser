import { Injectable, signal } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FileSelectionService {
  pendingFile = signal<File | null>(null);
  pendingLogTypeName = signal<string | null>(null);

  private readonly openPickerSubject = new Subject<void>();
  openPicker$ = this.openPickerSubject.asObservable();

  requestOpenPicker() {
    this.openPickerSubject.next();
  }

  setFile(file: File) {
    this.pendingFile.set(file);
  }

  setLogType(typeName: string) {
    this.pendingLogTypeName.set(typeName);
  }

  getPendingFile() {
    return this.pendingFile();
  }

  getPendingLogType() {
    return this.pendingLogTypeName();
  }

  clear() {
    this.pendingFile.set(null);
    this.pendingLogTypeName.set(null);
  }
}
