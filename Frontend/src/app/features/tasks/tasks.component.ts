import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { TaskService } from '../../core/services/task.service';
import { CategoryService } from '../../core/services/category.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskItem, TaskStatus, TaskPriority, RecurrenceType } from '../../core/models/task.model';
import { Category } from '../../core/models/category.model';
import { Objective } from '../../core/models/objective.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

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

  tasks = signal<TaskItem[]>([]);
  categories = signal<Category[]>([]);
  objectives = signal<Objective[]>([]);

  showForm = signal(false);
  editingId = signal<number | null>(null);
  formError = signal('');
  saving = signal(false);
  deleteTarget = signal<TaskItem | null>(null);

  statuses: TaskStatus[] = ['Pending', 'InProgress', 'Completed', 'Skipped'];
  priorities: TaskPriority[] = ['Low', 'Medium', 'High'];
  recurrenceTypes: RecurrenceType[] = ['None', 'Daily', 'Weekly', 'Monthly', 'Yearly'];

  pastelColors = ['#c7d2fe', '#bbf7d0', '#fecaca', '#fed7aa', '#fef08a', '#bae6fd', '#f5d0fe', '#e5e7eb'];
  emojis = ['📌', '✅', '📅', '💪', '📚', '💼', '🏠', '🛒', '🎯', '💡', '🧹', '🍎', '💊', '🎨', '🎮', '✈️', '💰', '🧘', '🐾', '📝'];

  highlightedTaskId = signal<number | null>(null);

  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private taskService: TaskService,
    private categoryService: CategoryService,
    private objectiveService: ObjectiveService,
    private route: ActivatedRoute
  ) {
    this.form = this.fb.group({
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
      tasks: this.taskService.getAll(userId),
      categories: this.categoryService.getAll(userId),
      objectives: this.objectiveService.getAll(userId)
    }).subscribe({
      next: ({ tasks, categories, objectives }) => {
        this.tasks.set(this.sortTasks(tasks));
        this.categories.set(categories);
        this.objectives.set(objectives);
        this.loading.set(false);
        this.highlightFromQueryParam();
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  private highlightFromQueryParam(): void {
    const taskIdParam = this.route.snapshot.queryParamMap.get('taskId');
    if (!taskIdParam) return;
    const id = Number(taskIdParam);
    this.highlightedTaskId.set(id);
    setTimeout(() => {
      document.getElementById('task-' + id)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
    setTimeout(() => this.highlightedTaskId.set(null), 2500);
  }

  pickColor(color: string): void {
    this.form.patchValue({ color });
  }

  pickEmoji(emoji: string): void {
    this.form.patchValue({ emoji });
  }

  private sortTasks(tasks: TaskItem[]): TaskItem[] {
    return [...tasks].sort((a, b) => new Date(a.scheduledDate).getTime() - new Date(b.scheduledDate).getTime());
  }

  categoryOf(task: TaskItem): Category | undefined {
    return this.categories().find(c => c.id === task.categoryId);
  }

  objectiveOf(task: TaskItem): Objective | undefined {
    return this.objectives().find(o => o.id === task.objectiveId);
  }

  isOverdue(task: TaskItem): boolean {
    return task.status !== 'Completed' && task.status !== 'Skipped' &&
      new Date(task.scheduledDate) < new Date(new Date().toDateString());
  }

  statusLabel(status: TaskStatus): string {
    switch (status) {
      case 'Pending': return 'Pendiente';
      case 'InProgress': return 'En progreso';
      case 'Completed': return 'Completada';
      case 'Skipped': return 'Omitida';
    }
  }

  priorityLabel(priority: TaskPriority): string {
    switch (priority) {
      case 'Low': return 'Baja';
      case 'Medium': return 'Media';
      case 'High': return 'Alta';
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

  openCreate(): void {
    this.editingId.set(null);
    this.formError.set('');
    this.form.reset({
      title: '',
      description: '',
      emoji: '',
      color: '#c7d2fe',
      scheduledDate: '',
      priority: 'Medium',
      categoryId: null,
      objectiveId: null,
      isRecurring: false,
      recurrenceType: 'None',
      status: 'Pending'
    });
    this.showForm.set(true);
  }

  openEdit(task: TaskItem): void {
    this.editingId.set(task.id);
    this.formError.set('');
    this.form.reset({
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
    const objectiveId = v.objectiveId ? Number(v.objectiveId) : undefined;
    const editingId = this.editingId();

    if (editingId === null) {
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
          this.tasks.set(this.sortTasks([...this.tasks(), created]));
          this.saving.set(false);
          this.showForm.set(false);
          if (created.objectiveId) {
            this.recomputeObjectiveProgress(created.objectiveId, this.tasks());
          }
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo crear la tarea. Intenta de nuevo.');
        }
      });
    } else {
      const previousObjectiveId = this.tasks().find(t => t.id === editingId)?.objectiveId;
      this.taskService.update(editingId, {
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
          const updatedTasks = this.sortTasks(this.tasks().map(t => t.id === updated.id ? updated : t));
          this.tasks.set(updatedTasks);
          this.saving.set(false);
          this.showForm.set(false);
          if (updated.objectiveId) {
            this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
          }
          if (previousObjectiveId && previousObjectiveId !== updated.objectiveId) {
            this.recomputeObjectiveProgress(previousObjectiveId, updatedTasks);
          }
        },
        error: () => {
          this.saving.set(false);
          this.formError.set('No se pudo guardar la tarea. Intenta de nuevo.');
        }
      });
    }
  }

  private recomputeObjectiveProgress(objectiveId: number, tasks: TaskItem[]): void {
    const linked = tasks.filter(t => t.objectiveId === objectiveId);
    const objective = this.objectives().find(o => o.id === objectiveId);
    if (!objective) return;

    const progressPercentage = linked.length === 0
      ? 0
      : Math.round((linked.filter(t => t.status === 'Completed').length / linked.length) * 100);
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
      this.objectives.set(this.objectives().map(o => o.id === updated.id ? updated : o));
    });
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
        const remaining = this.tasks().filter(t => t.id !== target.id);
        this.tasks.set(remaining);
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
}
