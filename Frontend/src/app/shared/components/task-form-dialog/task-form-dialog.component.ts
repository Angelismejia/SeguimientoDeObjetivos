import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { TaskService } from '../../../core/services/task.service';
import { TaskItem, TaskStatus, TaskPriority, RecurrenceType } from '../../../core/models/task.model';
import { Objective } from '../../../core/models/objective.model';
import { Category } from '../../../core/models/category.model';
import { TASK_EMOJIS } from '../../../core/constants/task-emojis';
import { mensajeDeError } from '../../../core/utils/http-error.util';
import { rangoOrdenado } from '../../../core/utils/rango-ordenado.validator';
import { recurrenceLabel, taskPriorityLabel, taskStatusLabel } from '../../../core/utils/task-labels';
import { CategoryQuickFormComponent } from '../category-quick-form/category-quick-form.component';

/** Lo que recibe la pantalla al guardar, para actualizar su lista y el progreso. */
export interface TaskSaved {
  task: TaskItem;
  /** Objetivo al que estaba ligada antes de editarla: si cambio, hay que recalcularlo tambien. */
  previousObjectiveId?: number;
}

/**
 * Crear o editar una tarea. Estaba copiado entero en el dashboard y en Tareas.
 * Guarda por su cuenta y avisa con (saved); la lista y el progreso de los
 * objetivos los sigue manejando cada pantalla, porque son suyos.
 *
 * Se muestra con un @if, asi que cada vez que se abre arranca con el
 * formulario cargado desde [task] (editar) o vacio en [defaultDate] (crear).
 */
@Component({
  selector: 'app-task-form-dialog',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CategoryQuickFormComponent],
  templateUrl: './task-form-dialog.component.html',
  styleUrl: '../dialog-form.css'
})
export class TaskFormDialogComponent implements OnInit {
  /** La tarea a editar; null para crear una nueva. */
  @Input() task: TaskItem | null = null;
  /** Dia que se propone al crear (yyyy-MM-dd). */
  @Input() defaultDate = '';
  @Input() objectives: Objective[] = [];
  @Input() categories: Category[] = [];

  @Output() saved = new EventEmitter<TaskSaved>();
  @Output() categoryCreated = new EventEmitter<Category>();
  @Output() closed = new EventEmitter<void>();

  formError = signal('');
  saving = signal(false);
  showCategoryForm = signal(false);
  showAllEmojis = signal(false);

  taskStatuses: TaskStatus[] = ['Pending', 'InProgress', 'Completed', 'Skipped'];
  taskPriorities: TaskPriority[] = ['Low', 'Medium', 'High'];
  // Sin 'None': el desplegable solo se muestra con la casilla de recurrente marcada,
  // y una tarea recurrente sin frecuencia no se repite nunca (el backend la rechaza).
  recurrenceTypes: RecurrenceType[] = ['Daily', 'Weekly', 'Monthly', 'Yearly'];

  pastelColors = ['#c7d2fe', '#bbf7d0', '#fecaca', '#fed7aa', '#fef08a', '#bae6fd', '#f5d0fe', '#e5e7eb'];
  emojis = TASK_EMOJIS;
  emojiPreviewCount = 20;

  taskPriorityLabel = taskPriorityLabel;
  taskStatusLabel = taskStatusLabel;
  recurrenceLabel = recurrenceLabel;

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private taskService: TaskService
  ) {
    this.form = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      emoji: [''],
      color: ['#c7d2fe'],
      scheduledDate: ['', Validators.required],
      scheduledTime: [''],
      endTime: [''],
      priority: ['Medium' as TaskPriority],
      categoryId: [null],
      objectiveId: [null],
      isRecurring: [false],
      recurrenceType: ['None' as RecurrenceType],
      status: ['Pending' as TaskStatus]
    }, { validators: rangoOrdenado('scheduledTime', 'endTime', 'finAntesDeInicio') });
  }

  get editing(): boolean {
    return this.task !== null;
  }

  ngOnInit(): void {
    const task = this.task;
    if (!task) {
      this.form.patchValue({ scheduledDate: this.defaultDate });
      return;
    }

    this.showAllEmojis.set(!!task.emoji && !this.emojis.slice(0, this.emojiPreviewCount).includes(task.emoji));
    this.form.reset({
      title: task.title,
      description: task.description ?? '',
      emoji: task.emoji ?? '',
      color: task.color ?? '#c7d2fe',
      scheduledDate: task.scheduledDate?.substring(0, 10) ?? '',
      scheduledTime: task.scheduledTime?.substring(0, 5) ?? '',
      endTime: task.endTime?.substring(0, 5) ?? '',
      priority: task.priority,
      categoryId: task.categoryId ?? null,
      objectiveId: task.objectiveId ?? null,
      isRecurring: task.isRecurring,
      recurrenceType: task.recurrenceType,
      status: task.status
    });
  }

  onRecurringToggle(): void {
    const esRecurrente = this.form.value.isRecurring;

    if (esRecurrente && this.form.value.recurrenceType === 'None') {
      this.form.patchValue({ recurrenceType: 'Daily' });
    } else if (!esRecurrente) {
      this.form.patchValue({ recurrenceType: 'None' });
    }
  }

  pickColor(color: string): void {
    this.form.patchValue({ color });
  }

  pickEmoji(emoji: string): void {
    this.form.patchValue({ emoji });
  }

  onCategoryCreated(category: Category): void {
    this.categoryCreated.emit(category);
    this.form.patchValue({ categoryId: category.id });
    this.showCategoryForm.set(false);
  }

  close(): void {
    this.closed.emit();
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.formError.set('');

    const v = this.form.value;
    const categoryId = v.categoryId ? Number(v.categoryId) : undefined;
    const objectiveId = v.objectiveId ? Number(v.objectiveId) : undefined;
    const scheduledTime = v.scheduledTime ? v.scheduledTime + ':00' : undefined;
    const endTime = v.endTime ? v.endTime + ':00' : undefined;
    const previa = this.task;

    if (!previa) {
      this.taskService.create({
        title: v.title,
        description: v.description || undefined,
        emoji: v.emoji || undefined,
        color: v.color || undefined,
        scheduledDate: v.scheduledDate,
        scheduledTime,
        endTime,
        priority: v.priority,
        isRecurring: v.isRecurring,
        recurrenceType: v.recurrenceType,
        categoryId,
        objectiveId
      }, this.auth.getUserId()).subscribe({
        next: created => {
          this.saving.set(false);
          this.saved.emit({ task: created });
        },
        error: (e) => {
          this.saving.set(false);
          this.formError.set(mensajeDeError(e, 'No se pudo crear la tarea. Intenta de nuevo.'));
        }
      });
      return;
    }

    // La tarea previa hace falta entera, no solo su objectiveId: este PUT reemplaza
    // la fila y el formulario no tiene campos para el recordatorio ni para la
    // repeticion, asi que sin reenviarlos editar una tarea los borraba (la misma
    // trampa que cuerpoDeActualizacion resuelve en las pantallas que solo marcan).
    this.taskService.update(previa.id, {
      title: v.title,
      description: v.description || undefined,
      emoji: v.emoji || undefined,
      color: v.color || undefined,
      scheduledDate: v.scheduledDate,
      scheduledTime,
      endTime,
      priority: v.priority,
      status: v.status,
      isRecurring: v.isRecurring,
      recurrenceType: v.recurrenceType,
      reminderMinutesBefore: previa.reminderMinutesBefore,
      repeatEveryWeeks: previa.repeatEveryWeeks,
      endRepeatDate: previa.endRepeatDate,
      categoryId,
      objectiveId
    }).subscribe({
      next: updated => {
        this.saving.set(false);
        this.saved.emit({ task: updated, previousObjectiveId: previa.objectiveId });
      },
      error: (e) => {
        this.saving.set(false);
        this.formError.set(mensajeDeError(e, 'No se pudo guardar la tarea. Intenta de nuevo.'));
      }
    });
  }
}
