import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Valida que el campo "hasta" de un formulario no sea anterior al "desde".
 *
 * Va sobre el FormGroup y no sobre un control, porque la regla necesita mirar
 * dos campos a la vez: es exactamente lo que las DataAnnotations del backend no
 * pueden expresar, y por eso hasta ahora no lo validaba nadie.
 *
 * Compara los valores como texto a proposito. Tanto `<input type="date">`
 * ("YYYY-MM-DD") como `<input type="time">` ("HH:mm") devuelven strings con
 * ceros a la izquierda y de ancho fijo, asi que el orden alfabetico coincide con
 * el cronologico. Pasarlos por `new Date(...)` no aportaria nada y reintroduce
 * el problema de huso horario que ya se documento en task-status.util.ts.
 *
 * Un rango a medias no es un error: si falta cualquiera de las dos puntas no hay
 * nada que comparar, y ambos campos suelen ser opcionales.
 */
export function rangoOrdenado(desde: string, hasta: string, error: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const inicio = group.get(desde)?.value;
    const fin = group.get(hasta)?.value;

    if (!inicio || !fin) return null;

    return fin < inicio ? { [error]: true } : null;
  };
}
