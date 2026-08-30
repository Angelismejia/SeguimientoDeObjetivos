import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TaskService } from '../../core/services/task.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { CategoryService } from '../../core/services/category.service';
import { TaskItem, TaskPriority, TaskStatus, RecurrenceType } from '../../core/models/task.model';
import { Objective } from '../../core/models/objective.model';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { isTaskDoneOn, isTaskOverdue } from '../../core/utils/task-status.util';
import { computeObjectiveProgress } from '../../core/utils/objective-progress.util';
import { TASK_EMOJIS } from '../../core/constants/task-emojis';
import { mensajeDeError } from '../../core/utils/http-error.util';
import { rangoOrdenado } from '../../core/utils/rango-ordenado.validator';

type TaskFilter = 'all' | 'today' | 'upcoming' | 'recurring' | 'completed' | 'important';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmDialogComponent],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.css'
})
export class TasksComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);

  allTasks = signal<TaskItem[]>([]);
  objectives = signal<Objective[]>([]);
  categories = signal<Category[]>([]);

  searchQuery = signal('');
  activeFilter = signal<TaskFilter>('all');

  filters: { key: TaskFilter; label: string; icon: string }[] = [
    { key: 'all', label: 'Todas', icon: 'list' },
    { key: 'today', label: 'Hoy', icon: 'today' },
    { key: 'upcoming', label: 'Próximas', icon: 'schedule' },
    { key: 'recurring', label: 'Recurrentes', icon: 'repeat' },
    { key: 'completed', label: 'Completadas', icon: 'check_circle' },
    { key: 'important', label: 'Importantes', icon: 'star' }
  ];

  filteredTasks = computed(() => {
    const filter = this.activeFilter();
    const query = this.searchQuery().trim().toLowerCase();
    const todayKey = this.dateKey(new Date());
    let tasks = this.allTasks();

    switch (filter) {
      case 'today':
        tasks = tasks.filter(t => t.scheduledDate?.substring(0, 10) === todayKey);
        break;
      case 'upcoming':
        tasks = tasks.filter(t =>
          !isTaskDoneOn(t, todayKey) && t.status !== 'Skipped' &&
          !!t.scheduledDate && t.scheduledDate.substring(0, 10) > todayKey
        );
        break;
      case 'recurring':
        tasks = tasks.filter(t => t.isRecurring);
        break;
      case 'completed':
        tasks = tasks.filter(t => isTaskDoneOn(t, todayKey));
        break;
      case 'important':
        tasks = tasks.filter(t => t.priority === 'High');
        break;
    }

    if (query) {
      tasks = tasks.filter(t =>
        t.title.toLowerCase().includes(query) ||
        (t.description ?? '').toLowerCase().includes(query)
      );
    }

    return [...tasks].sort((a, b) => new Date(a.scheduledDate).getTime() - new Date(b.scheduledDate).getTime());
  });

  // ── Form modal (crear/editar) ────────────────────────
  showTaskForm = signal(false);
  editingTaskId = signal<number | null>(null);
  taskFormError = signal('');
  savingTask = signal(false);
  deleteTarget = signal<TaskItem | null>(null);

  taskStatuses: TaskStatus[] = ['Pending', 'InProgress', 'Completed', 'Skipped'];
  taskPriorities: TaskPriority[] = ['Low', 'Medium', 'High'];
  recurrenceTypes: RecurrenceType[] = ['None', 'Daily', 'Weekly', 'Monthly', 'Yearly'];

  pastelColors = ['#c7d2fe', '#bbf7d0', '#fecaca', '#fed7aa', '#fef08a', '#bae6fd', '#f5d0fe', '#e5e7eb'];
  emojis = TASK_EMOJIS;
  emojiPreviewCount = 20;
  showAllEmojis = signal(false);

  taskForm: FormGroup;

  // ── Categoría rápida (embebida en el form de tarea) ────
  showCategoryForm = signal(false);
  savingCategory = signal(false);
  categoryFormError = signal('');
  categoryForm: FormGroup;

  categoryIcons = [
    'label', 'work', 'school', 'fitness_center', 'restaurant', 'favorite',
    'home', 'savings', 'book', 'music_note', 'self_improvement',
    'medical_services', 'palette', 'code', 'sports_esports', 'pets',
    'flight', 'shopping_cart', 'brush', 'directions_run'
  ];
  categoryColorPresets = ['#4f46e5', '#7c3aed', '#dc2626', '#ea580c', '#d97706', '#16a34a', '#0891b2', '#2563eb', '#db2777', '#64748b'];

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private taskService: TaskService,
    private objectiveService: ObjectiveService,
    private categoryService: CategoryService
  ) {
    this.taskForm = this.fb.group({
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

    this.categoryForm = this.fb.group({
      name: ['', Validators.required],
      color: ['#4f46e5'],
      icon: ['label']
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    const userId = this.auth.getUserId();
    forkJoin({
      tasks: this.taskService.getAll(userId),
      objectives: this.objectiveService.getAll(userId),
      categories: this.categoryService.getAll(userId)
    }).subscribe({
      next: ({ tasks, objectives, categories }) => {
        this.allTasks.set(tasks);
        this.objectives.set(objectives);
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  setFilter(filter: TaskFilter): void {
    this.activeFilter.set(filter);
  }

  onSearchChange(value: string): void {
    this.searchQuery.set(value);
  }

  categoryOf(task: TaskItem): Category | undefined {
    return this.categories().find(c => c.id === task.categoryId);
  }

  objectiveOf(task: TaskItem): Objective | undefined {
    return this.objectives().find(o => o.id === task.objectiveId);
  }

  isOverdue(task: TaskItem): boolean {
    return isTaskOverdue(task, this.dateKey(new Date()), this.isDone(task));
  }

  isFuture(task: TaskItem): boolean {
    if (!task.scheduledDate) return false;
    const taskDate = this.parseDateKey(task.scheduledDate.substring(0, 10));
    const today = this.parseDateKey(this.dateKey(new Date()));
    return taskDate > today;
  }

  isDone(task: TaskItem, dateKey?: string): boolean {
    return isTaskDoneOn(task, dateKey ?? this.dateKey(new Date()));
  }

  taskPriorityLabel(priority: TaskPriority): string {
    switch (priority) {
      case 'Low': return 'Baja';
      case 'Medium': return 'Media';
      case 'High': return 'Alta';
    }
  }

  taskStatusLabel(status: TaskStatus): string {
    switch (status) {
      case 'Pending': return 'Pendiente';
      case 'InProgress': return 'En progreso';
      case 'Completed': return 'Completada';
      case 'Skipped': return 'Omitida';
    }
  }

  recurrenceLabel(type: RecurrenceType): string {
    switch (type) {
      case 'None': return 'No se repite';
      case 'Daily': return 'Diaria';
      case 'Weekly': return 'Semanal';
      case 'Monthly': return 'Mensual';
      case 'Yearly': return 'Anual';
    }
  }

  dateLabel(task: TaskItem): string {
    if (!task.scheduledDate) return '';
    const date = this.parseDateKey(task.scheduledDate.substring(0, 10));
    return date.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
  }

  toggleTaskComplete(task: TaskItem): void {
    const currentlyDone = this.isDone(task);
    if (!currentlyDone && this.isFuture(task)) return;
    const newStatus: TaskStatus = currentlyDone ? 'Pending' : 'Completed';
    // Las recurrentes son una sola fila con una unica scheduledDate: al completarla hay
    // que moverla a "hoy" para que la racha la vea como hecha. Usamos el hoy LOCAL del
    // navegador (no el del backend, que corre en UTC y puede ser otro dia distinto).
    const scheduledDate = (task.isRecurring && newStatus === 'Completed')
      ? this.dateKey(new Date())
      : task.scheduledDate;
    this.taskService.update(task.id, {
      title: task.title,
      description: task.description,
      emoji: task.emoji,
      color: task.color,
      scheduledDate,
      scheduledTime: task.scheduledTime,
      endTime: task.endTime,
      priority: task.priority,
      status: newStatus,
      isRecurring: task.isRecurring,
      recurrenceType: task.recurrenceType,
      categoryId: task.categoryId,
      objectiveId: task.objectiveId
    }).subscribe({
      next: updated => {
        const updatedTasks = this.allTasks().map(t => t.id === updated.id ? updated : t);
        this.allTasks.set(updatedTasks);
        if (updated.objectiveId) {
          this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
        }
      }
    });
  }

  // ── Categoría rápida ───────────────────────────────────
  openCategoryForm(): void {
    this.categoryFormError.set('');
    this.categoryForm.reset({ name: '', color: '#4f46e5', icon: 'label' });
    this.showCategoryForm.set(true);
  }

  closeCategoryForm(): void {
    this.showCategoryForm.set(false);
  }

  pickCategoryColor(color: string): void {
    this.categoryForm.patchValue({ color });
  }

  pickCategoryIcon(icon: string): void {
    this.categoryForm.patchValue({ icon });
  }

  submitCategory(): void {
    if (this.categoryForm.invalid) return;
    this.savingCategory.set(true);
    this.categoryFormError.set('');

    const v = this.categoryForm.value;
    this.categoryService.create({
      name: v.name,
      color: v.color,
      icon: v.icon
    }).subscribe({
      next: created => {
        this.categories.set([...this.categories(), created]);
        this.taskForm.patchValue({ categoryId: created.id });
        this.savingCategory.set(false);
        this.showCategoryForm.set(false);
      },
      error: (e) => {
        this.savingCategory.set(false);
        this.categoryFormError.set(mensajeDeError(e, 'No se pudo crear la categoría. Intenta de nuevo.'));
      }
    });
  }

  // ── Tareas: CRUD ──────────────────────────────────────
  pickTaskColor(color: string): void {
    this.taskForm.patchValue({ color });
  }

  pickTaskEmoji(emoji: string): void {
    this.taskForm.patchValue({ emoji });
  }

  openCreateTask(): void {
    this.editingTaskId.set(null);
    this.taskFormError.set('');
    this.showAllEmojis.set(false);
    this.taskForm.reset({
      title: '',
      description: '',
      emoji: '',
      color: '#c7d2fe',
      scheduledDate: this.dateKey(new Date()),
      scheduledTime: '',
      endTime: '',
      priority: 'Medium',
      categoryId: null,
      objectiveId: null,
      isRecurring: false,
      recurrenceType: 'None',
      status: 'Pending'
    });
    this.showTaskForm.set(true);
  }

  openEditTask(task: TaskItem): void {
    this.editingTaskId.set(task.id);
    this.taskFormError.set('');
    this.showAllEmojis.set(!!task.emoji && !this.emojis.slice(0, this.emojiPreviewCount).includes(task.emoji));
    this.taskForm.reset({
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
    this.showTaskForm.set(true);
  }

  closeTaskForm(): void {
    this.showTaskForm.set(false);
  }

  submitTask(): void {
    if (this.taskForm.invalid) return;
    this.savingTask.set(true);
    this.taskFormError.set('');

    const v = this.taskForm.value;
    const categoryId = v.categoryId ? Number(v.categoryId) : undefined;
    const objectiveId = v.objectiveId ? Number(v.objectiveId) : undefined;
    const scheduledTime = v.scheduledTime ? v.scheduledTime + ':00' : undefined;
    const endTime = v.endTime ? v.endTime + ':00' : undefined;
    const editingTaskId = this.editingTaskId();

    if (editingTaskId === null) {
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
          const updatedTasks = [...this.allTasks(), created];
          this.allTasks.set(updatedTasks);
          this.savingTask.set(false);
          this.showTaskForm.set(false);
          if (created.objectiveId) {
            this.recomputeObjectiveProgress(created.objectiveId, updatedTasks);
          }
        },
        error: (e) => {
          this.savingTask.set(false);
          this.taskFormError.set(mensajeDeError(e, 'No se pudo crear la tarea. Intenta de nuevo.'));
        }
      });
    } else {
      const previousObjectiveId = this.allTasks().find(t => t.id === editingTaskId)?.objectiveId;
      this.taskService.update(editingTaskId, {
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
        categoryId,
        objectiveId
      }).subscribe({
        next: updated => {
          const updatedTasks = this.allTasks().map(t => t.id === updated.id ? updated : t);
          this.allTasks.set(updatedTasks);
          this.savingTask.set(false);
          this.showTaskForm.set(false);
          if (updated.objectiveId) {
            this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
          }
          if (previousObjectiveId && previousObjectiveId !== updated.objectiveId) {
            this.recomputeObjectiveProgress(previousObjectiveId, updatedTasks);
          }
        },
        error: (e) => {
          this.savingTask.set(false);
          this.taskFormError.set(mensajeDeError(e, 'No se pudo guardar la tarea. Intenta de nuevo.'));
        }
      });
    }
  }

  askDelete(task: TaskItem): void {
    this.deleteTarget.set(task);
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.taskService.delete(target.id).subscribe({
      next: () => {
        const remaining = this.allTasks().filter(t => t.id !== target.id);
        this.allTasks.set(remaining);
        this.deleteTarget.set(null);
        if (target.objectiveId) {
          this.recomputeObjectiveProgress(target.objectiveId, remaining);
        }
      },
      error: () => {
        this.deleteTarget.set(null);
        this.loadError.set(true);
      }
    });
  }

  private recomputeObjectiveProgress(objectiveId: number, tasks: TaskItem[]): void {
    const linked = tasks.filter(t => t.objectiveId === objectiveId);
    if (linked.length === 0) return;

    const objective = this.objectives().find(o => o.id === objectiveId);
    if (!objective) return;

    const progreso = computeObjectiveProgress(objective, linked);
    if (!progreso) return;

    if (progreso.askIfFinished) {
      this.objetivoPorConfirmar.set({ objective, taskCount: progreso.taskCount });
    }
    if (!progreso.changed) return;

    this.guardarObjetivo(objective, {
      status: progreso.status,
      progressPercentage: progreso.progressPercentage
    });
  }

  // ── ¿Terminaste el objetivo? ─────────────────────────
  objetivoPorConfirmar = signal<{ objective: Objective; taskCount: number } | null>(null);

  confirmarObjetivoTerminado(): void {
    const pendiente = this.objetivoPorConfirmar();
    if (!pendiente) return;
    this.objetivoPorConfirmar.set(null);
    // Al confirmar, el objetivo si llega al 100%: es el unico caso donde
    // corresponde ese numero.
    this.guardarObjetivo(pendiente.objective, { status: 'Completed', progressPercentage: 100 });
  }

  posponerObjetivoTerminado(): void {
    const pendiente = this.objetivoPorConfirmar();
    if (!pendiente) return;
    this.objetivoPorConfirmar.set(null);
    this.guardarObjetivo(pendiente.objective, { completionAskedAtTaskCount: pendiente.taskCount });
  }

  private guardarObjetivo(objective: Objective, cambios: Partial<Objective>): void {
    this.objectiveService.update(objective.id, {
      title: objective.title,
      description: objective.description,
      categoryId: objective.categoryId,
      startDate: objective.startDate,
      endDate: objective.endDate,
      status: cambios.status ?? objective.status,
      progressPercentage: cambios.progressPercentage ?? objective.progressPercentage,
      completionAskedAtTaskCount:
        cambios.completionAskedAtTaskCount ?? objective.completionAskedAtTaskCount
    }).subscribe(updated => {
      this.objectives.set(this.objectives().map(o => o.id === updated.id ? updated : o));
    });
  }

  private dateKey(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private parseDateKey(key: string): Date {
    const [y, m, d] = key.split('-').map(Number);
    return new Date(y, m - 1, d);
  }
}
