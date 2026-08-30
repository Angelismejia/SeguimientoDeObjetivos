/**
 * Traduce un error de HttpClient al mensaje que ve el usuario.
 *
 * El backend responde errores en dos formatos distintos, y la diferencia
 * importa:
 *
 * - `GlobalExceptionHandler` devuelve ProblemDetails con `detail`, un texto ya
 *   escrito en español y pensado para leerse ("No puedes seguir a este
 *   usuario"). Ese se muestra tal cual: es la explicacion real.
 * - Las validaciones de los DTOs ([Required], [MaxLength]...) las contesta
 *   [ApiController] solo, como ValidationProblemDetails: `detail` viene vacio y
 *   los mensajes van en `errors`, generados por el framework y en ingles
 *   ("The field Title must be a string with a maximum length of 150."). Esos NO
 *   se muestran: se reemplazan por un aviso propio en español.
 *
 * `porDefecto` es el mensaje de la accion concreta ("No se pudo crear la
 * tarea..."), que se usa cuando el error no trae nada mejor.
 */
export function mensajeDeError(error: any, porDefecto: string): string {
  const status = error?.status;
  const cuerpo = error?.error;

  // Explicacion del backend, ya redactada para el usuario.
  const detail = typeof cuerpo?.detail === 'string' ? cuerpo.detail.trim() : '';
  if (detail) return detail;

  // Validacion de DTO: sabemos que el problema esta en el formulario, pero los
  // textos del framework no son mostrables.
  if (status === 400 && cuerpo?.errors && typeof cuerpo.errors === 'object') {
    return 'Revisa los datos del formulario: hay campos incompletos o demasiado largos.';
  }

  if (status === 0 || status === 504) {
    return 'No se pudo conectar con el servidor. Puede estar iniciandose: espera unos segundos y vuelve a intentar.';
  }
  if (status === 429) {
    return 'Demasiados intentos. Espera un momento antes de volver a probar.';
  }
  if (status === 403) {
    return 'No tienes permiso para hacer esto.';
  }
  if (status === 404) {
    return 'Ese elemento ya no existe. Puede que se haya borrado.';
  }
  if (status >= 500) {
    return 'Hubo un problema en el servidor. Intenta de nuevo en unos segundos.';
  }

  // 400 y 401 sin cuerpo util: el que llama sabe que estaba intentando el
  // usuario ("Usuario o contraseña incorrectos"), y eso gana a cualquier texto
  // generico que pudieramos poner aca.
  return porDefecto;
}
