namespace Application.Common
{
    // Los listados que crecen sin techo (alertas, diario, chat) se piden de a
    // pedazos. El tamanio lo propone el front, pero el backend lo acota: sin el
    // tope, un ?limit=1000000 volvia a traer todo de una vez.
    //
    // Se pagina por cursor ("los N anteriores a este id"), no por numero de
    // pagina: si entra una fila nueva mientras se lee, con ?page=2 todo se corre
    // un lugar y se repite un elemento; con el cursor no.
    public static class Paginacion
    {
        public const int Maximo = 100;

        public static int Limite(int pedido) => Math.Clamp(pedido, 1, Maximo);
    }
}
