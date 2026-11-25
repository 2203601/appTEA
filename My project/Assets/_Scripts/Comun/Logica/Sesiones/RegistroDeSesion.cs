using System;
using System.Collections.Generic;

// Lleva la cuenta de una partida mientras se juega y la convierte en una SesionDeJuego al terminar.
// No usa Unity ni lee el reloj: quien lo usa pasa la hora, así se puede probar sin escenas.
public class RegistroDeSesion
{
    readonly Guid juegoId;
    readonly string dificultad;
    readonly int? nivel;
    readonly bool contarAciertos;
    readonly DateTime inicio;

    DateTime? pausaDesde;
    TimeSpan pausado = TimeSpan.Zero;
    int aciertos;
    int errores;
    int pistas;
    readonly List<string> claves = new List<string>();
    readonly Dictionary<string, double> detalles = new Dictionary<string, double>();
    SesionDeJuego finalizada;

    // contarAciertos = false en juegos sin aciertos ni errores (Escribir siguiendo la línea): se guardan como null
    public RegistroDeSesion(Guid juegoId, string dificultad, int? nivel, DateTime ahora, bool contarAciertos = true)
    {
        this.juegoId = juegoId;
        this.dificultad = dificultad;
        this.nivel = nivel;
        this.contarAciertos = contarAciertos;
        inicio = ahora;
    }

    public bool Terminada => finalizada != null;
    public bool EnPausa => pausaDesde.HasValue;
    public int Aciertos => aciertos;
    public int Errores => errores;
    public int Pistas => pistas;

    public void Pausar(DateTime ahora)
    {
        if (Terminada || EnPausa) return;
        pausaDesde = ahora;
    }

    public void Reanudar(DateTime ahora)
    {
        if (Terminada || !EnPausa) return;
        pausado += Positivo(ahora - pausaDesde.Value);
        pausaDesde = null;
    }

    public void RegistrarAcierto() { if (!Terminada) aciertos++; }
    public void RegistrarError() { if (!Terminada) errores++; }
    public void RegistrarPista() { if (!Terminada) pistas++; }

    // Métrica propia del juego: se suma al valor que ya tenga esa clave (ej.: salidas del camino)
    public void SumarDetalle(string clave, double valor)
    {
        if (Terminada || string.IsNullOrEmpty(clave)) return;
        detalles.TryGetValue(clave, out double actual);
        FijarDetalle(clave, actual + valor);
    }

    // Reemplaza el valor de esa clave (ej.: precisión final del trazo)
    public void FijarDetalle(string clave, double valor)
    {
        if (Terminada || string.IsNullOrEmpty(clave)) return;
        if (!detalles.ContainsKey(clave)) claves.Add(clave);
        detalles[clave] = valor;
    }

    // Se puede llamar más de una vez: devuelve siempre la misma sesión, con los mismos ids
    public SesionDeJuego Finalizar(DateTime ahora, bool completada)
    {
        if (finalizada != null) return finalizada;

        if (EnPausa) Reanudar(ahora);

        TimeSpan total = Positivo(ahora - inicio);
        TimeSpan activo = Positivo(total - pausado);

        var s = new SesionDeJuego
        {
            Id = Guid.NewGuid(),
            JuegoId = juegoId,
            Inicio = inicio,
            Fin = ahora < inicio ? inicio : ahora,
            DuracionActivaSeg = (int)Math.Round(activo.TotalSeconds),
            PausadoSeg = (int)Math.Round(pausado.TotalSeconds),
            Completada = completada,
            Dificultad = dificultad,
            Nivel = nivel,
            Metricas = new MetricasSesion
            {
                Id = Guid.NewGuid(),
                Aciertos = contarAciertos ? aciertos : (int?)null,
                Errores = contarAciertos ? errores : (int?)null,
                PistasUsadas = pistas,
                RegistradaEn = ahora
            }
        };
        foreach (string clave in claves)
            s.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = clave, Valor = detalles[clave] });

        finalizada = s;
        return s;
    }

    static TimeSpan Positivo(TimeSpan t) => t < TimeSpan.Zero ? TimeSpan.Zero : t;
}
