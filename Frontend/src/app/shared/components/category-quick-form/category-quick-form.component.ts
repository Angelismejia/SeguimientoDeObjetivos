import { Component, EventEmitter, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CategoryService } from '../../../core/services/category.service';
import { Category } from '../../../core/models/category.model';
import { mensajeDeError } from '../../../core/utils/http-error.util';

/**
 * Crear una categoria sin salir del formulario de tarea. Se muestra con un @if,
 * asi que cada vez que se abre arranca con el formulario limpio.
 */
@Component({
  selector: 'app-category-quick-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './category-quick-form.component.html',
  styleUrl: '../dialog-form.css'
})
export class CategoryQuickFormComponent {
  @Output() created = new EventEmitter<Category>();
  @Output() closed = new EventEmitter<void>();

  saving = signal(false);
  formError = signal('');
  form: FormGroup;

  icons = [
    'label', 'work', 'school', 'fitness_center', 'restaurant', 'favorite',
    'home', 'savings', 'book', 'music_note', 'self_improvement',
    'medical_services', 'palette', 'code', 'sports_esports', 'pets',
    'flight', 'shopping_cart', 'brush', 'directions_run'
  ];
  colorPresets = ['#4f46e5', '#7c3aed', '#dc2626', '#ea580c', '#d97706', '#16a34a', '#0891b2', '#2563eb', '#db2777', '#64748b'];

  constructor(
    private fb: FormBuilder,
    private categoryService: CategoryService
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      color: ['#4f46e5'],
      icon: ['label']
    });
  }

  pickColor(color: string): void {
    this.form.patchValue({ color });
  }

  pickIcon(icon: string): void {
    this.form.patchValue({ icon });
  }

  close(): void {
    this.closed.emit();
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set('');

    const v = this.form.value;
    this.categoryService.create({ name: v.name, color: v.color, icon: v.icon }).subscribe({
      next: created => {
        this.saving.set(false);
        this.created.emit(created);
      },
      error: (e) => {
        this.saving.set(false);
        this.formError.set(mensajeDeError(e, 'No se pudo crear la categoría. Intenta de nuevo.'));
      }
    });
  }
}
