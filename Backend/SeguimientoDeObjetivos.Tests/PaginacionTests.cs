using Application.Common;

namespace SeguimientoDeObjetivos.Tests;

// El tamanio de pagina lo propone el front, pero quien llama puede mandar
// cualquier numero. Sin el tope, ?limit=1000000 volvia a traer todo de una vez
// y la paginacion no servia de nada.
public class PaginacionTests
{
    [Theory]
    [InlineData(20, 20)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(1000000, 100)]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    public void El_limite_queda_entre_1_y_el_maximo(int pedido, int esperado)
    {
        Assert.Equal(esperado, Paginacion.Limite(pedido));
    }
}
