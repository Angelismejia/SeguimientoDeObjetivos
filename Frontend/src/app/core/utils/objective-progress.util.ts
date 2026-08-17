import { Objective, ObjectiveStatus } from '../models/objective.model';
import { TaskItem } from '../models/task.model';

export interface ObjectiveProgress {
  progressPercentage: number;
  status: ObjectiveStatus;
  taskCount: number;
  /** Hay que preguntarle al usuario si da el objetivo por terminado. */
  askIfFinished: boolean;
  /** El porcentaje o el estado cambiaron, hay que guardar. */
  changed: boolean;
}

/**
 * Decide en que estado queda un objetivo segun sus tareas.
 *
 * Vivia copiado en dashboard, objectives y tasks. Estando triplicado, arreglar
 * un solo lugar dejaba los otros dos con el comportamiento viejo, que es
 * exactamente lo que paso al introducir la pregunta de "¿terminaste?".
 *
 * Dos reglas que no son obvias:
 *
 * - Llegar al 100% NO completa el objetivo. Quedarse sin tareas pendientes no es
 *   lo mismo que haber logrado la meta: las tareas que existen son las que se le
 *   ocurrieron al usuario hasta ahora. Se le pregunta y decide el.
 * - Un objetivo con una tarea recurrente vinculada nunca se da por terminado:
 *   mañana esa tarea vuelve a estar pendiente, asi que no se pregunta nada.
 */
export function computeObjectiveProgress(
  objective: Objective,
  linkedTasks: TaskItem[]
): ObjectiveProgress | null {
  if (linkedTasks.length === 0) return null;

  const completed = linkedTasks.filter(t => t.status === 'Completed').length;
  const progressPercentage = Math.round((completed / linkedTasks.length) * 100);
  const hasRecurring = linkedTasks.some(t => t.isRecurring);

  let status = objective.status;
  if (status !== 'Cancelled' && status !== 'Completed') {
    status = progressPercentage > 0 ? 'InProgress' : 'Pending';
  }

  // Si ya respondio "todavia no" con esta misma cantidad de tareas, no se
  // vuelve a preguntar: recien cuando agregue tareas nuevas y las complete.
  const asked = objective.completionAskedAtTaskCount;
  const askIfFinished =
    progressPercentage === 100 &&
    !hasRecurring &&
    objective.status !== 'Completed' &&
    objective.status !== 'Cancelled' &&
    (asked === null || asked === undefined || linkedTasks.length > asked);

  return {
    progressPercentage,
    status,
    taskCount: linkedTasks.length,
    askIfFinished,
    changed: objective.progressPercentage !== progressPercentage || objective.status !== status
  };
}
