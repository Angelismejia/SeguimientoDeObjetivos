import { RecurrenceType, TaskPriority, TaskStatus } from '../models/task.model';

// Los textos en castellano de los enums de tareas. Estaban copiados en el
// dashboard, en Tareas y en el formulario; si se cambiaba uno, los otros
// quedaban diciendo otra cosa.

export function taskPriorityLabel(priority: TaskPriority): string {
  switch (priority) {
    case 'Low': return 'Baja';
    case 'Medium': return 'Media';
    case 'High': return 'Alta';
  }
}

export function taskStatusLabel(status: TaskStatus): string {
  switch (status) {
    case 'Pending': return 'Pendiente';
    case 'InProgress': return 'En progreso';
    case 'Completed': return 'Completada';
    case 'Skipped': return 'Omitida';
  }
}

export function recurrenceLabel(type: RecurrenceType): string {
  switch (type) {
    case 'None': return 'No se repite';
    case 'Daily': return 'Diaria';
    case 'Weekly': return 'Semanal';
    case 'Monthly': return 'Mensual';
    case 'Yearly': return 'Anual';
  }
}
