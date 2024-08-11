// File: Objetivo.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace SeguimientoDeObjetivos.Entities
{
    public class Objetivo
    {
        public int Id { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public List<Tarea> Tareas { get; set; } = new List<Tarea>();

        public double ObtenerProgreso()
        {
            if (Tareas.Count == 0) return 0;
            return (Tareas.Count(t => t.Completada) / (double)Tareas.Count) * 100;
        }
    }
}
