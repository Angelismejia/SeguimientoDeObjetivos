/**
 * Convierte una marca de tiempo del backend al Date que realmente representa.
 *
 * Todas las entidades nacen con `DateTime.UtcNow`, pero las columnas son
 * `timestamp without time zone`: Npgsql las devuelve sin zona horaria y el JSON
 * llega como "2026-09-30T20:40:05", sin la Z del final.
 *
 * `new Date("2026-09-30T20:40:05")` interpreta ese texto como hora LOCAL. Como
 * el valor guardado es UTC, cada hora que muestra la app queda corrida por el
 * desfase de la zona (4 horas aqui): un mensaje enviado a las 16:40 se ve como
 * enviado a las 20:40.
 *
 * La Z se agrega solo si el texto no trae ya una zona, para que esto siga
 * funcionando el dia que el backend empiece a mandarla.
 *
 * Solo aplica a marcas de tiempo. Una fecha suelta ("2026-09-30", como
 * scheduledDate o entryDate) representa un dia, no un instante, y se deja como
 * esta: agregarle una zona la correria de dia.
 */
export function fechaDelBackend(valor: string | Date): Date {
  if (valor instanceof Date) return valor;

  const texto = valor.trim();
  const traeZona = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(texto);
  const traeHora = texto.includes('T');

  return new Date(traeZona || !traeHora ? texto : `${texto}Z`);
}
