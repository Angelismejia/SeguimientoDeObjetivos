import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TaskService } from '../../core/services/task.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { CategoryService } from '../../core/services/category.service';
import { TaskItem, TaskStatus } from '../../core/models/task.model';
import { Objective } from '../../core/models/objective.model';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { TaskFormDialogComponent, TaskSaved } from '../../shared/components/task-form-dialog/task-form-dialog.component';
import { recurrenceLabel, taskPriorityLabel } from '../../core/utils/task-labels';
import { isTaskDoneOn, isTaskOverdue } from '../../core/utils/task-status.util';
import { cuerpoDeActualizacion } from '../../core/utils/task-update.util';
import { computeObjectiveProgress } from '../../core/utils/objective-progress.util';
import { mensajeDeError } from '../../core/utils/http-error.util';

type TaskFilter = 'all' | 'today' | 'upcoming' | 'recurring' | 'completed' | 'important';

@Component({
  selector: 'app-tasks',
  standalone: true,
  imports: [CommonModule, ConfirmDialogComponent, TaskFormDialogComponent],
  templateUrl: './tasks.component.html',
  styleUrl: './tasks.component.css'
})
export class TasksComponent implements OnInit {
  loading = signal(true);
  loadError = signal('');

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

  // ── Tareas: form modal (shared/components/task-form-dialog) ──
  showTaskForm = signal(false);
  /** La tarea que se esta editando; null cuando el formulario crea una nueva. */
  editingTask = signal<TaskItem | null>(null);
  deleteTarget = signal<TaskItem | null>(null);

  taskPriorityLabel = taskPriorityLabel;
  recurrenceLabel = recurrenceLabel;

  constructor(
    private auth: AuthService,
    private taskService: TaskService,
    private objectiveService: ObjectiveService,
    private categoryService: CategoryService
  ) {
  }

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set('');
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
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudieron cargar las tareas.'));
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

  dateLabel(task: TaskItem): string {
    if (!task.scheduledDate) return '';
    const date = this.parseDateKey(task.scheduledDate.substring(0, 10));
    return date.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
  }

  toggleTaskComplete(task: TaskItem): void {
    const currentlyDone = this.isDone(task);
    if (!currentlyDone && this.isFuture(task)) return;
    const newStatus: TaskStatus = currentlyDone ? 'Pending' : 'Completed';

    // Igual que en el dashboard: las recurrentes guardan un registro por dia en
    // TaskCompletions y la fila de la tarea no se toca. Antes se le movia la
    // scheduledDate a hoy, lo que la borraba del calendario en los dias anteriores.
    // El dia es el hoy LOCAL del navegador (el backend corre en UTC).
    if (task.isRecurring) {
      const dia = this.dateKey(new Date());
      this.taskService.setCompletion(task.id, dia, !currentlyDone).subscribe({
        next: () => {
          const dias = new Set(task.completedDates ?? []);
          if (currentlyDone) dias.delete(dia); else dias.add(dia);
          const actualizada = { ...task, completedDates: [...dias].sort() };
          const tareas = this.allTasks().map(t => t.id === task.id ? actualizada : t);
          this.allTasks.set(tareas);
          if (task.objectiveId) this.recomputeObjectiveProgress(task.objectiveId, tareas);
        }
      });
      return;
    }

    this.taskService.update(task.id,
      cuerpoDeActualizacion(task, { status: newStatus })).subscribe({
      next: updated => {
        const updatedTasks = this.allTasks().map(t => t.id === updated.id ? updated : t);
        this.allTasks.set(updatedTasks);
        if (updated.objectiveId) {
          this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
        }
      }
    });
  }

  // ── Tareas: CRUD ──────────────────────────────────────
  openCreateTask(): void {
    this.editingTask.set(null);
    this.showTaskForm.set(true);
  }

  openEditTask(task: TaskItem): void {
    this.editingTask.set(task);
    this.showTaskForm.set(true);
  }

  closeTaskForm(): void {
    this.showTaskForm.set(false);
  }

  /** Dia que el formulario propone al crear una tarea. */
  newTaskDate(): string {
    return this.dateKey(new Date());
  }

  onCategoryCreated(category: Category): void {
    this.categories.set([...this.categories(), category]);
  }

  // El formulario ya guardo: aca solo se actualiza la lista y el progreso de
  // los objetivos afectados (el nuevo y, si se cambio, el anterior).
  onTaskSaved({ task, previousObjectiveId }: TaskSaved): void {
    const existe = this.allTasks().some(t => t.id === task.id);
    const updatedTasks = existe
      ? this.allTasks().map(t => t.id === task.id ? task : t)
      : [...this.allTasks(), task];
    this.allTasks.set(updatedTasks);
    this.showTaskForm.set(false);

    if (task.objectiveId) {
      this.recomputeObjectiveProgress(task.objectiveId, updatedTasks);
    }
    if (previousObjectiveId && previousObjectiveId !== task.objectiveId) {
      this.recomputeObjectiveProgress(previousObjectiveId, updatedTasks);
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
      error: (e) => {
        this.deleteTarget.set(null);
        this.loadError.set(mensajeDeError(e, 'No se pudo borrar la tarea.'));
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
    }).subscribe({
      next: updated => {
        this.objectives.set(this.objectives().map(o => o.id === updated.id ? updated : o));
      },
      // Esto es un guardado de fondo: el usuario marco una tarea, no pidio editar
      // un objetivo. Sin este handler el error subia al GlobalErrorHandler y
      // pintaba el banner rojo de diagnostico encima de la pantalla, con un texto
      // sobre campos que la persona ni habia tocado. El progreso se recalcula solo
      // la proxima vez, asi que alcanza con dejar rastro en la consola.
      error: err => console.error('No se pudo sincronizar el progreso del objetivo', err)
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
