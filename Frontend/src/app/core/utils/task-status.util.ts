import { TaskItem } from '../models/task.model';

/**
 * Las tareas recurrentes son una sola fila con una unica scheduledDate (no hay
 * un registro por dia). Al completarlas, el backend mueve esa fecha a "hoy",
 * asi que solo cuentan como completadas en el dia exacto en que se marcaron;
 * cualquier otro dia deben verse pendientes de nuevo. Las tareas no recurrentes
 * simplemente respetan su status.
 */
export function isTaskDoneOn(task: TaskItem, dateKey: string): boolean {
  if (task.status !== 'Completed') return false;
  if (!task.isRecurring) return true;
  return task.scheduledDate?.substring(0, 10) === dateKey;
}
