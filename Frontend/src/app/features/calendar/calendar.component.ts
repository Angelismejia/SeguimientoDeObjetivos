import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TaskService } from '../../core/services/task.service';
import { TaskItem } from '../../core/models/task.model';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { isTaskDoneOn, isTaskOverdue } from '../../core/utils/task-status.util';

// Antes esto era un modal ("calendario grande") que solo se podia abrir desde
// el Dashboard. Se convirtio en pagina propia (/calendar) para que el menu
// secundario (sidebar de escritorio, "mas" desde Perfil en movil) pueda
// enlazarla directamente. La logica de mes/dia es la misma que tenia el modal;
// solo cambio de vivir en DashboardComponent a vivir aca.
@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [CommonModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './calendar.component.html',
  styleUrl: './calendar.component.css'
})
export class CalendarComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);
  allTasks = signal<TaskItem[]>([]);

  monthCursor = signal(this.startOfMonth(new Date()));
  selectedDate = signal<string | null>(this.dateKey(new Date()));
  deleteTaskTarget = signal<TaskItem | null>(null);

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

  selectedDayTasks = computed(() => {
    const key = this.selectedDate();
    if (!key) return [];
    const date = this.parseDateKey(key);
    return this.allTasks()
      .filter(t => this.taskOccursOn(t, date))
      .sort((a, b) => (a.scheduledTime ?? '99:99').localeCompare(b.scheduledTime ?? '99:99'));
  });

  selectedDateLabel = computed(() => {
    const key = this.selectedDate();
    if (!key) return '';
    return this.parseDateKey(key).toLocaleDateString('es-ES', { weekday: 'long', day: 'numeric', month: 'long' });
  });

  constructor(private auth: AuthService, private taskService: TaskService) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.taskService.getAll(this.auth.getUserId()).subscribe({
      next: tasks => {
        this.allTasks.set(tasks);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
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

  goToday(): void {
    this.monthCursor.set(this.startOfMonth(new Date()));
    this.selectedDate.set(this.dateKey(new Date()));
  }

  selectDay(key: string): void {
    this.selectedDate.set(this.selectedDate() === key ? null : key);
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
    const scheduledDate = (task.isRecurring && newStatus === 'Completed')
      ? (dateKey ?? this.dateKey(new Date()))
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
    }).subscribe(updated => {
      this.allTasks.set(this.allTasks().map(t => t.id === updated.id ? updated : t));
    });
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
        this.allTasks.set(this.allTasks().filter(t => t.id !== target.id));
        this.deleteTaskTarget.set(null);
      },
      error: () => {
        this.deleteTaskTarget.set(null);
      }
    });
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

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  private startOfWeek(date: Date): Date {
    const d = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const offset = (d.getDay() + 6) % 7; // Monday-first
    d.setDate(d.getDate() - offset);
    return d;
  }

  private parseDateKey(key: string): Date {
    const [y, m, d] = key.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  private dateKey(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
