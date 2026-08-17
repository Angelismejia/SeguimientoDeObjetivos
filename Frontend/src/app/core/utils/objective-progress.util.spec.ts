import { describe, it, expect } from 'vitest';
import { computeObjectiveProgress } from './objective-progress.util';
import { Objective } from '../models/objective.model';
import { TaskItem } from '../models/task.model';

function objetivo(over: Partial<Objective> = {}): Objective {
  return {
    id: 1,
    title: 'Aprender Angular',
    status: 'InProgress',
    progressPercentage: 0,
    isPrimary: false,
    isPrivate: false,
    userId: 1,
    createdAt: '2026-08-01T00:00:00Z',
    ...over
  } as Objective;
}

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
    objectiveId: 1,
    createdAt: '2026-08-01T00:00:00Z',
    ...over
  } as TaskItem;
}

const hecha = (id: number) => tarea({ id, status: 'Completed' });
const pendiente = (id: number) => tarea({ id, status: 'Pending' });

describe('computeObjectiveProgress', () => {
  it('sin tareas vinculadas no calcula nada', () => {
    expect(computeObjectiveProgress(objetivo(), [])).toBeNull();
  });

  it('con la mitad hecha da 50% y queda en progreso', () => {
    const r = computeObjectiveProgress(objetivo(), [hecha(1), pendiente(2)])!;
    expect(r.progressPercentage).toBe(50);
    expect(r.status).toBe('InProgress');
    expect(r.askIfFinished).toBe(false);
  });

  it('sin ninguna hecha queda pendiente', () => {
    const r = computeObjectiveProgress(objetivo(), [pendiente(1), pendiente(2)])!;
    expect(r.progressPercentage).toBe(0);
    expect(r.status).toBe('Pending');
  });

  // El caso que motivo el cambio: el usuario veia "En progreso — 100%".
  it('con todas hechas pero sin confirmar muestra 99%, no 100%', () => {
    const r = computeObjectiveProgress(objetivo(), [hecha(1), hecha(2)])!;
    expect(r.progressPercentage).toBe(99);
    expect(r.status).toBe('InProgress');
  });

  it('con todas hechas pregunta si termino', () => {
    const r = computeObjectiveProgress(objetivo(), [hecha(1), hecha(2)])!;
    expect(r.askIfFinished).toBe(true);
  });

  it('un objetivo ya completado no vuelve a preguntar', () => {
    const r = computeObjectiveProgress(
      objetivo({ status: 'Completed' }),
      [hecha(1), hecha(2)]
    )!;
    expect(r.askIfFinished).toBe(false);
  });

  it('un objetivo cancelado no pregunta ni cambia de estado', () => {
    const r = computeObjectiveProgress(
      objetivo({ status: 'Cancelled' }),
      [hecha(1), hecha(2)]
    )!;
    expect(r.askIfFinished).toBe(false);
    expect(r.status).toBe('Cancelled');
  });

  // Un habito no termina: mañana la tarea vuelve a estar pendiente.
  it('con una tarea recurrente vinculada nunca pregunta', () => {
    const r = computeObjectiveProgress(
      objetivo(),
      [hecha(1), tarea({ id: 2, status: 'Completed', isRecurring: true })]
    )!;
    expect(r.askIfFinished).toBe(false);
  });

  it('si ya dijo "todavia no" con esas mismas tareas, no insiste', () => {
    const r = computeObjectiveProgress(
      objetivo({ completionAskedAtTaskCount: 2 }),
      [hecha(1), hecha(2)]
    )!;
    expect(r.askIfFinished).toBe(false);
  });

  it('pero si agrega una tarea nueva y la completa, vuelve a preguntar', () => {
    const r = computeObjectiveProgress(
      objetivo({ completionAskedAtTaskCount: 2 }),
      [hecha(1), hecha(2), hecha(3)]
    )!;
    expect(r.askIfFinished).toBe(true);
  });

  it('detecta cuando no hay nada que guardar', () => {
    const r = computeObjectiveProgress(
      objetivo({ progressPercentage: 50, status: 'InProgress' }),
      [hecha(1), pendiente(2)]
    )!;
    expect(r.changed).toBe(false);
  });
});
