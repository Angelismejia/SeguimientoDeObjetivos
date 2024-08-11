using System;
namespace SeguimientoDeObjetivos.Entities
{
    public class Tarea
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public bool Completada { get; set; }
    }
}
