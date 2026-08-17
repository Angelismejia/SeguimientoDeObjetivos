import { TaskItem } from '../models/task.model';

/**
 * Las tareas recurrentes son una sola fila con una unica scheduledDate (no hay
 * un registro por dia). Al completarlas, el backend mueve esa fecha a "hoy",
 * asi que solo cuentan como completadas en el dia exacto en que se marcaron;
 * cualquier otro dia deben verse pendientes de nuevo. Las tareas no recurrentes
 * simplemente respetan su status.
 */
export function isTaskDoneOn(task: TaskItem, dateKey: string): boolean {
  // Una tarea recurrente es una sola fila con una unica scheduledDate, asi que su
  // status no puede describir mas de un dia. Su historial vive en completedDates:
  // una entrada por cada dia que se marco.
  if (task.isRecurring) {
    return (task.completedDates ?? []).some(d => d.substring(0, 10) === dateKey);
  }
  // Las de un solo dia se resuelven con su propio status, que les alcanza.
  return task.status === 'Completed';
}

/**
 * Comparamos las fechas como texto "YYYY-MM-DD" en vez de `new Date(...)`:
 * un string sin hora se parsea como medianoche UTC, y comparado contra la
 * medianoche LOCAL puede marcar como "vencidas" tareas programadas para hoy
 * mismo, dependiendo del huso horario del usuario.
 */
export function isTaskOverdue(task: TaskItem, todayKey: string, doneToday: boolean): boolean {
  if (doneToday || task.status === 'Skipped' || !task.scheduledDate) return false;
  return task.scheduledDate.substring(0, 10) < todayKey;
}
