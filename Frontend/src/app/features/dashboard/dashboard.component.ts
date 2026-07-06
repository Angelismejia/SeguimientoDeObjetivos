import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { AuthService } from '../../core/services/auth.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskService } from '../../core/services/task.service';
import { Objective } from '../../core/models/objective.model';
import { TaskItem } from '../../core/models/task.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, BaseChartDirective],
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

  topObjectives = signal<Objective[]>([]);
  upcomingTasks = signal<TaskItem[]>([]);

  chartData = signal<ChartData<'doughnut'>>({ labels: [], datasets: [{ data: [] }] });
  chartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } }
  };

  constructor(
    private auth: AuthService,
    private objectiveService: ObjectiveService,
    private taskService: TaskService
  ) {
    this.name = this.auth.getName();
  }

  ngOnInit(): void {
    const userId = this.auth.getUserId();
    forkJoin({
      objectives: this.objectiveService.getAll(userId),
      tasks: this.taskService.getAll(userId)
    }).subscribe({
      next: ({ objectives, tasks }) => {
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
      !!t.dueDate && new Date(t.dueDate) < today && t.status !== 'Completed'
    ).length);

    this.topObjectives.set(
      objectives
        .filter(o => o.status === 'InProgress')
        .sort((a, b) => b.progressPercentage - a.progressPercentage)
        .slice(0, 4)
    );

    this.upcomingTasks.set(
      tasks
        .filter(t => t.status !== 'Completed' && t.status !== 'Skipped' && !!t.dueDate)
        .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
        .slice(0, 5)
    );

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
    return !!task.dueDate && new Date(task.dueDate) < new Date();
  }
}
