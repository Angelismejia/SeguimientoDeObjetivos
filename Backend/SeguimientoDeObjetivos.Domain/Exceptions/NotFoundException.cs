namespace Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, int id)
            : base($"{name} con id {id} no fue encontrado.") { }

        public NotFoundException(string name, string identifier)
            : base($"{name} '{identifier}' no fue encontrado.") { }
    }
}
