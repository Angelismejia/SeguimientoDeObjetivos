import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskService } from '../../core/services/task.service';
import { CategoryService } from '../../core/services/category.service';
import { Objective, ObjectiveStatus } from '../../core/models/objective.model';
import { TaskItem } from '../../core/models/task.model';
import { Category } from '../../core/models/category.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

interface HeatmapCell {
  key: string;
  completed: boolean;
}

@Component({
  selector: 'app-objectives',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ConfirmDialogComponent],
  templateUrl: './objectives.component.html',
  styleUrl: './objectives.component.css'
})
export class ObjectivesComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);

  objectives = signal<Objective[]>([]);
  categories = signal<Category[]>([]);
  allTasks = signal<TaskItem[]>([]);

  // ── Form modal (crear/editar) ────────────────────────
  showForm = signal(false);
  editingId = signal<number | null>(null);
  formError = signal('');
  saving = signal(false);
  deleteTarget = signal<Objective | null>(null);
  statuses: ObjectiveStatus[] = ['Pending', 'InProgress', 'Completed', 'Cancelled'];

  form: FormGroup;

  // ── Detalle ───────────────────────────────────────────
  viewingObjectiveId = signal<number | null>(null);

  viewingObjective = computed(() =>
    this.objectives().find(o => o.id === this.viewingObjectiveId()) ?? null
  );

  viewingObjectiveTasks = computed(() => {
    const id = this.viewingObjectiveId();
    if (id === null) return [];
    return this.allTasks()
      .filter(t => t.objectiveId === id)
      .sort((a, b) => new Date(a.scheduledDate).getTime() - new Date(b.scheduledDate).getTime());
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private objectiveService: ObjectiveService,
    private taskService: TaskService,
    private categoryService: CategoryService
  ) {
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
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

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

  isOverdue(task: TaskItem): boolean {
    return task.status !== 'Completed' && task.status !== 'Skipped' &&
      new Date(task.scheduledDate) < new Date(new Date().toDateString());
  }

  // ── Racha y mapa de calor ─────────────────────────────
  private completedDaysFor(objectiveId: number): Set<string> {
    return new Set(
      this.allTasks()
        .filter(t => t.objectiveId === objectiveId && t.status === 'Completed' && !!t.scheduledDate)
        .map(t => t.scheduledDate.substring(0, 10))
    );
  }

  streakFor(objectiveId: number): number {
    const days = this.completedDaysFor(objectiveId);
    let cursor = this.dateKey(new Date());
    if (!days.has(cursor)) {
      cursor = this.dateKey(this.addDays(new Date(), -1));
    }
    let streak = 0;
    while (days.has(cursor)) {
      streak++;
      cursor = this.dateKey(this.addDays(this.parseDateKey(cursor), -1));
    }
    return streak;
  }

  heatmapFor(objectiveId: number): HeatmapCell[] {
    const days = this.completedDaysFor(objectiveId);
    const today = new Date();
    const cells: HeatmapCell[] = [];
    for (let i = 34; i >= 0; i--) {
      const key = this.dateKey(this.addDays(today, -i));
      cells.push({ key, completed: days.has(key) });
    }
    return cells;
  }

  // ── Mapa de calor: click para ver la fecha ────────────
  selectedCell = signal<{ objectiveId: number; key: string } | null>(null);

  selectCell(objectiveId: number, cell: HeatmapCell, event: Event): void {
    event.stopPropagation();
    const current = this.selectedCell();
    if (current && current.objectiveId === objectiveId && current.key === cell.key) {
      this.selectedCell.set(null);
      return;
    }
    this.selectedCell.set({ objectiveId, key: cell.key });
  }

  selectedCellLabel(objectiveId: number): string | null {
    const sel = this.selectedCell();
    if (!sel || sel.objectiveId !== objectiveId) return null;
    const date = this.parseDateKey(sel.key);
    const completed = this.completedDaysFor(objectiveId).has(sel.key);
    const dateLabel = date.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
    return `${dateLabel} — ${completed ? 'completado ✅' : 'sin actividad'}`;
  }

  private addDays(date: Date, days: number): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    d.setDate(d.getDate() + days);
    return d;
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

  // ── Detalle: abrir/cerrar ─────────────────────────────
  openObjectiveDetail(objective: Objective): void {
    this.viewingObjectiveId.set(objective.id);
  }

  closeObjectiveDetail(): void {
    this.viewingObjectiveId.set(null);
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
        if (updated.objectiveId) {
          this.recomputeObjectiveProgress(updated.objectiveId, updatedTasks);
        }
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
      this.objectives.set(this.objectives().map(o => o.id === updated.id ? updated : o));
    });
  }

  // ── CRUD ───────────────────────────────────────────────
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
          this.objectives.set([...this.objectives(), created]);
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
          this.objectives.set(this.objectives().map(o => o.id === updatedObj.id ? updatedObj : o));
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
        this.objectives.set(this.objectives().filter(o => o.id !== target.id));
        this.deleteTarget.set(null);
      },
      error: () => {
        this.deleteTarget.set(null);
        this.loadError.set(true);
      }
    });
  }
}
