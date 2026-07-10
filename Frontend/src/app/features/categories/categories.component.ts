import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { CategoryService } from '../../core/services/category.service';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmDialogComponent],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.css'
})
export class CategoriesComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);

  categories = signal<Category[]>([]);

  showForm = signal(false);
  editingId = signal<number | null>(null);
  formError = signal('');
  saving = signal(false);
  deleteTarget = signal<Category | null>(null);

  icons = [
    'label', 'work', 'school', 'fitness_center', 'restaurant', 'favorite',
    'home', 'savings', 'book', 'music_note', 'self_improvement',
    'medical_services', 'palette', 'code', 'sports_esports', 'pets',
    'flight', 'shopping_cart', 'brush', 'directions_run'
  ];

  colorPresets = ['#4f46e5', '#7c3aed', '#dc2626', '#ea580c', '#d97706', '#16a34a', '#0891b2', '#2563eb', '#db2777', '#64748b'];

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private categoryService: CategoryService
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      color: ['#4f46e5'],
      icon: ['label']
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.categoryService.getAll(this.auth.getUserId()).subscribe({
      next: categories => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formError.set('');
    this.form.reset({ name: '', color: '#4f46e5', icon: 'label' });
    this.showForm.set(true);
  }

  openEdit(category: Category): void {
    this.editingId.set(category.id);
    this.formError.set('');
    this.form.reset({
      name: category.name,
      color: category.color ?? '#4f46e5',
      icon: category.icon ?? 'label'
    });
    this.showForm.set(true);
  }

  closeForm(): void {
    this.showForm.set(false);
  }

  pickColor(color: string): void {
    this.form.patchValue({ color });
  }

  pickIcon(icon: string): void {
    this.form.patchValue({ icon });
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set('');

    const v = this.form.value;
    const editingId = this.editingId();

    if (editingId === null) {
      this.categoryService.create({
        name: v.name,
        color: v.color,
        icon: v.icon
      }, this.auth.getUserId()).subscribe({
        next: created => {
          this.categories.set([...this.categories(), created]);
          this.saving.set(false);
          this.showForm.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo crear la categoría. Intenta de nuevo.');
        }
      });
    } else {
      this.categoryService.update(editingId, {
        name: v.name,
        color: v.color,
        icon: v.icon
      }).subscribe({
        next: updated => {
          this.categories.set(this.categories().map(c => c.id === updated.id ? updated : c));
          this.saving.set(false);
          this.showForm.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo guardar la categoría. Intenta de nuevo.');
        }
      });
    }
  }

  askDelete(category: Category): void {
    this.deleteTarget.set(category);
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.categoryService.delete(target.id).subscribe({
      next: () => {
        this.categories.set(this.categories().filter(c => c.id !== target.id));
        this.deleteTarget.set(null);
      },
      error: () => {
        this.deleteTarget.set(null);
        this.loadError.set(true);
      }
    });
  }
}
