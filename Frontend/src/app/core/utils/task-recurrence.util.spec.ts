import { describe, it, expect } from 'vitest';
import { taskOccursOn } from './task-recurrence.util';
import { TaskItem } from '../models/task.model';

// Esta regla existe dos veces: aca (dashboard y calendario) y en C#
// (RecurrenciaDeTareas.OcurreEl, que usa el recordatorio). Estos casos son los
// mismos que fija RecordatoriosDeRecurrentesTests del backend: si uno de los dos
// lados cambia y el otro no, el calendario y los avisos dejan de coincidir.

function tarea(over: Partial<TaskItem> = {}): TaskItem {
  return {
    id: 1,
    title: 'Correr',
    scheduledDate: '2026-11-02', // lunes
    status: 'Pending',
    isRecurring: false,
    recurrenceType: 'None',
    priority: 'Medium',
    userId: 1,
    createdAt: '2026-11-01T00:00:00Z',
    ...over
  } as TaskItem;
}

const dia = (key: string) => {
  const [y, m, d] = key.split('-').map(Number);
  return new Date(y, m - 1, d);
};

describe('taskOccursOn', () => {
  it('una tarea de un solo dia solo cae ese dia', () => {
    const t = tarea();
    expect(taskOccursOn(t, dia('2026-11-02'))).toBe(true);
    expect(taskOccursOn(t, dia('2026-11-03'))).toBe(false);
  });

  it('una diaria cae todos los dias desde que empieza, y no antes', () => {
    const t = tarea({ isRecurring: true, recurrenceType: 'Daily' });
    expect(taskOccursOn(t, dia('2026-11-01'))).toBe(false);
    expect(taskOccursOn(t, dia('2026-11-02'))).toBe(true);
    expect(taskOccursOn(t, dia('2026-11-20'))).toBe(true);
  });

  it('una semanal solo cae el mismo dia de la semana', () => {
    const t = tarea({ isRecurring: true, recurrenceType: 'Weekly' });
    expect(taskOccursOn(t, dia('2026-11-09'))).toBe(true);
    expect(taskOccursOn(t, dia('2026-11-10'))).toBe(false);
  });

  it('cada dos semanas se salta la semana del medio', () => {
    const t = tarea({ isRecurring: true, recurrenceType: 'Weekly', repeatEveryWeeks: 2 });
    expect(taskOccursOn(t, dia('2026-11-09'))).toBe(false);
    expect(taskOccursOn(t, dia('2026-11-16'))).toBe(true);
  });

  it('una mensual cae el mismo numero de dia y una anual el mismo dia y mes', () => {
    const mensual = tarea({ isRecurring: true, recurrenceType: 'Monthly' });
    expect(taskOccursOn(mensual, dia('2026-12-02'))).toBe(true);
    expect(taskOccursOn(mensual, dia('2026-12-03'))).toBe(false);

    const anual = tarea({ isRecurring: true, recurrenceType: 'Yearly' });
    expect(taskOccursOn(anual, dia('2027-11-02'))).toBe(true);
    expect(taskOccursOn(anual, dia('2026-12-02'))).toBe(false);
  });

  it('no cae despues de que termina la repeticion', () => {
    const t = tarea({ isRecurring: true, recurrenceType: 'Daily', endRepeatDate: '2026-11-05' });
    expect(taskOccursOn(t, dia('2026-11-05'))).toBe(true);
    expect(taskOccursOn(t, dia('2026-11-06'))).toBe(false);
  });

  it('una tarea sin fecha no cae nunca', () => {
    expect(taskOccursOn(tarea({ scheduledDate: undefined }), dia('2026-11-02'))).toBe(false);
  });

  it('marcada como recurrente pero sin tipo se trata como de un solo dia', () => {
    const t = tarea({ isRecurring: true, recurrenceType: 'None' });
    expect(taskOccursOn(t, dia('2026-11-02'))).toBe(true);
    expect(taskOccursOn(t, dia('2026-11-03'))).toBe(false);
  });
});
