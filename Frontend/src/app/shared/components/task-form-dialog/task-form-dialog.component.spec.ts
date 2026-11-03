import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { TaskFormDialogComponent, TaskSaved } from './task-form-dialog.component';
import { TaskService } from '../../../core/services/task.service';
import { AuthService } from '../../../core/services/auth.service';
import { CategoryService } from '../../../core/services/category.service';
import { TaskItem, UpdateTaskDto } from '../../../core/models/task.model';

// El formulario estaba copiado en el dashboard y en Tareas. Al juntarlo, lo que
// no se puede perder es lo que ya se habia arreglado una vez: editar una tarea
// reenvia los campos que el formulario no muestra (recordatorio, repeticion).

const tarea: TaskItem = {
  id: 7,
  title: 'Correr',
  scheduledDate: '2026-11-02T00:00:00',
  scheduledTime: '07:30:00',
  priority: 'High',
  status: 'Pending',
  isRecurring: true,
  recurrenceType: 'Weekly',
  reminderMinutesBefore: 15,
  repeatEveryWeeks: 2,
  endRepeatDate: '2026-12-31T00:00:00',
  userId: 1,
  objectiveId: 3,
  createdAt: '2026-11-01T10:00:00Z'
};

class TaskServiceFalso {
  creada: unknown = null;
  actualizada: UpdateTaskDto | null = null;

  create(data: unknown) {
    this.creada = data;
    return of({ ...tarea, id: 99, objectiveId: undefined });
  }

  update(_id: number, data: UpdateTaskDto) {
    this.actualizada = data;
    return of({ ...tarea, objectiveId: data.objectiveId });
  }
}

describe('TaskFormDialogComponent', () => {
  let servicio: TaskServiceFalso;

  beforeEach(() => {
    servicio = new TaskServiceFalso();
    TestBed.configureTestingModule({
      imports: [TaskFormDialogComponent],
      providers: [
        { provide: TaskService, useValue: servicio },
        { provide: AuthService, useValue: { getUserId: () => 1 } },
        { provide: CategoryService, useValue: {} }
      ]
    });
  });

  function abrir(task: TaskItem | null, defaultDate = '2026-11-05') {
    const fixture = TestBed.createComponent(TaskFormDialogComponent);
    fixture.componentInstance.task = task;
    fixture.componentInstance.defaultDate = defaultDate;
    fixture.detectChanges();
    return fixture.componentInstance;
  }

  it('al crear propone el dia que le pasa la pantalla', () => {
    const form = abrir(null, '2026-11-05');

    expect(form.editing).toBe(false);
    expect(form.form.value.scheduledDate).toBe('2026-11-05');
  });

  it('al editar carga la tarea en el formulario', () => {
    const form = abrir(tarea);

    expect(form.editing).toBe(true);
    expect(form.form.value.title).toBe('Correr');
    expect(form.form.value.scheduledDate).toBe('2026-11-02');
    expect(form.form.value.scheduledTime).toBe('07:30');
  });

  it('al editar reenvia el recordatorio y la repeticion aunque no esten en el formulario', () => {
    const form = abrir(tarea);
    form.form.patchValue({ title: 'Correr 5 km' });

    form.submit();

    expect(servicio.actualizada?.title).toBe('Correr 5 km');
    expect(servicio.actualizada?.reminderMinutesBefore).toBe(15);
    expect(servicio.actualizada?.repeatEveryWeeks).toBe(2);
    expect(servicio.actualizada?.endRepeatDate).toBe('2026-12-31T00:00:00');
  });

  it('avisa el objetivo anterior para que la pantalla recalcule los dos', () => {
    const form = abrir(tarea);
    let recibido: TaskSaved | undefined;
    form.saved.subscribe(e => recibido = e);
    form.form.patchValue({ objectiveId: 5 });

    form.submit();

    expect(recibido?.task.objectiveId).toBe(5);
    expect(recibido?.previousObjectiveId).toBe(3);
  });

  it('al crear emite la tarea nueva sin objetivo anterior', () => {
    const form = abrir(null);
    let recibido: TaskSaved | undefined;
    form.saved.subscribe(e => recibido = e);
    form.form.patchValue({ title: 'Leer' });

    form.submit();

    expect(recibido?.task.id).toBe(99);
    expect(recibido?.previousObjectiveId).toBeUndefined();
  });
});
