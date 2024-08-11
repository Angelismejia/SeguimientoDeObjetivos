// File: ServicioObjetivo.cs
using System.Collections.Generic;
using SeguimientoDeObjetivos.Data;
using SeguimientoDeObjetivos.Entities;

namespace SeguimientoDeObjetivos.Services
{
    public class ServicioObjetivo
    {
        private readonly RepositorioObjetivo _repositorio;

        public ServicioObjetivo(RepositorioObjetivo repositorio)
        {
            _repositorio = repositorio;
        }

        public IEnumerable<Objetivo> ObtenerTodos() => _repositorio.ObtenerTodos();

        public Objetivo ObtenerPorId(int id) => _repositorio.ObtenerPorId(id);

        public void CrearObjetivo(Objetivo objetivo) => _repositorio.Agregar(objetivo);

        public void ActualizarObjetivo(Objetivo objetivo) => _repositorio.Actualizar(objetivo);

        public void EliminarObjetivo(int id) => _repositorio.Eliminar(id);
    }
}
