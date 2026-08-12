import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ChartConfiguration, ChartData } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';
import { AuthService } from '../../core/services/auth.service';
import { TaskService } from '../../core/services/task.service';
import { ObjectiveService } from '../../core/services/objective.service';
import { TaskItem } from '../../core/models/task.model';
import { Objective, ObjectiveStatus } from '../../core/models/objective.model';

type Period = 'week' | 'month' | '3months' | 'year';

interface HeatmapCell {
  date: Date;
  key: string;
  count: number;
  level: number;
  inFuture: boolean;
}

interface Insight {
  icon: string;
  title: string;
  body: string;
}

// El Dashboard responde "¿cómo voy hoy?". Esta pantalla responde
// "¿cómo he progresado con el tiempo y qué patrones tengo?" — por eso no repite
// las tarjetas del dashboard (tareas de hoy, calendario semanal, etc.), sino que
// mira ventanas de tiempo más largas: tendencia, constancia, rendimiento por
// objetivo e insights derivados de datos reales.
@Component({
  selector: 'app-statistics',
  standalone: true,
  imports: [CommonModule, RouterLink, BaseChartDirective],
  templateUrl: './statistics.component.html',
  styleUrl: './statistics.component.css'
})
export class StatisticsComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);

  allTasks = signal<TaskItem[]>([]);
  objectives = signal<Objective[]>([]);

  period = signal<Period>('week');
  periods: { key: Period; label: string }[] = [
    { key: 'week', label: 'Semana' },
    { key: 'month', label: 'Mes' },
    { key: '3months', label: '3 meses' },
    { key: 'year', label: 'Año' }
  ];

  private readonly PERIOD_DAYS: Record<Period, number> = {
    week: 7,
    month: 30,
    '3months': 90,
    year: 365
  };

  // Escala secuencial de 1 tono (indigo), validada con el validador de la skill
  // de dataviz para que cada escalon se distinga del anterior y el mas claro
  // tenga contraste suficiente. El nivel 0 (sin tareas) no usa relleno: se
  // dibuja como celda vacia con borde — es el tratamiento estandar de este tipo
  // de mapa de calor (ausencia de dato, no un dato "muy bajo").
  private readonly HEAT_COLORS = ['#93a2fa', '#7986f6', '#6366f1', '#4338ca'];

  private readonly GOOD_COLOR = '#0ca30c';
  private readonly BAD_COLOR = '#d03b3b';

  constructor(
    private auth: AuthService,
    private taskService: TaskService,
    private objectiveService: ObjectiveService
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    const userId = this.auth.getUserId();
    forkJoin({
      tasks: this.taskService.getAll(userId),
      objectives: this.objectiveService.getAll(userId)
    }).subscribe({
      next: ({ tasks, objectives }) => {
        this.allTasks.set(tasks);
        this.objectives.set(objectives);
        this.buildProgressChart();
        this.buildActivityChart();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  setPeriod(p: Period): void {
    if (this.period() === p) return;
    this.period.set(p);
    this.buildProgressChart();
    this.buildActivityChart();
  }

  // ── Dato base compartido: dia en que una tarea quedo "hecha" ──────────────
  // Nota (misma limitacion en toda la pantalla): una tarea recurrente es una
  // sola fila con un unico scheduledDate, asi que solo puede tener UN dia
  // marcado como completado a la vez — completarla hoy "pisa" el registro de
  // ayer. Ver [[project_pending_features]] item 7. Todo lo que sigue usa esta
  // misma fecha (consistente con la racha del dashboard), a sabiendas de eso.
  private doneDayKeys(): string[] {
    return this.allTasks()
      .filter(t => t.status === 'Completed' && !!t.scheduledDate)
      .map(t => t.scheduledDate.substring(0, 10));
  }

  private tasksScheduledInRange(start: Date, end: Date): TaskItem[] {
    return this.allTasks().filter(t => {
      if (!t.scheduledDate) return false;
      const d = this.parseDateKey(t.scheduledDate.substring(0, 10));
      return d >= start && d <= end;
    });
  }

  private completionRate(tasks: TaskItem[]): number {
    if (tasks.length === 0) return 0;
    return Math.round((tasks.filter(t => t.status === 'Completed').length / tasks.length) * 100);
  }

  // ── Tu progreso ──────────────────────────────
  progressChartData = signal<ChartData<'line'>>({ labels: [], datasets: [] });
  progressChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    elements: { line: { tension: 0.35 }, point: { radius: 0, hoverRadius: 4 } },
    scales: { y: { beginAtZero: true, max: 100, ticks: { callback: v => v + '%' } } },
    plugins: {
      legend: { display: false },
      tooltip: { callbacks: { label: ctx => `${ctx.parsed.y}% completado` } }
    }
  };

  currentPeriodRate = signal(0);
  previousPeriodRate = signal<number | null>(null);
  hasProgressData = signal(false);

  private buildProgressChart(): void {
    const today = this.startOfDay(new Date());
    const days = this.PERIOD_DAYS[this.period()];
    const rangeStart = this.addDays(today, -(days - 1));
    const prevStart = this.addDays(rangeStart, -days);
    const prevEnd = this.addDays(rangeStart, -1);

    const inRange = this.tasksScheduledInRange(rangeStart, today);
    const inPrevRange = this.tasksScheduledInRange(prevStart, prevEnd);

    this.hasProgressData.set(inRange.length > 0);
    this.currentPeriodRate.set(this.completionRate(inRange));
    this.previousPeriodRate.set(inPrevRange.length > 0 ? this.completionRate(inPrevRange) : null);

    // Cantidad de puntos de la linea segun el periodo, para que se vea "suave"
    // (pocos puntos) en vez de un zigzag diario incluso en rangos largos.
    const bucketCount = this.period() === 'week' ? 7 : this.period() === 'month' ? 5 : this.period() === '3months' ? 12 : 12;
    const bucketDays = Math.ceil(days / bucketCount);

    const labels: string[] = [];
    const data: number[] = [];
    for (let i = bucketCount - 1; i >= 0; i--) {
      const bucketEnd = this.addDays(today, -i * bucketDays);
      const bucketStart = this.addDays(bucketEnd, -(bucketDays - 1));
      const bucketTasks = this.tasksScheduledInRange(bucketStart, bucketEnd);
      data.push(this.completionRate(bucketTasks));
      labels.push(bucketDays === 1
        ? bucketEnd.toLocaleDateString('es-ES', { weekday: 'short' })
        : bucketEnd.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' }));
    }

    this.progressChartData.set({
      labels,
      datasets: [{
        data,
        borderColor: '#4f46e5',
        backgroundColor: 'rgba(79,70,229,0.12)',
        fill: true,
        borderWidth: 2
      }]
    });
  }

  // ── Constancia (heatmap + rachas) ──────────────────────────────
  private readonly HEATMAP_WEEKS = 12;

  heatmapWeeks = computed<HeatmapCell[][]>(() => {
    const counts = new Map<string, number>();
    for (const t of this.allTasks()) {
      if (t.status !== 'Completed' || !t.scheduledDate) continue;
      const key = t.scheduledDate.substring(0, 10);
      counts.set(key, (counts.get(key) ?? 0) + 1);
    }

    const today = this.startOfDay(new Date());
    const endOfWeek = this.addDays(today, 6 - today.getDay());
    const totalDays = this.HEATMAP_WEEKS * 7;
    const start = this.addDays(endOfWeek, -totalDays + 1);

    const cells: HeatmapCell[] = [];
    for (let i = 0; i < totalDays; i++) {
      const date = this.addDays(start, i);
      const key = this.dateKey(date);
      const count = counts.get(key) ?? 0;
      cells.push({ date, key, count, level: this.heatLevel(count), inFuture: date > today });
    }

    const weeks: HeatmapCell[][] = [];
    for (let i = 0; i < cells.length; i += 7) weeks.push(cells.slice(i, i + 7));
    return weeks;
  });

  heatmapMonthLabels = computed(() =>
    this.heatmapWeeks().map(week => {
      const first = week[0];
      return first.date.getDate() <= 7 ? first.date.toLocaleDateString('es-ES', { month: 'short' }) : '';
    })
  );

  heatCellColor(cell: HeatmapCell): string {
    return cell.level === 0 ? 'transparent' : this.HEAT_COLORS[cell.level - 1];
  }

  heatCellTitle(cell: HeatmapCell): string {
    const label = cell.date.toLocaleDateString('es-ES', { day: 'numeric', month: 'long', year: 'numeric' });
    const noun = cell.count === 1 ? 'tarea completada' : 'tareas completadas';
    return `${label}: ${cell.count} ${noun}`;
  }

  private heatLevel(count: number): number {
    if (count <= 0) return 0;
    if (count === 1) return 1;
    if (count === 2) return 2;
    if (count === 3) return 3;
    return 4;
  }

  // Racha actual: igual criterio que el dashboard (hoy hacia atras).
  currentStreak = computed(() => {
    const done = new Set(this.doneDayKeys());
    const today = this.startOfDay(new Date());
    let cursor = this.dateKey(today);
    if (!done.has(cursor)) cursor = this.dateKey(this.addDays(today, -1));
    let streak = 0;
    while (done.has(cursor)) {
      streak++;
      cursor = this.dateKey(this.addDays(this.parseDateKey(cursor), -1));
    }
    return streak;
  });

  // Mejor racha historica: la corrida consecutiva mas larga entre todos los
  // dias con al menos una tarea completada.
  bestStreak = computed(() => {
    const days = [...new Set(this.doneDayKeys())].sort();
    if (days.length === 0) return 0;
    let best = 1;
    let run = 1;
    for (let i = 1; i < days.length; i++) {
      const prev = this.parseDateKey(days[i - 1]);
      const curr = this.parseDateKey(days[i]);
      const diff = Math.round((curr.getTime() - prev.getTime()) / 86400000);
      run = diff === 1 ? run + 1 : 1;
      best = Math.max(best, run);
    }
    return best;
  });

  // Dias activos dentro del periodo elegido, sobre el total de dias del periodo.
  activeDays = computed(() => {
    const days = this.PERIOD_DAYS[this.period()];
    const today = this.startOfDay(new Date());
    const rangeStart = this.addDays(today, -(days - 1));
    const done = new Set(this.doneDayKeys());
    let active = 0;
    for (let i = 0; i < days; i++) {
      if (done.has(this.dateKey(this.addDays(rangeStart, i)))) active++;
    }
    return { active, total: days };
  });

  // ── Rendimiento de objetivos ──────────────────────────────
  private readonly STATUS_LABEL: Record<ObjectiveStatus, string> = {
    Pending: 'Pendiente',
    InProgress: 'En progreso',
    Completed: 'Completado',
    Cancelled: 'Cancelado'
  };

  topObjectives = computed(() =>
    [...this.objectives()]
      .filter(o => o.status !== 'Cancelled')
      .sort((a, b) => b.progressPercentage - a.progressPercentage)
      .slice(0, 5)
      .map(o => ({ ...o, statusLabel: this.STATUS_LABEL[o.status] }))
  );

  // ── Actividad: tareas completadas por sub-periodo ──────────────────────
  // Adaptacion: el mockup pedia "objetivos completados por semana", pero
  // Objective no guarda cuando se completo (no hay CompletedAt), asi que no
  // hay forma de reconstruir ese dato sin inventarlo. Se usa la cuenta real
  // de tareas completadas por sub-periodo en su lugar.
  activityChartData = signal<ChartData<'bar'>>({ labels: [], datasets: [] });
  activityChartOptions: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
    plugins: { legend: { display: false } }
  };
  hasActivityData = signal(false);

  private buildActivityChart(): void {
    const today = this.startOfDay(new Date());
    const days = this.PERIOD_DAYS[this.period()];
    const bucketCount = this.period() === 'week' ? 7 : this.period() === 'month' ? 5 : this.period() === '3months' ? 12 : 12;
    const bucketDays = Math.ceil(days / bucketCount);

    const labels: string[] = [];
    const data: number[] = [];
    let total = 0;
    for (let i = bucketCount - 1; i >= 0; i--) {
      const bucketEnd = this.addDays(today, -i * bucketDays);
      const bucketStart = this.addDays(bucketEnd, -(bucketDays - 1));
      const count = this.tasksScheduledInRange(bucketStart, bucketEnd).filter(t => t.status === 'Completed').length;
      total += count;
      data.push(count);
      labels.push(bucketDays === 1
        ? bucketEnd.toLocaleDateString('es-ES', { weekday: 'short' })
        : bucketEnd.toLocaleDateString('es-ES', { day: 'numeric', month: 'short' }));
    }

    this.hasActivityData.set(total > 0);
    this.activityChartData.set({
      labels,
      datasets: [{ data, backgroundColor: '#4f46e5', borderRadius: 4, barPercentage: 0.6 }]
    });
  }

  // ── Insights ──────────────────────────────
  insights = computed<Insight[]>(() => {
    const list: Insight[] = [];

    const curr = this.currentPeriodRate();
    const prev = this.previousPeriodRate();
    if (prev !== null) {
      const delta = curr - prev;
      if (delta > 0) {
        list.push({
          icon: '✨',
          title: 'Vas mejorando',
          body: `Completaste ${delta} puntos porcentuales más de tareas que en el período anterior.`
        });
      } else if (delta < 0) {
        list.push({
          icon: '📉',
          title: 'Bajaste el ritmo',
          body: `Completaste ${Math.abs(delta)} puntos porcentuales menos de tareas que en el período anterior.`
        });
      }
    }

    const bestDay = this.mostProductiveWeekday();
    if (bestDay) {
      list.push({
        icon: '🔥',
        title: `Los ${bestDay.name} son tu fuerte`,
        body: `Es el día en que más tareas completás (${bestDay.count} en total).`
      });
    }

    const topObjective = this.topObjectives()[0];
    if (topObjective && topObjective.progressPercentage > 0) {
      list.push({
        icon: '🎯',
        title: `${topObjective.title} es tu objetivo con más avance`,
        body: `Llevás un ${topObjective.progressPercentage}% completado.`
      });
    }

    return list.slice(0, 3);
  });

  private mostProductiveWeekday(): { name: string; count: number } | null {
    const names = ['domingo', 'lunes', 'martes', 'miércoles', 'jueves', 'viernes', 'sábado'];
    const counts = new Array(7).fill(0);
    for (const key of this.doneDayKeys()) {
      counts[this.parseDateKey(key).getDay()]++;
    }
    const max = Math.max(...counts);
    if (max === 0) return null;
    const idx = counts.indexOf(max);
    return { name: names[idx], count: max };
  }

  // ── Resumen ──────────────────────────────
  summary = computed(() => {
    const tasks = this.allTasks();
    const objectivesCompleted = this.objectives().filter(o => o.status === 'Completed').length;
    const completionRate = this.completionRate(tasks);
    const bestDay = this.mostProductiveWeekday();
    return {
      objectivesCompleted,
      completionRate,
      bestStreak: this.bestStreak(),
      bestDayName: bestDay ? this.capitalize(bestDay.name) : '—'
    };
  });

  private capitalize(s: string): string {
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  private startOfDay(date: Date): Date {
    const d = new Date(date);
    d.setHours(0, 0, 0, 0);
    return d;
  }

  private addDays(date: Date, amount: number): Date {
    const d = new Date(date);
    d.setDate(d.getDate() + amount);
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
}
