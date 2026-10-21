using System.Collections.Generic;
using NUnit.Framework;

public class RondaDeOpcionesTests
{
    static readonly string[] Todas = { "alegre", "triste", "enojado", "asustado", "sorprendido", "cansado" };

    // ---- Responder: fallos y pista -----------------------------------------------------------

    [Test]
    public void PrimerFallo_NoActivaLaPista()
    {
        var ronda = new RondaDeOpciones(2);

        var resultado = ronda.Responder(false);

        Assert.AreEqual(ResultadoRespuesta.Fallo, resultado);
        Assert.AreEqual(1, ronda.Fallos);
    }

    [Test]
    [Category("Smoke")]
    public void SegundoFallo_ActivaLaPista()
    {
        var ronda = new RondaDeOpciones(2);
        ronda.Responder(false);

        var resultado = ronda.Responder(false);

        Assert.AreEqual(ResultadoRespuesta.FalloConPista, resultado);
    }

    [Test]
    public void LuegoDeLaPista_CadaFalloSigueDandoPista()
    {
        var ronda = new RondaDeOpciones(2);
        ronda.Responder(false);
        ronda.Responder(false);

        Assert.AreEqual(ResultadoRespuesta.FalloConPista, ronda.Responder(false));
        Assert.AreEqual(3, ronda.Fallos);
    }

    [Test]
    [Category("Smoke")]
    public void Acierto_ResuelveLaRonda_SinImportarLosFallosPrevios()
    {
        var ronda = new RondaDeOpciones(2);
        ronda.Responder(false);

        var resultado = ronda.Responder(true);

        Assert.AreEqual(ResultadoRespuesta.Acierto, resultado);
        Assert.IsTrue(ronda.Resuelta);
        Assert.AreEqual(1, ronda.Fallos);
    }

    [Test]
    public void AciertoALaPrimera_NoTieneFallos()
    {
        var ronda = new RondaDeOpciones();

        ronda.Responder(true);

        Assert.AreEqual(0, ronda.Fallos);
        Assert.IsTrue(ronda.Resuelta);
    }

    [Test]
    public void FalloDespuesDeResolver_NoSumaFallos()
    {
        var ronda = new RondaDeOpciones(2);
        ronda.Responder(true);

        ronda.Responder(false);

        Assert.AreEqual(0, ronda.Fallos);
    }

    [Test]
    public void ConUnSoloFalloParaLaPista_ElPrimerFalloYaLaActiva()
    {
        var ronda = new RondaDeOpciones(1);

        Assert.AreEqual(ResultadoRespuesta.FalloConPista, ronda.Responder(false));
    }

    [Test]
    public void ConTresFallosParaLaPista_LaActivaRecienEnElTercero()
    {
        var ronda = new RondaDeOpciones(3);

        Assert.AreEqual(ResultadoRespuesta.Fallo, ronda.Responder(false));
        Assert.AreEqual(ResultadoRespuesta.Fallo, ronda.Responder(false));
        Assert.AreEqual(ResultadoRespuesta.FalloConPista, ronda.Responder(false));
    }

    [Test]
    public void ValorPorDefecto_SonDosFallos()
    {
        Assert.AreEqual(2, new RondaDeOpciones().FallosParaPista);
    }

    // ---- ElegirOpciones ----------------------------------------------------------------------

    [Test]
    public void ElegirOpciones_IncluyeSiempreLaCorrecta_YRespetaLaCantidad()
    {
        for (int semilla = 0; semilla < 200; semilla++)
        {
            var opciones = RondaDeOpciones.ElegirOpciones(Todas, "triste", 4, new System.Random(semilla));

            Assert.AreEqual(4, opciones.Count);
            Assert.Contains("triste", opciones);
        }
    }

    [Test]
    public void ElegirOpciones_NoRepiteOpciones()
    {
        for (int semilla = 0; semilla < 200; semilla++)
        {
            var opciones = RondaDeOpciones.ElegirOpciones(Todas, "alegre", 6, new System.Random(semilla));

            Assert.AreEqual(opciones.Count, new HashSet<string>(opciones).Count);
        }
    }

    [Test]
    public void ElegirOpciones_ConMasCantidadQueDisponibles_DevuelveTodas()
    {
        var opciones = RondaDeOpciones.ElegirOpciones(Todas, "cansado", 50, new System.Random(1));

        Assert.AreEqual(Todas.Length, opciones.Count);
    }

    [TestCase(1)]
    [TestCase(0)]
    [TestCase(-3)]
    public void ElegirOpciones_ConCantidadMinimaOMenor_DevuelveSoloLaCorrecta(int cantidad)
    {
        var opciones = RondaDeOpciones.ElegirOpciones(Todas, "enojado", cantidad, new System.Random(1));

        CollectionAssert.AreEqual(new[] { "enojado" }, opciones);
    }

    [Test]
    public void ElegirOpciones_NoModificaLaListaOriginal()
    {
        var original = new List<string>(Todas);

        RondaDeOpciones.ElegirOpciones(original, "triste", 3, new System.Random(5));

        CollectionAssert.AreEqual(Todas, original);
    }

    [Test]
    public void ElegirOpciones_MismaSemilla_MismoResultado()
    {
        var a = RondaDeOpciones.ElegirOpciones(Todas, "triste", 4, new System.Random(42));
        var b = RondaDeOpciones.ElegirOpciones(Todas, "triste", 4, new System.Random(42));

        CollectionAssert.AreEqual(a, b);
    }

    [Test]
    public void ElegirOpciones_LaCorrectaNoSiempreQuedaPrimera()
    {
        bool aparecioEnOtraPosicion = false;
        for (int semilla = 0; semilla < 100 && !aparecioEnOtraPosicion; semilla++)
        {
            var opciones = RondaDeOpciones.ElegirOpciones(Todas, "triste", 4, new System.Random(semilla));
            if (opciones[0] != "triste") aparecioEnOtraPosicion = true;
        }

        Assert.IsTrue(aparecioEnOtraPosicion);
    }
}
