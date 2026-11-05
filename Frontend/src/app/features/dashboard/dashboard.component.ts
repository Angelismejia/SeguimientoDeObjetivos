import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskService } from '../../core/services/task.service';
import { CategoryService } from '../../core/services/category.service';
import { Objective, ObjectiveStatus } from '../../core/models/objective.model';
import { TaskItem } from '../../core/models/task.model';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { TaskFormDialogComponent, TaskSaved } from '../../shared/components/task-form-dialog/task-form-dialog.component';
import { recurrenceLabel, taskPriorityLabel } from '../../core/utils/task-labels';
import { completedDayKeys, isTaskDoneOn, isTaskOverdue } from '../../core/utils/task-status.util';
import { taskOccursOn } from '../../core/utils/task-recurrence.util';
import { cuerpoDeActualizacion } from '../../core/utils/task-update.util';
import { computeObjectiveProgress } from '../../core/utils/objective-progress.util';
import { mensajeDeError } from '../../core/utils/http-error.util';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ConfirmDialogComponent, TaskFormDialogComponent, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  name: string;
  firstName: string;
  loading = signal(true);
  loadError = signal('');

  objectives = signal<Objective[]>([]);
  categories = signal<Category[]>([]);
  allTasks = signal<TaskItem[]>([]);

  // "Estado de tus objetivos": antes se dibujaba con un doughnut de chart.js que
  // dejaba mucho espacio vacio en movil. Se reemplazo por un resumen compacto
  // (mismos conteos reales, solo cambia como se muestran) — ver dashboard.component.html.
  objectiveStatusSummary = computed(() => {
    const objs = this.objectives();
    const total = objs.length;
    const pct = (n: number) => total > 0 ? Math.round((n / total) * 100) : 0;
    const counts = {
      Pending: objs.filter(o => o.status === 'Pending').length,
      InProgress: objs.filter(o => o.status === 'InProgress').length,
      Completed: objs.filter(o => o.status === 'Completed').length,
      Cancelled: objs.filter(o => o.status === 'Cancelled').length
    };
    return {
      total,
      items: [
        { key: 'Pending', label: 'Pendientes', dotClass: 'dot-pending', count: counts.Pending, pct: pct(counts.Pending) },
        { key: 'InProgress', label: 'En progreso', dotClass: 'dot-inprogress', count: counts.InProgress, pct: pct(counts.InProgress) },
        { key: 'Completed', label: 'Completados', dotClass: 'dot-completed', count: counts.Completed, pct: pct(counts.Completed) },
        { key: 'Cancelled', label: 'Cancelados', dotClass: 'dot-cancelled', count: counts.Cancelled, pct: pct(counts.Cancelled) }
      ]
    };
  });

  // "Resumen de hoy": reutiliza exactamente los mismos datos que ya calculaba
  // todayProgress()/streak() — solo agrega la lectura de "pendientes" (lo que
  // falta) al lado de lo que ya se completo hoy.
  todaySummary = computed(() => {
    const { done, total } = this.todayProgress();
    return { pending: total - done, completed: done, streakDays: this.streak() };
  });

  private static readonly OBJECTIVE_STATUS_LABEL: Record<ObjectiveStatus, string> = {
    Pending: 'Pendiente',
    InProgress: 'En progreso',
    Completed: 'Completado',
    Cancelled: 'Cancelado'
  };

  // "Objetivos recientes": los ultimos creados que siguen vigentes (no
  // cancelados), para que el usuario vea a que le esta metiendo mano ahora.
  recentObjectives = computed(() =>
    [...this.objectives()]
      .filter(o => o.status !== 'Cancelled')
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 4)
      .map(o => ({ ...o, statusLabel: DashboardComponent.OBJECTIVE_STATUS_LABEL[o.status] }))
  );

  // ── Calendario (vista semanal) ──────────────────────
  weekCursor = signal(this.startOfWeek(new Date()));
  selectedDate = signal<string | null>(this.dateKey(new Date()));

  weekDays = computed(() => {
    const weekStart = this.weekCursor();
    const tasks = this.allTasks();
    const todayKey = this.dateKey(new Date());

    return Array.from({ length: 7 }, (_, i) => {
      const date = new Date(weekStart);
      date.setDate(date.getDate() + i);
      const key = this.dateKey(date);
      const dayTasks = tasks.filter(t => taskOccursOn(t, date));
      return {
        date,
        key,
        dayNumber: date.getDate(),
        weekdayShort: date.toLocaleDateString('es-ES', { weekday: 'short' }),
        isToday: key === todayKey,
        hasOverdue: dayTasks.some(t => this.isOverdue(t)),
        taskCount: dayTasks.length
      };
    });
  });

  streak = computed(() => {
    // Cada tarea aporta todos sus dias hechos, no uno solo: una recurrente marcada
    // el lunes, martes y miercoles suma los tres. Antes se leia su unica
    // scheduledDate, asi que jamas podia aportar mas de un dia y la racha se
    // reiniciaba sola.
    const completedDays = new Set(this.allTasks().flatMap(completedDayKeys));

    let cursor = this.dateKey(new Date());
    if (!completedDays.has(cursor)) {
      cursor = this.dateKey(this.addDays(new Date(), -1));
    }
    let streak = 0;
    while (completedDays.has(cursor)) {
      streak++;
      cursor = this.dateKey(this.addDays(this.parseDateKey(cursor), -1));
    }
    return streak;
  });

  private addDays(date: Date, days: number): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    d.setDate(d.getDate() + days);
    return d;
  }

  // ── "Hoy": lo primero que se ve al entrar ─────────────
  todayTasks = computed(() => {
    const today = new Date();
    return this.allTasks()
      .filter(t => taskOccursOn(t, today) && t.status !== 'Skipped')
      .sort((a, b) => (a.scheduledTime ?? '99:99').localeCompare(b.scheduledTime ?? '99:99'));
  });

  todayProgress = computed(() => {
    const todayKey = this.dateKey(new Date());
    const tasks = this.todayTasks();
    const done = tasks.filter(t => isTaskDoneOn(t, todayKey)).length;
    const total = tasks.length;
    return { done, total, pct: total > 0 ? Math.round((done / total) * 100) : 0 };
  });

  featuredObjective = computed(() => {
    const objs = this.objectives();
    if (objs.length === 0) return null;

    const marked = objs.find(o => o.isPrimary);
    if (marked) return marked;

    // Todavía no marcaste ninguno como principal: mostramos el más relevante
    // como sugerencia (podés fijar uno de verdad desde Objetivos).
    const inProgress = objs.filter(o => o.status === 'InProgress');
    const pool = inProgress.length > 0 ? inProgress : objs.filter(o => o.status === 'Pending');
    const candidates = pool.length > 0 ? pool : objs;
    return [...candidates].sort((a, b) => b.progressPercentage - a.progressPercentage)[0] ?? null;
  });

  objectiveCaption(progressPercentage: number): string {
    if (progressPercentage >= 70) return 'Estás avanzando muy bien 🚀';
    if (progressPercentage >= 35) return 'Vas por buen camino 💪';
    return 'Es un buen momento para retomarlo ✨';
  }

  // ── Estadísticas rápidas ───────────────────────────────
  activeObjectivesCount = computed(() =>
    this.objectives().filter(o => o.status !== 'Completed' && o.status !== 'Cancelled').length
  );

  completedTasksCount = computed(() =>
    this.allTasks().filter(t => t.status === 'Completed').length
  );

  avgProgress = computed(() => {
    const objs = this.objectives();
    if (objs.length === 0) return 0;
    return Math.round(objs.reduce((sum, o) => sum + o.progressPercentage, 0) / objs.length);
  });

  isTodaySelected(): boolean {
    return this.selectedDate() === this.dateKey(new Date());
  }

  todayLabel(): string {
    return new Date().toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
  }

  taskDurationLabel(task: TaskItem): string | null {
    if (!task.scheduledTime || !task.endTime) return null;
    const [sh, sm] = task.scheduledTime.split(':').map(Number);
    const [eh, em] = task.endTime.split(':').map(Number);
    const minutes = (eh * 60 + em) - (sh * 60 + sm);
    if (minutes <= 0) return null;
    return minutes >= 60 ? `${Math.round(minutes / 60)} h` : `${minutes} minutos`;
  }

  selectedDayTasks = computed(() => {
    const key = this.selectedDate();
    if (!key) return [];
    const date = this.parseDateKey(key);
    return this.allTasks().filter(t => taskOccursOn(t, date));
  });

  weekRangeLabel = computed(() => {
    const start = this.weekCursor();
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    const fmt = (d: Date) => d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
    return `${fmt(start)} – ${fmt(end)} ${end.getFullYear()}`;
  });


  private parseDateKey(key: string): Date {
    const [y, m, d] = key.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  // ── Tareas: form modal (shared/components/task-form-dialog) ──
  showTaskForm = signal(false);
  /** La tarea que se esta editando; null cuando el formulario crea una nueva. */
  editingTask = signal<TaskItem | null>(null);
  deleteTaskTarget = signal<TaskItem | null>(null);

  taskPriorityLabel = taskPriorityLabel;
  recurrenceLabel = recurrenceLabel;

  constructor(
    private auth: AuthService,
    private objectiveService: ObjectiveService,
    private taskService: TaskService,
    private categoryService: CategoryService
  ) {
    this.name = this.auth.getName();
    this.firstName = this.name.split(' ')[0] || this.name;

  }

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set('');
    const userId = this.auth.getUserId();
    forkJoin({
      objectives: this.objectiveService.getAll(userId),
      tasks: this.taskService.getAll(userId),
      categories: this.categoryService.getAll(userId)
    }).subscribe({
      next: ({ objectives, tasks, categories }) => {
        this.objectives.set(objectives);
        this.allTasks.set(tasks);
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudo cargar tu progreso.'));
      }
    });
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

  timeRangeLabel(task: TaskItem): string {
    if (!task.scheduledTime) return '';
    const start = task.scheduledTime.substring(0, 5);
    return task.endTime ? `${start} - ${task.endTime.substring(0, 5)}` : start;
  }

  toggleTaskComplete(task: TaskItem, dateKey?: string): void {
    const currentlyDone = this.isDone(task, dateKey);
    if (!currentlyDone && this.isFuture(task)) return;
    const newStatus = currentlyDone ? 'Pending' : 'Completed';

    // Las recurrentes guardan un registro por dia en TaskCompletions, asi que no se
    // toca la fila de la tarea: solo se agrega o quita el dia que se esta marcando.
    // Antes se le movia la scheduledDate a ese dia, lo que ademas la hacia
    // desaparecer del calendario en todas las fechas anteriores.
    //
    // El dia sale de dateKey (p.ej. una fecha pasada elegida en el calendario) o,
    // si no llega, del hoy LOCAL del navegador: el backend corre en UTC y puede
    // estar en otro dia.
    if (task.isRecurring) {
      const dia = dateKey ?? this.dateKey(new Date());
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
    return this.selectedDate() ?? this.dateKey(new Date());
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

  askDeleteTask(task: TaskItem): void {
    this.deleteTaskTarget.set(task);
  }

  cancelDeleteTask(): void {
    this.deleteTaskTarget.set(null);
  }

  confirmDeleteTask(): void {
    const target = this.deleteTaskTarget();
    if (!target) return;
    this.taskService.delete(target.id).subscribe({
      next: () => {
        const remaining = this.allTasks().filter(t => t.id !== target.id);
        this.allTasks.set(remaining);
        this.deleteTaskTarget.set(null);
        if (target.objectiveId) {
          this.recomputeObjectiveProgress(target.objectiveId, remaining);
        }
      },
      error: (e) => {
        this.deleteTaskTarget.set(null);
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

  prevWeek(): void {
    const d = new Date(this.weekCursor());
    d.setDate(d.getDate() - 7);
    this.weekCursor.set(d);
  }

  nextWeek(): void {
    const d = new Date(this.weekCursor());
    d.setDate(d.getDate() + 7);
    this.weekCursor.set(d);
  }

  goToday(): void {
    this.weekCursor.set(this.startOfWeek(new Date()));
    this.selectedDate.set(this.dateKey(new Date()));
  }

  selectDay(key: string): void {
    this.selectedDate.set(this.selectedDate() === key ? null : key);
  }

  private startOfWeek(date: Date): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const offset = (d.getDay() + 6) % 7; // Monday-first
    d.setDate(d.getDate() - offset);
    return d;
  }

  private dateKey(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

}
