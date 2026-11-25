using System;

public enum ResultadoSubida
{
    Subida,
    Reintentar,
    NoAutorizado,
    Rechazada,
    Invalida
}

public class ClienteSubida
{
    readonly ITransporte transporte;

    public ClienteSubida(ITransporte transporte)
    {
        this.transporte = transporte;
    }

    public ResultadoSubida Subir(Guid perfilId, SesionDeJuego sesion, string token)
    {
        if (ContratoSubida.Validar(perfilId, sesion).Count > 0) return ResultadoSubida.Invalida;
        return SubirPayload(ContratoSubida.ConstruirPayload(perfilId, sesion), token);
    }

    // Reenvía un JSON ya armado (el que quedó guardado en la cola local)
    public ResultadoSubida SubirPayload(string json, string token)
    {
        if (string.IsNullOrEmpty(token)) return ResultadoSubida.NoAutorizado;
        try
        {
            return Clasificar(transporte.Enviar(ContratoSubida.Ruta, json, token).Codigo);
        }
        catch (ErrorDeRedException)
        {
            return ResultadoSubida.Reintentar;
        }
    }

    public static ResultadoSubida Clasificar(int codigoHttp)
    {
        if (codigoHttp >= 200 && codigoHttp < 300) return ResultadoSubida.Subida;
        if (codigoHttp == 401) return ResultadoSubida.NoAutorizado;
        if (codigoHttp == 408 || codigoHttp == 429 || codigoHttp >= 500) return ResultadoSubida.Reintentar;
        if (codigoHttp >= 400) return ResultadoSubida.Rechazada;
        return ResultadoSubida.Reintentar;   // respuesta inesperada: mejor no perder la sesión
    }
}
