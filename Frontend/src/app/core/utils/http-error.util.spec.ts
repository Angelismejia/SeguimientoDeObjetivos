import { describe, it, expect } from 'vitest';
import { mensajeDeError } from './http-error.util';

const POR_DEFECTO = 'No se pudo crear la tarea. Intenta de nuevo.';

// Lo que devuelve GlobalExceptionHandler del backend.
const problemDetails = (status: number, detail: string) => ({
  status,
  error: { status, title: 'Bad Request', detail }
});

// Lo que devuelve [ApiController] solo cuando falla un [Required]/[MaxLength].
const validationProblem = (errors: Record<string, string[]>) => ({
  status: 400,
  error: { status: 400, title: 'One or more validation errors occurred.', errors }
});

describe('mensajeDeError', () => {
  // El caso que motivo el helper: el backend explicaba el problema en español y
  // el front lo tiraba a la basura para mostrar "Intenta de nuevo".
  it('muestra el detail del backend cuando viene', () => {
    const e = problemDetails(400, 'Este usuario no acepta seguidores.');
    expect(mensajeDeError(e, POR_DEFECTO)).toBe('Este usuario no acepta seguidores.');
  });

  it('ignora un detail vacio o en blanco', () => {
    expect(mensajeDeError(problemDetails(400, '   '), POR_DEFECTO)).toBe(POR_DEFECTO);
  });

  // Los textos de DataAnnotations son del framework y estan en ingles: se
  // reemplazan por un aviso propio en vez de mostrarlos.
  it('ante una validacion de DTO avisa del formulario, sin textos en ingles', () => {
    const e = validationProblem({
      Title: ['The field Title must be a string with a maximum length of 150.']
    });
    const msg = mensajeDeError(e, POR_DEFECTO);
    expect(msg).toContain('formulario');
    expect(msg).not.toContain('maximum length');
  });

  it('el detail gana sobre errors si vienen los dos', () => {
    const e = { status: 400, error: { detail: 'Ya existe una categoría con ese nombre.', errors: { Name: ['x'] } } };
    expect(mensajeDeError(e, POR_DEFECTO)).toBe('Ya existe una categoría con ese nombre.');
  });

  it('distingue servidor caido de error del servidor', () => {
    expect(mensajeDeError({ status: 0 }, POR_DEFECTO)).toContain('conectar');
    expect(mensajeDeError({ status: 504 }, POR_DEFECTO)).toContain('conectar');
    expect(mensajeDeError({ status: 500 }, POR_DEFECTO)).toContain('servidor');
  });

  it('avisa del limite de intentos', () => {
    expect(mensajeDeError({ status: 429 }, POR_DEFECTO)).toContain('Demasiados intentos');
  });

  it('403 y 404 tienen mensaje propio', () => {
    expect(mensajeDeError({ status: 403 }, POR_DEFECTO)).toContain('permiso');
    expect(mensajeDeError({ status: 404 }, POR_DEFECTO)).toContain('ya no existe');
  });

  // El login depende de esto: su 401 debe decir "Usuario o contraseña
  // incorrectos", no un texto generico sobre permisos.
  it('en 400 y 401 sin cuerpo util respeta el mensaje del que llama', () => {
    expect(mensajeDeError({ status: 401 }, 'Usuario o contraseña incorrectos')).toBe('Usuario o contraseña incorrectos');
    expect(mensajeDeError({ status: 400 }, POR_DEFECTO)).toBe(POR_DEFECTO);
  });

  it('no se rompe con un error vacio o nulo', () => {
    expect(mensajeDeError(null, POR_DEFECTO)).toBe(POR_DEFECTO);
    expect(mensajeDeError({}, POR_DEFECTO)).toBe(POR_DEFECTO);
  });
});
