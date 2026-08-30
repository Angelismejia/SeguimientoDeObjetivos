import { TaskItem } from '../models/task.model';

/**
 * Dice si una tarea quedo hecha en un dia concreto.
 *
 * Hay dos caminos porque hay dos formas de guardar la respuesta: una tarea
 * recurrente lleva un registro por dia (completedDates), y una de un solo dia
 * se describe entera con su status. Este es el unico lugar donde esa diferencia
 * se resuelve; todo lo demas pregunta aca.
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
 * Todos los dias en que la tarea quedo hecha, en formato "YYYY-MM-DD".
 *
 * Es la version "a lo largo del tiempo" de isTaskDoneOn: en vez de preguntar por
 * un dia, devuelve la lista entera. Una recurrente marcada lunes, martes y
 * miercoles aporta tres dias; una de un solo dia aporta uno o ninguno.
 *
 * Devuelve una lista y no un Set a proposito: quien cuenta cuantas tareas se
 * completaron necesita los repetidos (tres tareas distintas el lunes son tres),
 * y quien mira que dias hubo actividad arma su propio Set.
 */
export function completedDayKeys(task: TaskItem): string[] {
  if (task.isRecurring) {
    return (task.completedDates ?? []).map(d => d.substring(0, 10));
  }
  return task.status === 'Completed' && task.scheduledDate
    ? [task.scheduledDate.substring(0, 10)]
    : [];
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
