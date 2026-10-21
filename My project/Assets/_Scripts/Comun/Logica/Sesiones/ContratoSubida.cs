using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Contrato de subida de una sesión (función upload_game_session de Supabase):
// arma el JSON exacto y valida los datos ANTES de enviarlos, con las mismas reglas que la base.
public static class ContratoSubida
{
    public const string Ruta = "/rest/v1/rpc/upload_game_session";

    static readonly string[] Dificultades = { "facil", "medio", "dificil" };

    public static List<string> Validar(Guid perfilId, SesionDeJuego s)
    {
        var errores = new List<string>();
        if (s == null)
        {
            errores.Add("sesion nula");
            return errores;
        }

        if (perfilId == Guid.Empty) errores.Add("profile_id vacío");
        if (s.Id == Guid.Empty) errores.Add("session.id vacío");
        if (s.JuegoId == Guid.Empty) errores.Add("session.game_id vacío");
        if (s.Fin.HasValue && s.Fin.Value < s.Inicio) errores.Add("ended_at anterior a started_at");
        if (s.DuracionActivaSeg < 0) errores.Add("duration_sec negativo");
        if (s.PausadoSeg < 0) errores.Add("paused_sec negativo");
        if (s.Dificultad != null && Array.IndexOf(Dificultades, s.Dificultad) < 0)
            errores.Add("difficulty debe ser facil, medio o dificil");

        MetricasSesion m = s.Metricas;
        if (m != null)
        {
            if (m.Id == Guid.Empty) errores.Add("metrics.id vacío");
            if (m.Aciertos < 0 || m.Errores < 0 || m.PistasUsadas < 0) errores.Add("metrics con contadores negativos");
            if (m.TiempoRespuestaMedioMs.HasValue && !EsNumeroValido(m.TiempoRespuestaMedioMs.Value))
                errores.Add("avg_response_ms no es un número válido");
        }

        var claves = new HashSet<string>();
        foreach (DetalleMetrica d in s.Detalles)
        {
            if (string.IsNullOrEmpty(d.Clave)) errores.Add("details con metric_key vacía");
            else if (!claves.Add(d.Clave)) errores.Add("metric_key repetida: " + d.Clave);
            if (!EsNumeroValido(d.Valor)) errores.Add("metric_value no es un número válido: " + d.Clave);
            if (d.Id == Guid.Empty) errores.Add("details.id vacío: " + d.Clave);
        }
        return errores;
    }

    // JSON del cuerpo del POST. "metrics" y "details" solo se mandan si hay datos.
    public static string ConstruirPayload(Guid perfilId, SesionDeJuego s)
    {
        var j = new StringBuilder();
        j.Append("{\"profile_id\":").Append(Texto(perfilId.ToString()));

        j.Append(",\"session\":{");
        j.Append("\"id\":").Append(Texto(s.Id.ToString()));
        j.Append(",\"game_id\":").Append(Texto(s.JuegoId.ToString()));
        j.Append(",\"started_at\":").Append(Fecha(s.Inicio));
        j.Append(",\"ended_at\":").Append(s.Fin.HasValue ? Fecha(s.Fin.Value) : "null");
        j.Append(",\"duration_sec\":").Append(s.DuracionActivaSeg.ToString(CultureInfo.InvariantCulture));
        j.Append(",\"paused_sec\":").Append(s.PausadoSeg.ToString(CultureInfo.InvariantCulture));
        j.Append(",\"completed\":").Append(s.Completada ? "true" : "false");
        j.Append(",\"difficulty\":").Append(s.Dificultad == null ? "null" : Texto(s.Dificultad));
        j.Append(",\"level\":").Append(Entero(s.Nivel));
        j.Append('}');

        MetricasSesion m = s.Metricas;
        if (m != null)
        {
            j.Append(",\"metrics\":{");
            j.Append("\"id\":").Append(Texto(m.Id.ToString()));
            j.Append(",\"correct_answers\":").Append(Entero(m.Aciertos));
            j.Append(",\"wrong_answers\":").Append(Entero(m.Errores));
            j.Append(",\"avg_response_ms\":").Append(m.TiempoRespuestaMedioMs.HasValue ? Numero(m.TiempoRespuestaMedioMs.Value) : "null");
            j.Append(",\"hints_used\":").Append(m.PistasUsadas.ToString(CultureInfo.InvariantCulture));
            if (m.RegistradaEn.HasValue) j.Append(",\"recorded_at\":").Append(Fecha(m.RegistradaEn.Value));
            j.Append('}');
        }

        if (s.Detalles.Count > 0)
        {
            j.Append(",\"details\":[");
            for (int i = 0; i < s.Detalles.Count; i++)
            {
                DetalleMetrica d = s.Detalles[i];
                if (i > 0) j.Append(',');
                j.Append("{\"id\":").Append(Texto(d.Id.ToString()));
                j.Append(",\"metric_key\":").Append(Texto(d.Clave));
                j.Append(",\"metric_value\":").Append(Numero(d.Valor)).Append('}');
            }
            j.Append(']');
        }

        j.Append('}');
        return j.ToString();
    }

    static bool EsNumeroValido(double valor) => !double.IsNaN(valor) && !double.IsInfinity(valor);

    // Siempre con punto decimal, sin importar la cultura del dispositivo (ej.: es-AR usa coma)
    static string Numero(double valor) => valor.ToString("R", CultureInfo.InvariantCulture);

    static string Entero(int? valor) => valor.HasValue ? valor.Value.ToString(CultureInfo.InvariantCulture) : "null";

    static string Fecha(DateTime fecha) =>
        Texto(fecha.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));

    static string Texto(string texto)
    {
        var e = new StringBuilder("\"");
        foreach (char c in texto)
        {
            switch (c)
            {
                case '"': e.Append("\\\""); break;
                case '\\': e.Append("\\\\"); break;
                case '\n': e.Append("\\n"); break;
                case '\r': e.Append("\\r"); break;
                case '\t': e.Append("\\t"); break;
                default:
                    if (c < ' ') e.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else e.Append(c);
                    break;
            }
        }
        return e.Append('"').ToString();
    }
}
