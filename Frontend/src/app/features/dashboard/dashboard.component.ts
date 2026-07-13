import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { AuthService } from '../../core/services/auth.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskService } from '../../core/services/task.service';
import { CategoryService } from '../../core/services/category.service';
import { Objective, ObjectiveStatus } from '../../core/models/objective.model';
import { TaskItem, TaskStatus, TaskPriority, RecurrenceType } from '../../core/models/task.model';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BaseChartDirective, ConfirmDialogComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  name: string;
  loading = signal(true);
  loadError = signal(false);
  hasObjectives = signal(false);

  totalObjectives = signal(0);
  completedObjectives = signal(0);
  pendingTasks = signal(0);
  overdueTasks = signal(0);

  objectives = signal<Objective[]>([]);
  categories = signal<Category[]>([]);
  allTasks = signal<TaskItem[]>([]);

  chartData = signal<ChartData<'doughnut'>>({ labels: [], datasets: [{ data: [] }] });
  chartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } }
  };

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
      const dayTasks = tasks.filter(t => this.taskOccursOn(t, date));
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

  upcomingTasks = computed(() =>
    this.allTasks()
      .filter(t => t.status !== 'Completed' && t.status !== 'Skipped' && !!t.scheduledDate)
      .sort((a, b) => new Date(a.scheduledDate).getTime() - new Date(b.scheduledDate).getTime())
      .slice(0, 5)
  );

  selectedDayTasks = computed(() => {
    const key = this.selectedDate();
    if (!key) return [];
    const date = this.parseDateKey(key);
    return this.allTasks().filter(t => this.taskOccursOn(t, date));
  });

  weekRangeLabel = computed(() => {
    const start = this.weekCursor();
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    const fmt = (d: Date) => d.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' });
    return `${fmt(start)} – ${fmt(end)} ${end.getFullYear()}`;
  });

  // ── Calendario (vista mensual ampliada) ──────────────
  showBigCalendar = signal(false);
  monthCursor = signal(this.startOfMonth(new Date()));

  monthRangeLabel = computed(() =>
    this.monthCursor().toLocaleDateString('es-ES', { month: 'long', year: 'numeric' })
  );

  monthDays = computed(() => {
    const monthStart = this.monthCursor();
    const tasks = this.allTasks();
    const todayKey = this.dateKey(new Date());
    const gridStart = this.startOfWeek(monthStart);

    return Array.from({ length: 42 }, (_, i) => {
      const date = new Date(gridStart);
      date.setDate(date.getDate() + i);
      const key = this.dateKey(date);
      const dayTasks = tasks.filter(t => this.taskOccursOn(t, date));
      return {
        date,
        key,
        dayNumber: date.getDate(),
        isToday: key === todayKey,
        inMonth: date.getMonth() === monthStart.getMonth(),
        hasOverdue: dayTasks.some(t => this.isOverdue(t)),
        taskCount: dayTasks.length
      };
    });
  });

  openBigCalendar(): void {
    this.monthCursor.set(this.startOfMonth(this.weekCursor()));
    this.showBigCalendar.set(true);
  }

  closeBigCalendar(): void {
    this.showBigCalendar.set(false);
  }

  prevMonth(): void {
    const d = new Date(this.monthCursor());
    d.setMonth(d.getMonth() - 1);
    this.monthCursor.set(this.startOfMonth(d));
  }

  nextMonth(): void {
    const d = new Date(this.monthCursor());
    d.setMonth(d.getMonth() + 1);
    this.monthCursor.set(this.startOfMonth(d));
  }

  selectDayFromMonth(key: string): void {
    const date = this.parseDateKey(key);
    this.weekCursor.set(this.startOfWeek(date));
    this.selectedDate.set(key);
    this.showBigCalendar.set(false);
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  private taskOccursOn(task: TaskItem, date: Date): boolean {
    if (!task.scheduledDate) return false;
    const start = this.parseDateKey(task.scheduledDate.substring(0, 10));
    const day = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    if (day < start) return false;
    if (task.endRepeatDate) {
      const end = this.parseDateKey(task.endRepeatDate.substring(0, 10));
      if (day > end) return false;
    }
    if (!task.isRecurring || task.recurrenceType === 'None') {
      return day.getTime() === start.getTime();
    }
    const diffDays = Math.round((day.getTime() - start.getTime()) / 86400000);
    switch (task.recurrenceType) {
      case 'Daily':
        return true;
      case 'Weekly': {
        const weeks = task.repeatEveryWeeks && task.repeatEveryWeeks > 0 ? task.repeatEveryWeeks : 1;
        return diffDays % (7 * weeks) === 0;
      }
      case 'Monthly':
        return day.getDate() === start.getDate();
      case 'Yearly':
        return day.getDate() === start.getDate() && day.getMonth() === start.getMonth();
      default:
        return false;
    }
  }

  private parseDateKey(key: string): Date {
    const [y, m, d] = key.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  // ── Objetivos: form modal ────────────────────────────
  showForm = signal(false);
  editingId = signal<number | null>(null);
  formError = signal('');
  saving = signal(false);
  deleteTarget = signal<Objective | null>(null);
  statuses: ObjectiveStatus[] = ['Pending', 'InProgress', 'Completed', 'Cancelled'];

  form: FormGroup;

  // ── Tareas: form modal ───────────────────────────────
  showTaskForm = signal(false);
  editingTaskId = signal<number | null>(null);
  taskFormError = signal('');
  savingTask = signal(false);
  deleteTaskTarget = signal<TaskItem | null>(null);

  taskStatuses: TaskStatus[] = ['Pending', 'InProgress', 'Completed', 'Skipped'];
  taskPriorities: TaskPriority[] = ['Low', 'Medium', 'High'];
  recurrenceTypes: RecurrenceType[] = ['None', 'Daily', 'Weekly', 'Monthly', 'Yearly'];

  pastelColors = ['#c7d2fe', '#bbf7d0', '#fecaca', '#fed7aa', '#fef08a', '#bae6fd', '#f5d0fe', '#e5e7eb'];
  emojis = ['📌', '✅', '📅', '💪', '📚', '💼', '🏠', '🛒', '🎯', '💡', '🧹', '🍎', '💊', '🎨', '🎮', '✈️', '💰', '🧘', '🐾', '📝'];

  taskForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private objectiveService: ObjectiveService,
    private taskService: TaskService,
    private categoryService: CategoryService
  ) {
    this.name = this.auth.getName();
    this.form = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      categoryId: [null],
      startDate: [''],
      endDate: [''],
      status: ['Pending' as ObjectiveStatus],
      progressPercentage: [0]
    });

    this.form.get('status')?.valueChanges.subscribe(status => {
      if (status === 'Completed') {
        this.form.get('progressPercentage')?.setValue(100, { emitEvent: false });
      }
    });
    this.form.get('progressPercentage')?.valueChanges.subscribe(progress => {
      if (Number(progress) === 100 && this.form.get('status')?.value !== 'Cancelled') {
        this.form.get('status')?.setValue('Completed', { emitEvent: false });
      } else if (Number(progress) < 100 && this.form.get('status')?.value === 'Completed') {
        this.form.get('status')?.setValue('InProgress', { emitEvent: false });
      }
    });

    this.taskForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      emoji: [''],
      color: ['#c7d2fe'],
      scheduledDate: ['', Validators.required],
      priority: ['Medium' as TaskPriority],
      categoryId: [null],
      objectiveId: [null],
      isRecurring: [false],
      recurrenceType: ['None' as RecurrenceType],
      status: ['Pending' as TaskStatus]
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
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
        this.computeStats(objectives, tasks);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  private computeStats(objectives: Objective[], tasks: TaskItem[]): void {
    this.hasObjectives.set(objectives.length > 0);
    this.totalObjectives.set(objectives.length);

    const completed = objectives.filter(o => o.status === 'Completed').length;
    this.completedObjectives.set(completed);

    const today = new Date();
    this.pendingTasks.set(tasks.filter(t => t.status === 'Pending' || t.status === 'InProgress').length);
    this.overdueTasks.set(tasks.filter(t =>
      !!t.scheduledDate && new Date(t.scheduledDate) < today && t.status !== 'Completed'
    ).length);

    const pending = objectives.filter(o => o.status === 'Pending').length;
    const inProgress = objectives.filter(o => o.status === 'InProgress').length;
    const cancelled = objectives.filter(o => o.status === 'Cancelled').length;

    this.chartData.set({
      labels: ['Pendientes', 'En progreso', 'Completados', 'Cancelados'],
      datasets: [{
        data: [pending, inProgress, completed, cancelled],
        backgroundColor: ['#c7d2fe', '#818cf8', '#4f46e5', '#e5e7eb'],
        borderWidth: 0
      }]
    });
  }

  isOverdue(task: TaskItem): boolean {
    return !!task.scheduledDate && new Date(task.scheduledDate) < new Date();
  }

  toggleTaskComplete(task: TaskItem): void {
    const newStatus = task.status === 'Completed' ? 'Pending' : 'Completed';
    this.taskService.update(task.id, {
      title: task.title,
      description: task.description,
      emoji: task.emoji,
      color: task.color,
      scheduledDate: task.scheduledDate,
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
        this.computeStats(this.objectives(), updatedTasks);
        if (updated.objectiveId) {
          this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
        }
      }
    });
  }

  // ── Tareas: CRUD ──────────────────────────────────────
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

  pickTaskColor(color: string): void {
    this.taskForm.patchValue({ color });
  }

  pickTaskEmoji(emoji: string): void {
    this.taskForm.patchValue({ emoji });
  }

  openCreateTask(): void {
    this.editingTaskId.set(null);
    this.taskFormError.set('');
    this.taskForm.reset({
      title: '',
      description: '',
      emoji: '',
      color: '#c7d2fe',
      scheduledDate: this.selectedDate() ?? this.dateKey(new Date()),
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
    this.taskForm.reset({
      title: task.title,
      description: task.description ?? '',
      emoji: task.emoji ?? '',
      color: task.color ?? '#c7d2fe',
      scheduledDate: task.scheduledDate?.substring(0, 10) ?? '',
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
    const editingTaskId = this.editingTaskId();

    if (editingTaskId === null) {
      this.taskService.create({
        title: v.title,
        description: v.description || undefined,
        emoji: v.emoji || undefined,
        color: v.color || undefined,
        scheduledDate: v.scheduledDate,
        priority: v.priority,
        isRecurring: v.isRecurring,
        recurrenceType: v.recurrenceType,
        categoryId,
        objectiveId
      }, this.auth.getUserId()).subscribe({
        next: created => {
          const updatedTasks = [...this.allTasks(), created];
          this.allTasks.set(updatedTasks);
          this.computeStats(this.objectives(), updatedTasks);
          this.savingTask.set(false);
          this.showTaskForm.set(false);
          if (created.objectiveId) {
            this.recomputeObjectiveProgress(created.objectiveId, updatedTasks);
          }
        },
        error: () => {
          this.savingTask.set(false);
          this.taskFormError.set('No se pudo crear la tarea. Intenta de nuevo.');
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
          this.computeStats(this.objectives(), updatedTasks);
          this.savingTask.set(false);
          this.showTaskForm.set(false);
          if (updated.objectiveId) {
            this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
          }
          if (previousObjectiveId && previousObjectiveId !== updated.objectiveId) {
            this.recomputeObjectiveProgress(previousObjectiveId, updatedTasks);
          }
        },
        error: () => {
          this.savingTask.set(false);
          this.taskFormError.set('No se pudo guardar la tarea. Intenta de nuevo.');
        }
      });
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
        this.computeStats(this.objectives(), remaining);
        this.deleteTaskTarget.set(null);
        if (target.objectiveId) {
          this.recomputeObjectiveProgress(target.objectiveId, remaining);
        }
      },
      error: () => {
        this.deleteTaskTarget.set(null);
        this.loadError.set(true);
      }
    });
  }

  private recomputeObjectiveProgress(objectiveId: number, tasks: TaskItem[]): void {
    const linked = tasks.filter(t => t.objectiveId === objectiveId);
    if (linked.length === 0) return;

    const objective = this.objectives().find(o => o.id === objectiveId);
    if (!objective) return;

    const completed = linked.filter(t => t.status === 'Completed').length;
    const progressPercentage = Math.round((completed / linked.length) * 100);
    if (objective.progressPercentage === progressPercentage) return;

    let status = objective.status;
    if (status !== 'Cancelled') {
      status = progressPercentage === 100 ? 'Completed' : progressPercentage > 0 ? 'InProgress' : 'Pending';
    }

    this.objectiveService.update(objectiveId, {
      title: objective.title,
      description: objective.description,
      categoryId: objective.categoryId,
      startDate: objective.startDate,
      endDate: objective.endDate,
      status,
      progressPercentage
    }).subscribe(updated => {
      const updatedObjectives = this.objectives().map(o => o.id === updated.id ? updated : o);
      this.objectives.set(updatedObjectives);
      this.computeStats(updatedObjectives, this.allTasks());
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

  // ── Objetivos: CRUD ──────────────────────────────────
  categoryOf(objective: Objective): Category | undefined {
    return this.categories().find(c => c.id === objective.categoryId);
  }

  statusLabel(status: ObjectiveStatus): string {
    switch (status) {
      case 'Pending': return 'Pendiente';
      case 'InProgress': return 'En progreso';
      case 'Completed': return 'Completado';
      case 'Cancelled': return 'Cancelado';
    }
  }

  openCreate(): void {
    this.editingId.set(null);
    this.formError.set('');
    this.form.reset({
      title: '',
      description: '',
      categoryId: null,
      startDate: '',
      endDate: '',
      status: 'Pending',
      progressPercentage: 0
    });
    this.showForm.set(true);
  }

  openEdit(objective: Objective): void {
    this.editingId.set(objective.id);
    this.formError.set('');
    this.form.reset({
      title: objective.title,
      description: objective.description ?? '',
      categoryId: objective.categoryId ?? null,
      startDate: objective.startDate?.substring(0, 10) ?? '',
      endDate: objective.endDate?.substring(0, 10) ?? '',
      status: objective.status,
      progressPercentage: objective.progressPercentage
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
    const categoryId = v.categoryId ? Number(v.categoryId) : undefined;
    const editingId = this.editingId();

    if (editingId === null) {
      this.objectiveService.create({
        title: v.title,
        description: v.description || undefined,
        categoryId,
        startDate: v.startDate || undefined,
        endDate: v.endDate || undefined
      }, this.auth.getUserId()).subscribe({
        next: created => {
          const updated = [...this.objectives(), created];
          this.objectives.set(updated);
          this.computeStats(updated, this.allTasks());
          this.saving.set(false);
          this.showForm.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo crear el objetivo. Intenta de nuevo.');
        }
      });
    } else {
      this.objectiveService.update(editingId, {
        title: v.title,
        description: v.description || undefined,
        categoryId,
        startDate: v.startDate || undefined,
        endDate: v.endDate || undefined,
        status: v.status,
        progressPercentage: Number(v.progressPercentage)
      }).subscribe({
        next: updatedObj => {
          const updated = this.objectives().map(o => o.id === updatedObj.id ? updatedObj : o);
          this.objectives.set(updated);
          this.computeStats(updated, this.allTasks());
          this.saving.set(false);
          this.showForm.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo guardar el objetivo. Intenta de nuevo.');
        }
      });
    }
  }

  askDelete(objective: Objective): void {
    this.deleteTarget.set(objective);
  }

  cancelDelete(): void {
    this.deleteTarget.set(null);
  }

  confirmDelete(): void {
    const target = this.deleteTarget();
    if (!target) return;
    this.objectiveService.delete(target.id).subscribe({
      next: () => {
        const updated = this.objectives().filter(o => o.id !== target.id);
        this.objectives.set(updated);
        this.computeStats(updated, this.allTasks());
        this.deleteTarget.set(null);
      },
      error: () => {
        this.deleteTarget.set(null);
        this.loadError.set(true);
      }
    });
  }
}
