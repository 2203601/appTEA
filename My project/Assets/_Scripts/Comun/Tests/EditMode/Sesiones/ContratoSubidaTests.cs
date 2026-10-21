using System;
using System.Globalization;
using NUnit.Framework;

public class ContratoSubidaTests
{
    // ---------- Payload ----------

    [Test]
    [Category("Smoke")]
    public void Payload_SesionCompleta_CoincideConElContrato()
    {
        // Arrange
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();

        // Act
        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        // Assert
        const string esperado =
            "{\"profile_id\":\"11111111-1111-1111-1111-111111111111\"," +
            "\"session\":{\"id\":\"22222222-2222-2222-2222-222222222222\"," +
            "\"game_id\":\"00000000-0000-0000-0000-000000000001\"," +
            "\"started_at\":\"2026-10-20T12:00:00Z\",\"ended_at\":\"2026-10-20T12:02:10Z\"," +
            "\"duration_sec\":120,\"paused_sec\":10,\"completed\":true,\"difficulty\":\"facil\",\"level\":1}," +
            "\"metrics\":{\"id\":\"33333333-3333-3333-3333-333333333333\",\"correct_answers\":5,\"wrong_answers\":2," +
            "\"avg_response_ms\":1234.5,\"hints_used\":1,\"recorded_at\":\"2026-10-20T12:02:10Z\"}," +
            "\"details\":[{\"id\":\"44444444-4444-4444-4444-444444444444\",\"metric_key\":\"first_try_pieces\",\"metric_value\":2}]}";
        Assert.AreEqual(esperado, json);
    }

    [Test]
    public void Payload_SinMetricasNiDetalles_OmiteMetricsYDetails()
    {
        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, SesionesDePrueba.Basica());

        Assert.IsFalse(json.Contains("\"metrics\""));
        Assert.IsFalse(json.Contains("\"details\""));
    }

    [Test]
    public void Payload_SesionSinTerminar_EnviaEndedAtNull()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Fin = null;

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        Assert.IsTrue(json.Contains("\"ended_at\":null"));
    }

    [Test]
    public void Payload_JuegoSinAciertos_EnviaNullEnLugarDeCero()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Metricas.Aciertos = null;
        sesion.Metricas.Errores = null;
        sesion.Metricas.TiempoRespuestaMedioMs = null;

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("\"correct_answers\":null,\"wrong_answers\":null,\"avg_response_ms\":null", json);
    }

    [Test]
    public void Payload_UsaPuntoDecimal_AunqueLaCulturaDelDispositivoUseComa()
    {
        // Arrange: cultura con coma decimal (como es-AR en la tablet)
        CultureInfo original = CultureInfo.CurrentCulture;
        var coma = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        coma.NumberFormat.NumberDecimalSeparator = ",";
        CultureInfo.CurrentCulture = coma;
        try
        {
            // Act
            string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, SesionesDePrueba.ConMetricas());

            // Assert
            StringAssert.Contains("\"avg_response_ms\":1234.5", json);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public void Payload_EscapaComillasYBarras_EnLasClaves()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "pieza:\"Gato\"\\1", Valor = 1 });

        string json = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        StringAssert.Contains("\"metric_key\":\"pieza:\\\"Gato\\\"\\\\1\"", json);
    }

    [Test]
    public void Payload_ElMismoObjeto_GeneraSiempreElMismoJson()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();

        string primero = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);
        string segundo = ContratoSubida.ConstruirPayload(SesionesDePrueba.Perfil, sesion);

        Assert.AreEqual(primero, segundo);
    }

    // ---------- Validación (clases de equivalencia y valores límite) ----------

    [Test]
    public void Validar_SesionCorrecta_NoTieneErrores()
    {
        Assert.IsEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, SesionesDePrueba.ConMetricas()));
    }

    [Test]
    public void Validar_ValoresLimiteAceptados_DuracionCeroFinIgualAInicioYDificultadNula()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.DuracionActivaSeg = 0;
        sesion.PausadoSeg = 0;
        sesion.Fin = sesion.Inicio;
        sesion.Dificultad = null;

        Assert.IsEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_SesionNula_EsInvalida()
    {
        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, null));
    }

    [Test]
    public void Validar_PerfilVacio_EsInvalida()
    {
        Assert.IsNotEmpty(ContratoSubida.Validar(Guid.Empty, SesionesDePrueba.Basica()));
    }

    [TestCase(-1, 0)]
    [TestCase(0, -1)]
    public void Validar_TiemposNegativos_SonInvalidos(int duracion, int pausa)
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.DuracionActivaSeg = duracion;
        sesion.PausadoSeg = pausa;

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_FinAnteriorAlInicio_EsInvalida()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Fin = sesion.Inicio.AddSeconds(-1);

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [TestCase("extremo")]
    [TestCase("")]
    [TestCase("Facil")]
    public void Validar_DificultadFueraDeLaLista_EsInvalida(string dificultad)
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Dificultad = dificultad;

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [TestCase("facil")]
    [TestCase("medio")]
    [TestCase("dificil")]
    public void Validar_LasTresDificultades_SonValidas(string dificultad)
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Dificultad = dificultad;

        Assert.IsEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_ClaveRepetida_EsInvalida()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "first_try_pieces", Valor = 3 });

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_ClaveVacia_EsInvalida()
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "", Valor = 1 });

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void Validar_ValorQueNoEsUnNumero_EsInvalida(double valor)
    {
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.Detalles.Add(new DetalleMetrica { Id = Guid.NewGuid(), Clave = "x", Valor = valor });

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }

    [Test]
    public void Validar_ContadoresNegativos_SonInvalidos()
    {
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();
        sesion.Metricas.Errores = -1;

        Assert.IsNotEmpty(ContratoSubida.Validar(SesionesDePrueba.Perfil, sesion));
    }
}
