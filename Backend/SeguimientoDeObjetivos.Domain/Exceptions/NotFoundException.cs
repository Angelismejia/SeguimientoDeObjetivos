namespace Domain.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string name, int id)
            : base($"{name} con id {id} no fue encontrado.") { }
    }
}
