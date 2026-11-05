import { TaskItem } from '../models/task.model';

/**
 * Si una tarea cae en un dia dado, teniendo en cuenta su recurrencia.
 *
 * Estaba copiada igual en el dashboard y en el calendario. El backend tiene la
 * misma regla en `RecurrenciaDeTareas.OcurreEl` (la usa el recordatorio de la
 * hora de la tarea): si se cambia una, hay que cambiar la otra.
 */
export function taskOccursOn(task: TaskItem, date: Date): boolean {
  if (!task.scheduledDate) return false;
  const start = parseDateKey(task.scheduledDate.substring(0, 10));
  const day = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  if (day < start) return false;
  if (task.endRepeatDate) {
    const end = parseDateKey(task.endRepeatDate.substring(0, 10));
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

function parseDateKey(key: string): Date {
  const [y, m, d] = key.split('-').map(Number);
  return new Date(y, m - 1, d);
}
