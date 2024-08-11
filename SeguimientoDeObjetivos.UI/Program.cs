using System;
using System.Collections.Generic;
using System.Linq;
using SeguimientoDeObjetivos.Data;
using SeguimientoDeObjetivos.Entities;
using SeguimientoDeObjetivos.Services;

namespace SeguimientoDeObjetivos.UI
{
    class Program
    {
        static ServicioObjetivo servicio = new ServicioObjetivo(new RepositorioObjetivo());

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("╔══════════════════════════════════════════════════╗");
                Console.WriteLine("║             Organiza, Enfócate, Logra            ║");
                Console.WriteLine("╚══════════════════════════════════════════════════╝");
                Console.WriteLine($"             Seguimiento de Objetivos  ({DateTime.Now.ToString("dd/MM/yyyy")})");
                Console.WriteLine("────────────────────────────────────────────────────");
                Console.WriteLine("Frase del dia:");
                Console.WriteLine("\"El éxito no es la clave de la felicidad. La felicidad es la clave del éxito.\"");
                Console.WriteLine("────────────────────────────────────────────────────");
                Console.WriteLine("Elige una opción para comenzar:");
                Console.WriteLine();
                Console.WriteLine("  1. -> Crear un nuevo objetivo");
                Console.WriteLine("  2. -> Editar un objetivo existente");
                Console.WriteLine("  3. -> Eliminar un objetivo");
                Console.WriteLine("  4. -> Ver todos los objetivos");
                Console.WriteLine("  5. -> Marcar tarea como completada");
                Console.WriteLine("  6. -> Salir del programa");
                Console.WriteLine();
                Console.WriteLine("────────────────────────────────────────────────────");
                Console.Write("Por favor, seleccione una opción: ");

                var opcion = Console.ReadLine();

                switch (opcion)
                {
                    case "1":
                        CrearObjetivo();
                        break;
                    case "2":
                        EditarObjetivo();
                        break;
                    case "3":
                        EliminarObjetivo();
                        break;
                    case "4":
                        VerObjetivos();
                        break;
                    case "5":
                        MarcarTareaComoCompletada();
                        break;
                    case "6":
                        return;
                    default:
                        Console.WriteLine("Opción inválida, presione una tecla para continuar...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void CrearObjetivo()
        {
            Console.Clear();
            Console.WriteLine("=== Crear un Nuevo Objetivo ===");
            Console.Write("Título: ");
            string titulo = Console.ReadLine();
            Console.Write("Descripción: ");
            string descripcion = Console.ReadLine();

            DateTime fechaInicio;
            while (!DateTime.TryParse(Console.ReadLine(), out fechaInicio))
            {
                Console.Write("Fecha de inicio (yyyy-mm-dd): ");
            }

            DateTime fechaFin;
            while (!DateTime.TryParse(Console.ReadLine(), out fechaFin))
            {
                Console.Write("Fecha de fin (yyyy-mm-dd): ");
            }

            var objetivo = new Objetivo
            {
                Titulo = titulo,
                Descripcion = descripcion,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            // Agregar tareas
            while (true)
            {
                Console.Write("¿Desea agregar una tarea? (s/n): ");
                string respuesta = Console.ReadLine();
                if (respuesta.ToLower() == "n") break;

                Console.Write("Nombre de la tarea: ");
                string nombreTarea = Console.ReadLine();
                objetivo.Tareas.Add(new Tarea { Nombre = nombreTarea, Completada = false });
            }

            servicio.CrearObjetivo(objetivo);
            Console.WriteLine("Objetivo creado exitosamente. Presione una tecla para continuar...");
            Console.ReadKey();
        }

        static void EditarObjetivo()
        {
            VerObjetivos();
            Console.Write("Seleccione el ID del objetivo a editar: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("ID inválido. Presione una tecla para continuar...");
                Console.ReadKey();
                return;
            }

            var objetivo = servicio.ObtenerPorId(id);
            if (objetivo == null)
            {
                Console.WriteLine("Objetivo no encontrado. Presione una tecla para continuar...");
                Console.ReadKey();
                return;
            }

            Console.Write("Nuevo Título (dejar en blanco para mantener el actual): ");
            string nuevoTitulo = Console.ReadLine();
            if (!string.IsNullOrEmpty(nuevoTitulo)) objetivo.Titulo = nuevoTitulo;

            Console.Write("Nueva Descripción (dejar en blanco para mantener la actual): ");
            string nuevaDescripcion = Console.ReadLine();
            if (!string.IsNullOrEmpty(nuevaDescripcion)) objetivo.Descripcion = nuevaDescripcion;

            Console.Write("Nueva Fecha de inicio (yyyy-mm-dd, dejar en blanco para mantener la actual): ");
            string nuevaFechaInicio = Console.ReadLine();
            if (!string.IsNullOrEmpty(nuevaFechaInicio) && DateTime.TryParse(nuevaFechaInicio, out DateTime fechaInicio))
            {
                objetivo.FechaInicio = fechaInicio;
            }

            Console.Write("Nueva Fecha de fin (yyyy-mm-dd, dejar en blanco para mantener la actual): ");
            string nuevaFechaFin = Console.ReadLine();
            if (!string.IsNullOrEmpty(nuevaFechaFin) && DateTime.TryParse(nuevaFechaFin, out DateTime fechaFin))
            {
                objetivo.FechaFin = fechaFin;
            }

            servicio.ActualizarObjetivo(objetivo);
            Console.WriteLine("Objetivo editado exitosamente. Presione una tecla para continuar...");
            Console.ReadKey();
        }

        static void EliminarObjetivo()
        {
            VerObjetivos();
            Console.Write("Seleccione el ID del objetivo a eliminar: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("ID inválido. Presione una tecla para continuar...");
                Console.ReadKey();
                return;
            }

            servicio.EliminarObjetivo(id);
            Console.WriteLine("Objetivo eliminado exitosamente. Presione una tecla para continuar...");
            Console.ReadKey();
        }

        static void VerObjetivos()
        {
            Console.Clear();
            Console.WriteLine("=== Lista de Objetivos ===");
            var objetivos = servicio.ObtenerTodos();
            foreach (var objetivo in objetivos)
            {
                Console.WriteLine($"ID: {objetivo.Id}, Título: {objetivo.Titulo}, Progreso: {objetivo.ObtenerProgreso():0.00}%");
                foreach (var tarea in objetivo.Tareas)
                {
                    Console.WriteLine($"\tTarea: {tarea.Nombre} - {(tarea.Completada ? "Completada" : "Pendiente")}");
                }
            }
            Console.WriteLine("Presione una tecla para continuar...");
            Console.ReadKey();
        }

        static void MarcarTareaComoCompletada()
        {
            VerObjetivos();
            Console.Write("Seleccione el ID del objetivo para marcar tareas como completadas: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("ID inválido. Presione una tecla para continuar...");
                Console.ReadKey();
                return;
            }

            var objetivo = servicio.ObtenerPorId(id);
            if (objetivo == null)
            {
                Console.WriteLine("Objetivo no encontrado. Presione una tecla para continuar...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Tareas del objetivo:");
            for (int i = 0; i < objetivo.Tareas.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {objetivo.Tareas[i].Nombre} - {(objetivo.Tareas[i].Completada ? "Completada" : "Pendiente")}");
            }

            Console.Write("Seleccione el número de la tarea que desea marcar como completada: ");
            if (!int.TryParse(Console.ReadLine(), out int tareaIndex) || tareaIndex < 1 || tareaIndex > objetivo.Tareas.Count)
            {
                Console.WriteLine("Número de tarea inválido. Presione una tecla para continuar...");
                Console.ReadKey();
                return;
            }

            objetivo.Tareas[tareaIndex - 1].Completada = true;
            servicio.ActualizarObjetivo(objetivo);
            Console.WriteLine("Tarea marcada como completada exitosamente. Presione una tecla para continuar...");
            Console.ReadKey();
        }
    }
}
