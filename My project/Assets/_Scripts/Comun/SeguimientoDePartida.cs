using System;
using System.Text;
using UnityEngine;

// Sigue una partida de cualquier juego: lleva el RegistroDeSesion, descuenta las pausas y entrega
// la sesión al terminar (completada) o al salir antes (incompleta).
// El GameManager lo agrega con Iniciar(), así no hace falta tocar las escenas.
public class SeguimientoDePartida : MonoBehaviour
{
    public RegistroDeSesion Registro { get; private set; }

    bool enSegundoPlano;

    public static SeguimientoDePartida Iniciar(GameObject objeto, Guid juegoId, Difficulty dificultad, bool contarAciertos = true)
    {
        var partida = objeto.AddComponent<SeguimientoDePartida>();
        // Nivel y dificultad: 1 facil, 2 medio, 3 dificil
        partida.Registro = new RegistroDeSesion(juegoId, dificultad.ToString().ToLowerInvariant(),
            (int)dificultad + 1, DateTime.UtcNow, contarAciertos);
        return partida;
    }

    // Se puede llamar más de una vez: solo la primera entrega la sesión
    public void Terminar(bool completada)
    {
        if (Registro == null || Registro.Terminada) return;

        SesionDeJuego sesion = Registro.Finalizar(DateTime.UtcNow, completada);
        Debug.Log(Resumen(sesion));

        if (SubidorDeSesiones.Instancia != null) SubidorDeSesiones.Instancia.Registrar(sesion);
        else Debug.Log("[Sesion] No se guardó: no hay un SubidorDeSesiones en la escena.");
    }

    void Update()
    {
        ActualizarPausa();
    }

    // La app pasó a segundo plano: ese tiempo no cuenta como juego
    void OnApplicationPause(bool pausada)
    {
        enSegundoPlano = pausada;
        ActualizarPausa();
    }

    void OnApplicationQuit()
    {
        Terminar(false);
    }

    // Salir de la escena sin haber terminado deja la partida como incompleta
    void OnDestroy()
    {
        Terminar(false);
    }

    void ActualizarPausa()
    {
        if (Registro == null) return;

        if (enSegundoPlano || ControlPausa.EnPausa(Time.timeScale)) Registro.Pausar(DateTime.UtcNow);
        else Registro.Reanudar(DateTime.UtcNow);
    }

    static string Resumen(SesionDeJuego s)
    {
        var texto = new StringBuilder("[Sesion] ");
        texto.Append("completada=").Append(s.Completada ? "true" : "false");
        texto.Append(" nivel=").Append(s.Nivel).Append(" (").Append(s.Dificultad).Append(')');
        texto.Append(" aciertos=").Append(s.Metricas.Aciertos);
        texto.Append(" errores=").Append(s.Metricas.Errores);
        texto.Append(" pistas=").Append(s.Metricas.PistasUsadas);
        foreach (DetalleMetrica d in s.Detalles) texto.Append(' ').Append(d.Clave).Append('=').Append(d.Valor);
        texto.Append(" activo=").Append(s.DuracionActivaSeg).Append('s');
        texto.Append(" pausa=").Append(s.PausadoSeg).Append('s');
        return texto.ToString();
    }
}
