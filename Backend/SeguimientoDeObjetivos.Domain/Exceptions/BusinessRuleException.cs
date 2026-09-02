namespace Domain.Exceptions
{
    /// <summary>
    /// Una regla de negocio que el usuario incumplio: seguirse a si mismo, una
    /// fecha de fin anterior a la de inicio, una contraseña que no coincide.
    /// Su mensaje esta escrito para leerse y se le devuelve tal cual al cliente.
    ///
    /// Existe para poder distinguirla de un fallo tecnico. Antes estos casos
    /// usaban InvalidOperationException, y GlobalExceptionHandler convertia en
    /// 400 CUALQUIER InvalidOperationException que le llegara. El problema es
    /// que hay muchas que no son del usuario: EF Core lanza una cuando pierde la
    /// conexion con SQL Server, asi que un arranque en frio de la BD terminaba
    /// contestando "400 Bad Request" con el texto interno de EF —"consider
    /// enabling transient error resiliency..."— mostrado en pantalla.
    /// </summary>
    public class BusinessRuleException : Exception
    {
        public BusinessRuleException(string message) : base(message) { }
    }
}
