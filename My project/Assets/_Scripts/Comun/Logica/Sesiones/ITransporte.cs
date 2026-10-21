using System;

public class RespuestaHttp
{
    public int Codigo { get; }
    public string Cuerpo { get; }

    public RespuestaHttp(int codigo, string cuerpo = "")
    {
        Codigo = codigo;
        Cuerpo = cuerpo;
    }
}

// Falla de red: sin conexión, timeout, DNS. Se distingue de una respuesta HTTP con error.
public class ErrorDeRedException : Exception
{
    public ErrorDeRedException(string mensaje) : base(mensaje) { }
}

// Frontera con el sistema externo. En Unity la implementa un envoltorio de UnityWebRequest.
// En las pruebas, un doble que devuelve respuestas guionadas.
public interface ITransporte
{
    // POST de un JSON a una ruta, con el token del usuario. Lanza ErrorDeRedException si no hay conexión.
    RespuestaHttp Enviar(string ruta, string json, string token);
}
