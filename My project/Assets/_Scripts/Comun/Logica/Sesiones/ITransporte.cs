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

public class ErrorDeRedException : Exception
{
    public ErrorDeRedException(string mensaje) : base(mensaje) { }
}

// Frontera con el servicio externo. En Unity la implementa UnityWebRequest.
public interface ITransporte
{
    RespuestaHttp Enviar(string ruta, string json, string token);
}
