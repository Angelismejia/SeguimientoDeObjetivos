import { describe, it, expect } from 'vitest';
import { completedDayKeys, isTaskDoneOn, isTaskOverdue } from './task-status.util';
import { TaskItem } from '../models/task.model';

function tarea(over: Partial<TaskItem> = {}): TaskItem {
  return {
    id: 1,
    title: 'Leer docs',
    scheduledDate: '2026-08-17',
    status: 'Pending',
    isRecurring: false,
    recurrenceType: 'None',
    priority: 'Medium',
    userId: 1,
    createdAt: '2026-08-01T00:00:00Z',
    ...over
  } as TaskItem;
}

const recurrente = (dias: string[]) =>
  tarea({ isRecurring: true, recurrenceType: 'Daily', completedDates: dias });

describe('isTaskDoneOn', () => {
  it('una tarea de un solo dia se resuelve con su status', () => {
    expect(isTaskDoneOn(tarea({ status: 'Completed' }), '2026-08-17')).toBe(true);
    expect(isTaskDoneOn(tarea({ status: 'Pending' }), '2026-08-17')).toBe(false);
  });

  it('una recurrente esta hecha solo en los dias que registro', () => {
    const t = recurrente(['2026-08-17', '2026-08-18']);
    expect(isTaskDoneOn(t, '2026-08-17')).toBe(true);
    expect(isTaskDoneOn(t, '2026-08-19')).toBe(false);
  });

  it('una recurrente sin dias registrados no esta hecha en ninguno', () => {
    expect(isTaskDoneOn(recurrente([]), '2026-08-17')).toBe(false);
    expect(isTaskDoneOn(tarea({ isRecurring: true }), '2026-08-17')).toBe(false);
  });

  it('el status de una recurrente no decide nada: manda completedDates', () => {
    const t = recurrente(['2026-08-17']);
    t.status = 'Completed';
    expect(isTaskDoneOn(t, '2026-08-20')).toBe(false);
  });

  it('acepta fechas con hora, comparando solo el dia', () => {
    expect(isTaskDoneOn(recurrente(['2026-08-17T00:00:00Z']), '2026-08-17')).toBe(true);
  });
});

describe('completedDayKeys', () => {
  it('una de un solo dia aporta su scheduledDate, y solo si esta completada', () => {
    expect(completedDayKeys(tarea({ status: 'Completed' }))).toEqual(['2026-08-17']);
    expect(completedDayKeys(tarea({ status: 'Pending' }))).toEqual([]);
  });

  // El bug que motivo el helper: estadisticas leia la unica scheduledDate, asi
  // que una recurrente jamas podia aportar mas de un dia y la racha se cortaba.
  it('una recurrente aporta TODOS sus dias, no uno solo', () => {
    const dias = completedDayKeys(recurrente(['2026-08-17', '2026-08-18', '2026-08-19']));
    expect(dias).toEqual(['2026-08-17', '2026-08-18', '2026-08-19']);
  });

  it('recorta la hora y deja la fecha en formato YYYY-MM-DD', () => {
    expect(completedDayKeys(recurrente(['2026-08-17T13:45:00Z']))).toEqual(['2026-08-17']);
  });

  it('conserva los repetidos entre tareas distintas del mismo dia', () => {
    const tareas = [tarea({ id: 1, status: 'Completed' }), tarea({ id: 2, status: 'Completed' })];
    expect(tareas.flatMap(completedDayKeys)).toHaveLength(2);
  });

  it('es consistente con isTaskDoneOn', () => {
    const t = recurrente(['2026-08-17', '2026-08-19']);
    for (const dia of ['2026-08-17', '2026-08-18', '2026-08-19']) {
      expect(completedDayKeys(t).includes(dia)).toBe(isTaskDoneOn(t, dia));
    }
  });
});

describe('isTaskOverdue', () => {
  it('una tarea de ayer sin hacer esta vencida', () => {
    expect(isTaskOverdue(tarea({ scheduledDate: '2026-08-16' }), '2026-08-17', false)).toBe(true);
  });

  it('si ya se hizo, no esta vencida', () => {
    expect(isTaskOverdue(tarea({ scheduledDate: '2026-08-16' }), '2026-08-17', true)).toBe(false);
  });

  // Comparar como texto y no con new Date() evita que una tarea de hoy se vea
  // vencida por diferencia de huso horario.
  it('una tarea de hoy no esta vencida', () => {
    expect(isTaskOverdue(tarea({ scheduledDate: '2026-08-17' }), '2026-08-17', false)).toBe(false);
  });

  it('una salteada no cuenta como vencida', () => {
    expect(isTaskOverdue(tarea({ scheduledDate: '2026-08-16', status: 'Skipped' }), '2026-08-17', false)).toBe(false);
  });
});
