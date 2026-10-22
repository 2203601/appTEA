using System;
using System.Collections.Generic;

// Doble de prueba del sistema externo: devuelve respuestas guionadas y registra lo que se le envió.
public class TransporteFalso : ITransporte
{
    readonly Queue<object> respuestas = new Queue<object>();
    object ultima = new RespuestaHttp(200);

    public List<string> Rutas { get; } = new List<string>();
    public List<string> Cuerpos { get; } = new List<string>();
    public List<string> Tokens { get; } = new List<string>();

    public TransporteFalso Responde(int codigo)
    {
        respuestas.Enqueue(new RespuestaHttp(codigo));
        return this;
    }

    public TransporteFalso SinRed()
    {
        respuestas.Enqueue(new ErrorDeRedException("sin conexión"));
        return this;
    }

    public TransporteFalso LuegoSiempre(int codigo)
    {
        ultima = new RespuestaHttp(codigo);
        return this;
    }

    public RespuestaHttp Enviar(string ruta, string json, string token)
    {
        Rutas.Add(ruta);
        Cuerpos.Add(json);
        Tokens.Add(token);
        object siguiente = respuestas.Count > 0 ? respuestas.Dequeue() : ultima;
        if (siguiente is ErrorDeRedException error) throw error;
        return (RespuestaHttp)siguiente;
    }
}

public static class SesionesDePrueba
{
    public static readonly Guid Perfil = new Guid("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Juego = new Guid("00000000-0000-0000-0000-000000000001");
    static readonly DateTime Inicio = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    public static SesionDeJuego Basica(string id = "22222222-2222-2222-2222-222222222222")
    {
        return new SesionDeJuego
        {
            Id = new Guid(id),
            JuegoId = Juego,
            Inicio = Inicio,
            Fin = Inicio.AddSeconds(130),
            DuracionActivaSeg = 120,
            PausadoSeg = 10,
            Completada = true,
            Dificultad = "facil",
            Nivel = 1,
        };
    }

    public static SesionDeJuego ConMetricas()
    {
        SesionDeJuego s = Basica();
        s.Metricas = new MetricasSesion
        {
            Id = new Guid("33333333-3333-3333-3333-333333333333"),
            Aciertos = 5,
            Errores = 2,
            TiempoRespuestaMedioMs = 1234.5,
            PistasUsadas = 1,
            RegistradaEn = Inicio.AddSeconds(130),
        };
        s.Detalles.Add(new DetalleMetrica { Id = new Guid("44444444-4444-4444-4444-444444444444"), Clave = "first_try_pieces", Valor = 2 });
        return s;
    }

    public static SesionDeJuego Numero(int n) => Basica("00000000-0000-0000-0000-" + n.ToString("D12"));
}
