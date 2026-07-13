import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { DiaryEntryService } from '../../core/services/diary-entry.service';
import { DiaryEntry } from '../../core/models/diary-entry.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-diary',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmDialogComponent],
  templateUrl: './diary.component.html',
  styleUrl: './diary.component.css'
})
export class DiaryComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);
  entries = signal<DiaryEntry[]>([]);

  showForm = signal(false);
  editingId = signal<number | null>(null);
  formError = signal('');
  saving = signal(false);
  deleteTarget = signal<DiaryEntry | null>(null);

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private diaryEntryService: DiaryEntryService
  ) {
    this.form = this.fb.group({
      title: [''],
      content: ['', Validators.required],
      entryDate: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.diaryEntryService.getAll(this.auth.getUserId()).subscribe({
      next: entries => {
        this.entries.set(this.sortEntries(entries));
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  private sortEntries(entries: DiaryEntry[]): DiaryEntry[] {
    return [...entries].sort((a, b) => new Date(b.entryDate).getTime() - new Date(a.entryDate).getTime());
  }

  private today(): string {
    const d = new Date();
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formError.set('');
    this.form.reset({ title: '', content: '', entryDate: this.today() });
    this.showForm.set(true);
  }

  openEdit(entry: DiaryEntry): void {
    this.editingId.set(entry.id);
    this.formError.set('');
    this.form.reset({
      title: entry.title ?? '',
      content: entry.content,
      entryDate: entry.entryDate?.substring(0, 10) ?? ''
    });
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set('');

    const v = this.form.value;
    const editingId = this.editingId();

    if (editingId === null) {
      this.diaryEntryService.create({
        title: v.title || undefined,
        content: v.content,
        entryDate: v.entryDate
      }, this.auth.getUserId()).subscribe({
        next: created => {
          this.entries.set(this.sortEntries([...this.entries(), created]));
          this.saving.set(false);
          this.showForm.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo guardar la entrada. Intenta de nuevo.');
        }
      });
    } else {
      this.diaryEntryService.update(editingId, {
        title: v.title || undefined,
        content: v.content,
        entryDate: v.entryDate
      }).subscribe({
        next: updated => {
          this.entries.set(this.sortEntries(this.entries().map(e => e.id === updated.id ? updated : e)));
          this.saving.set(false);
          this.showForm.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo guardar la entrada. Intenta de nuevo.');
        }
      });
    }
  }

  askDelete(entry: DiaryEntry): void {
    this.deleteTarget.set(entry);
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.diaryEntryService.delete(target.id).subscribe({
      next: () => {
        this.entries.set(this.entries().filter(e => e.id !== target.id));
        this.deleteTarget.set(null);
      },
      error: () => {
        this.deleteTarget.set(null);
        this.loadError.set(true);
      }
    });
  }
}
