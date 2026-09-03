import { HttpErrorResponse } from '@angular/common/http';
import { ErrorHandler, Injectable } from '@angular/core';
import { showErrorBanner } from './error-banner';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.error(error);
    showErrorBanner(describirError(error));
  }
}

/**
 * Saca un mensaje legible de cualquier cosa que llegue como error.
 *
 * Antes esto era `String(error)`, y ahi estaba el problema: HttpErrorResponse
 * de Angular *implementa* Error pero no lo *extiende*, asi que `instanceof
 * Error` da false y String() lo volvia el literal "[object Object]". El
 * resultado era que el backend mandaba un motivo perfectamente claro en el
 * ProblemDetails y el banner lo tapaba.
 */
function describirError(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    return describirErrorHttp(error);
  }

  if (error instanceof Error) {
    return error.stack ?? error.message;
  }

  // Ultimo recurso: un objeto suelto se ve mejor como JSON que como
  // "[object Object]".
  if (error && typeof error === 'object') {
    try {
      return JSON.stringify(error);
    } catch {
      return Object.prototype.toString.call(error);
    }
  }

  return String(error);
}

function describirErrorHttp(error: HttpErrorResponse): string {
  // Status 0 = la peticion nunca llego. Decir "HTTP 0" no le sirve a nadie.
  if (error.status === 0) {
    return 'No se pudo conectar con el servidor. Revisa tu conexión e intenta de nuevo.';
  }

  const cuerpo = error.error;

  if (typeof cuerpo === 'string' && cuerpo.trim()) {
    return `${cuerpo} (HTTP ${error.status})`;
  }

  if (cuerpo && typeof cuerpo === 'object') {
    // Errores de validacion de ASP.NET: { errors: { Campo: ["motivo"] } }
    const errores = (cuerpo as { errors?: Record<string, string[]> }).errors;
    if (errores) {
      const detalle = Object.entries(errores)
        .map(([campo, motivos]) => `${campo}: ${motivos.join(', ')}`)
        .join(' | ');
      if (detalle) return `${detalle} (HTTP ${error.status})`;
    }

    // ProblemDetails: el "detail" es el mensaje escrito para que lo lea una
    // persona, asi que va primero.
    const problema = cuerpo as { detail?: string; title?: string; message?: string };
    const detalle = problema.detail ?? problema.title ?? problema.message;
    if (detalle) return `${detalle} (HTTP ${error.status})`;
  }

  return `${error.message} (HTTP ${error.status})`;
}
