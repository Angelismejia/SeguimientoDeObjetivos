// File: RepositorioObjetivo.cs
using System.Collections.Generic;
using System.Linq;
using SeguimientoDeObjetivos.Entities;

namespace SeguimientoDeObjetivos.Data
{
    public class RepositorioObjetivo
    {
        private readonly List<Objetivo> _objetivos = new List<Objetivo>();
        private int _idCounter = 1;

        public IEnumerable<Objetivo> ObtenerTodos() => _objetivos;

        public Objetivo ObtenerPorId(int id) => _objetivos.FirstOrDefault(o => o.Id == id);

        public void Agregar(Objetivo objetivo)
        {
            objetivo.Id = _idCounter++;
            _objetivos.Add(objetivo);
        }

        public void Actualizar(Objetivo objetivo)
        {
            var existente = ObtenerPorId(objetivo.Id);
            if (existente != null)
            {
                existente.Titulo = objetivo.Titulo;
                existente.Descripcion = objetivo.Descripcion;
                existente.FechaInicio = objetivo.FechaInicio;
                existente.FechaFin = objetivo.FechaFin;
            }
        }

        public void Eliminar(int id)
        {
            var objetivo = ObtenerPorId(id);
            if (objetivo != null)
            {
                _objetivos.Remove(objetivo);
            }
        }
    }
}
