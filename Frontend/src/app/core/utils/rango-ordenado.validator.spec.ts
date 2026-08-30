import { describe, it, expect } from 'vitest';
import { FormControl, FormGroup } from '@angular/forms';
import { rangoOrdenado } from './rango-ordenado.validator';

function grupo(desde: string | null, hasta: string | null) {
  return new FormGroup(
    { inicio: new FormControl(desde), fin: new FormControl(hasta) },
    { validators: rangoOrdenado('inicio', 'fin', 'finAntesDeInicio') }
  );
}

describe('rangoOrdenado', () => {
  // El caso que motivo el validador: se podia guardar un objetivo que terminaba
  // antes de empezar, porque las DataAnnotations miran un campo a la vez.
  it('marca error si el fin es anterior al inicio', () => {
    expect(grupo('2026-08-20', '2026-08-10').hasError('finAntesDeInicio')).toBe(true);
  });

  it('acepta un rango en orden', () => {
    expect(grupo('2026-08-10', '2026-08-20').valid).toBe(true);
  });

  it('acepta que ambas puntas sean el mismo dia', () => {
    expect(grupo('2026-08-10', '2026-08-10').valid).toBe(true);
  });

  // Los dos campos son opcionales: media fecha no es una fecha invalida.
  it('no exige que el rango este completo', () => {
    expect(grupo('2026-08-10', null).valid).toBe(true);
    expect(grupo(null, '2026-08-10').valid).toBe(true);
    expect(grupo(null, null).valid).toBe(true);
    expect(grupo('', '').valid).toBe(true);
  });

  it('sirve igual para horas, que es el otro uso', () => {
    expect(grupo('18:00', '09:00').hasError('finAntesDeInicio')).toBe(true);
    expect(grupo('09:00', '18:00').valid).toBe(true);
  });

  // Comparar como texto solo funciona si el formato tiene ancho fijo y ceros a
  // la izquierda, que es justamente lo que devuelven los inputs date y time.
  it('ordena bien con ceros a la izquierda', () => {
    expect(grupo('09:00', '10:00').valid).toBe(true);
    expect(grupo('2026-09-01', '2026-10-01').valid).toBe(true);
    expect(grupo('2026-10-01', '2026-09-01').hasError('finAntesDeInicio')).toBe(true);
  });

  it('deja el error en el grupo, no en los controles', () => {
    const g = grupo('2026-08-20', '2026-08-10');
    expect(g.get('fin')!.valid).toBe(true);
    expect(g.invalid).toBe(true);
  });
});
