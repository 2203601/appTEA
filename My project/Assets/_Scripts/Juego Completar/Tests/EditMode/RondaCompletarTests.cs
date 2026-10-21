using System.Collections.Generic;
using NUnit.Framework;

public class RondaCompletarTests
{
    static List<Hueco> DosHuecos() => new List<Hueco>
    {
        new Hueco { indice = 1, correcta = 'A', distractores = "EO" },
        new Hueco { indice = 3, correcta = 'O', distractores = "IU" },
    };

    static RondaCompletar Ronda() => new RondaCompletar("gato", DosHuecos(), new System.Random(1));

    [Test]
    public void ElTextoSeGuardaEnMayusculas()
    {
        Assert.AreEqual("GATO", Ronda().Texto);
    }

    [Test]
    public void ArrancaEnElPrimerHueco()
    {
        var ronda = Ronda();

        Assert.IsFalse(ronda.Terminada);
        Assert.AreEqual(1, ronda.IndiceHuecoActual);
        Assert.AreEqual('A', ronda.CaracterEsperado);
    }

    [Test]
    [Category("Smoke")]
    public void Avanzar_PasaAlSiguienteHueco_YAlFinalTermina()
    {
        var ronda = Ronda();

        ronda.Avanzar();
        Assert.AreEqual(3, ronda.IndiceHuecoActual);
        Assert.IsFalse(ronda.Terminada);

        ronda.Avanzar();
        Assert.IsTrue(ronda.Terminada);
    }

    [TestCase(1, true)]
    [TestCase(3, true)]
    [TestCase(0, false)]
    [TestCase(2, false)]
    [TestCase(-1, false)]
    [TestCase(99, false)]
    public void EsHueco_SoloParaLasPosicionesOcultas(int indice, bool esperado)
    {
        Assert.AreEqual(esperado, Ronda().EsHueco(indice));
    }

    [Test]
    public void Opciones_IncluyenLaCorrectaYLosDistractores_SinRepetir()
    {
        var opciones = Ronda().OpcionesDelHuecoActual();

        CollectionAssert.AreEquivalent(new[] { 'A', 'E', 'O' }, opciones);
    }

    [Test]
    public void Opciones_CambianAlAvanzarDeHueco()
    {
        var ronda = Ronda();
        ronda.Avanzar();

        CollectionAssert.AreEquivalent(new[] { 'O', 'I', 'U' }, ronda.OpcionesDelHuecoActual());
    }

    [Test]
    public void Opciones_LaCorrectaNoSiempreQuedaPrimera()
    {
        bool otraPosicion = false;
        for (int semilla = 0; semilla < 100 && !otraPosicion; semilla++)
        {
            var ronda = new RondaCompletar("gato", DosHuecos(), new System.Random(semilla));
            if (ronda.OpcionesDelHuecoActual()[0] != 'A') otraPosicion = true;
        }

        Assert.IsTrue(otraPosicion);
    }
}
