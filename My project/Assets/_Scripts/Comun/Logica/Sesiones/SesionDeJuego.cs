using System;
using System.Collections.Generic;

// Datos de una sesión de juego que la tablet sube al servicio externo (ver quality/integracion-externa.md).
// Clase C# pura: los ids se generan UNA vez al crear la sesión, así un reintento manda exactamente lo mismo
// y el servidor no duplica nada.
public class SesionDeJuego
{
    public Guid Id { get; set; }
    public Guid JuegoId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime? Fin { get; set; }
    public int DuracionActivaSeg { get; set; }      // tiempo activo, sin contar la pausa
    public int PausadoSeg { get; set; }
    public bool Completada { get; set; }
    public string Dificultad { get; set; }          // "facil", "medio" o "dificil"
    public int? Nivel { get; set; }
    public MetricasSesion Metricas { get; set; }    // opcional
    public List<DetalleMetrica> Detalles { get; } = new List<DetalleMetrica>();
}

public class MetricasSesion
{
    public Guid Id { get; set; }
    public int? Aciertos { get; set; }              // null en juegos sin aciertos (Línea)
    public int? Errores { get; set; }
    public double? TiempoRespuestaMedioMs { get; set; }
    public int PistasUsadas { get; set; }
    public DateTime? RegistradaEn { get; set; }
}

public class DetalleMetrica
{
    public Guid Id { get; set; }
    public string Clave { get; set; }               // única dentro de la sesión
    public double Valor { get; set; }
}
