import { fechaDelBackend } from './fecha.util';

describe('fechaDelBackend', () => {
  it('lee como UTC una marca de tiempo sin zona, que es como la manda el backend', () => {
    const fecha = fechaDelBackend('2026-09-30T20:40:05');

    expect(fecha.toISOString()).toBe('2026-09-30T20:40:05.000Z');
  });

  it('respeta la zona cuando el texto ya la trae', () => {
    expect(fechaDelBackend('2026-09-30T20:40:05Z').toISOString())
      .toBe('2026-09-30T20:40:05.000Z');

    expect(fechaDelBackend('2026-09-30T16:40:05-04:00').toISOString())
      .toBe('2026-09-30T20:40:05.000Z');
  });

  // Una fecha suelta es un dia, no un instante: si le agregaramos zona se correria.
  it('no toca una fecha sin hora', () => {
    expect(fechaDelBackend('2026-09-30').toISOString())
      .toBe('2026-09-30T00:00:00.000Z');
  });

  it('deja pasar un Date que ya viene armado', () => {
    const original = new Date('2026-09-30T20:40:05Z');

    expect(fechaDelBackend(original)).toBe(original);
  });
});
