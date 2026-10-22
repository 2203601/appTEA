using System;
using NUnit.Framework;

public class ContratoSubidaMensajesTests
{
    static string Errores(Guid perfil, SesionDeJuego sesion) =>
        string.Join(" | ", ContratoSubida.Validar(perfil, sesion));

    [Test]
    public void Validar_SesionNula_IndicaQueLaSesionEsNula()
    {
        StringAssert.Contains("sesion nula", Errores(SesionesDePrueba.Perfil, null));
    }

    [Test]
    public void Validar_PerfilVacio_SenalaProfileId()
    {
        StringAssert.Contains("profile_id", Errores(Guid.Empty, SesionesDePrueba.Basica()));
    }

    [Test]
    public void Validar_IdsVacios_SenalanSessionIdYGameId()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Id = Guid.Empty;
        sesion.JuegoId = Guid.Empty;

        string errores = Errores(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("session.id", errores);
        StringAssert.Contains("session.game_id", errores);
    }

    [Test]
    public void Validar_FinAnterior_SenalaEndedAt()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Fin = sesion.Inicio.AddSeconds(-1);

        StringAssert.Contains("ended_at", Errores(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_TiemposNegativos_SenalanDurationYPaused()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.DuracionActivaSeg = -1;
        sesion.PausadoSeg = -1;

        string errores = Errores(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("duration_sec", errores);
        StringAssert.Contains("paused_sec", errores);
    }

    [Test]
    public void Validar_DificultadInvalida_SenalaDifficulty()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Dificultad = "extremo";

        StringAssert.Contains("difficulty", Errores(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_MetricasSinId_SenalaMetricsId()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Metricas.Id = Guid.Empty;

        StringAssert.Contains("metrics.id", Errores(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_TiempoDeRespuestaNoNumerico_SenalaAvgResponseMs()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Metricas.TiempoRespuestaMedioMs = double.NaN;

        StringAssert.Contains("avg_response_ms", Errores(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_DetalleSinId_SenalaDetailsId()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.Empty, Clave = "x", Valor = 1 });

        StringAssert.Contains("details.id", Errores(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_ClaveRepetidaYVacia_SenalanMetricKey()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "first_try_pieces", Valor = 1 });
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "", Valor = 1 });

        string errores = Errores(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("repetida", errores);
        StringAssert.Contains("vacía", errores);
    }

    [Test]
    public void Validar_ContadoresEnCero_SonValidos()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Metricas.Aciertos = 0;
        sesion.Metricas.Errores = 0;
        sesion.Metricas.PistasUsadas = 0;

        Assert.IsEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_UnContadorEnMenosUno_EsInvalidoCadaUno()
    {
        foreach (int cual in new[] { 0, 1, 2 })
        {
            SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
            if (cual == 0) sesion.Metricas.Aciertos = -1;
            if (cual == 1) sesion.Metricas.Errores = -1;
            if (cual == 2) sesion.Metricas.PistasUsadas = -1;

            Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion), "contador " + cual);
        }
    }

    [Test]
    public void Payload_SesionNoCompletadaYSinDificultad_EnviaFalseYNull()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Completada = false;
        sesion.Dificultad = null;
        sesion.Nivel = null;

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("\"completed\":false,\"difficulty\":null,\"level\":null", json);
    }

    [Test]
    public void Payload_VariosDetalles_SeparaCadaUnoConComa()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = new Guid("55555555-5555-5555-5555-555555555555"), Clave = "a", Valor = 1 });
        sesion.Detalles.Add(new DetalleMetrica { Id = new Guid("66666666-6666-6666-6666-666666666666"), Clave = "b", Valor = 2.5 });

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("\"metric_value\":1},{\"id\":\"66666666", json);
        StringAssert.EndsWith("\"metric_value\":2.5}]}", json);
    }

    [Test]
    public void Payload_EscapaSaltosDeLineaYTabulaciones()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "a\nb\rc\td", Valor = 1 });

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("\"metric_key\":\"a\\nb\\rc\\td\"", json);
    }

    [Test]
    public void Payload_EscapaCaracteresDeControl_PeroNoElEspacio()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "a b\u0001c", Valor = 1 });

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("\"metric_key\":\"a b\\u0001c\"", json);
    }
}
