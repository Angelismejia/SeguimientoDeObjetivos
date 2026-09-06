import { describe, it, expect } from 'vitest';
import { cuerpoDeActualizacion } from './task-update.util';
import { TaskItem } from '../models/task.model';

// Una tarea con TODOS los campos opcionales puestos: la gracia de estas pruebas
// es que ninguno se pierda en el camino.
const tareaCompleta: TaskItem = {
  id: 4,
  title: 'Estudiar Angular',
  description: 'Signals y control flow',
  emoji: '📚',
  color: '#4f46e5',
  scheduledDate: '2026-08-17',
  scheduledTime: '09:00:00',
  endTime: '11:00:00',
  reminderMinutesBefore: 15,
  priority: 'High',
  status: 'Pending',
  isRecurring: true,
  recurrenceType: 'Weekly',
  repeatEveryWeeks: 2,
  endRepeatDate: '2026-12-31',
  userId: 1,
  objectiveId: 9,
  categoryId: 3,
  createdAt: '2026-08-01T00:00:00Z'
};

describe('cuerpoDeActualizacion', () => {
  // Este es el bug que motivo el helper: el PUT reemplaza la fila entera, asi
  // que un campo que no viaja se guarda como null. Las cuatro pantallas que
  // marcaban una tarea como hecha se olvidaban de estos tres, y completar una
  // recurrente le borraba el recordatorio y su fecha de fin de repeticion.
  it('no pierde ningun campo que el backend vaya a sobrescribir', () => {
    const cuerpo = cuerpoDeActualizacion(tareaCompleta);

    expect(cuerpo.reminderMinutesBefore).toBe(15);
    expect(cuerpo.repeatEveryWeeks).toBe(2);
    expect(cuerpo.endRepeatDate).toBe('2026-12-31');
    expect(cuerpo.scheduledTime).toBe('09:00:00');
    expect(cuerpo.endTime).toBe('11:00:00');
  });

  it('copia todo el resto de la tarea tal cual', () => {
    const cuerpo = cuerpoDeActualizacion(tareaCompleta);

    expect(cuerpo).toEqual({
      title: 'Estudiar Angular',
      description: 'Signals y control flow',
      emoji: '📚',
      color: '#4f46e5',
      scheduledDate: '2026-08-17',
      scheduledTime: '09:00:00',
      endTime: '11:00:00',
      reminderMinutesBefore: 15,
      priority: 'High',
      status: 'Pending',
      isRecurring: true,
      recurrenceType: 'Weekly',
      repeatEveryWeeks: 2,
      endRepeatDate: '2026-12-31',
      objectiveId: 9,
      categoryId: 3
    });
  });

  it('los cambios pisan solo lo que nombran', () => {
    const cuerpo = cuerpoDeActualizacion(tareaCompleta, {
      status: 'Completed',
      scheduledDate: '2026-09-05'
    });

    expect(cuerpo.status).toBe('Completed');
    expect(cuerpo.scheduledDate).toBe('2026-09-05');
    expect(cuerpo.title).toBe('Estudiar Angular');
    expect(cuerpo.endRepeatDate).toBe('2026-12-31');
  });

  // No manda `id`, `userId`, `createdAt` ni `completedDates`: no son del DTO y
  // el backend los ignora o los maneja el.
  it('no arrastra campos que no son del DTO', () => {
    const cuerpo = cuerpoDeActualizacion(tareaCompleta) as unknown as Record<string, unknown>;

    expect(cuerpo['id']).toBeUndefined();
    expect(cuerpo['userId']).toBeUndefined();
    expect(cuerpo['createdAt']).toBeUndefined();
    expect(cuerpo['completedDates']).toBeUndefined();
  });
});
