import { TaskItem, UpdateTaskDto } from '../models/task.model';

/**
 * Arma el cuerpo de `PUT /api/tasks/{id}` a partir de la tarea que ya esta en
 * memoria, cambiando solo lo que se le pase.
 *
 * Ese PUT reemplaza la fila entera: todo campo que no se manda llega como null
 * al backend y se guarda como null. Las cuatro pantallas que marcan una tarea
 * como hecha armaban este objeto a mano, y las cuatro se olvidaban de algo:
 * ninguna mandaba `reminderMinutesBefore`, `repeatEveryWeeks` ni
 * `endRepeatDate`, y la de objetivos tampoco mandaba las horas. Marcar una
 * tarea le borraba el recordatorio y la fecha en que dejaba de repetirse, sin
 * ningun error a la vista.
 *
 * Con un solo lugar que lo arme, agregar un campo al modelo no puede volver a
 * perderse en cuatro copias que fueron quedando desalineadas.
 */
export function cuerpoDeActualizacion(
  task: TaskItem,
  cambios: Partial<UpdateTaskDto> = {}
): UpdateTaskDto {
  return {
    title: task.title,
    description: task.description,
    emoji: task.emoji,
    color: task.color,
    scheduledDate: task.scheduledDate,
    scheduledTime: task.scheduledTime,
    endTime: task.endTime,
    reminderMinutesBefore: task.reminderMinutesBefore,
    priority: task.priority,
    status: task.status,
    isRecurring: task.isRecurring,
    recurrenceType: task.recurrenceType,
    repeatEveryWeeks: task.repeatEveryWeeks,
    endRepeatDate: task.endRepeatDate,
    objectiveId: task.objectiveId,
    categoryId: task.categoryId,
    ...cambios
  };
}
