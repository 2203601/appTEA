using System.Collections.Generic;
using NUnit.Framework;

public class GeneradorHuecosLimitesTests
{
    const string DiccionarioChico = "MASA\nMISA\nPESA\nTESA\nMOSA\n";

    static GeneradorHuecos Generador(int semilla = 1) => new GeneradorHuecos(DiccionarioChico, new System.Random(semilla));

    [TestCase(null)]
    [TestCase("")]
    public void GenerarHuecosPalabra_SinPalabra_DevuelveNull(string palabra)
    {
        Assert.IsNull(Generador().GenerarHuecosPalabra(palabra, 1, 3));
    }

    [Test]
    public void GenerarHuecosPalabra_ConCeroHuecos_DevuelveNull()
    {
        Assert.IsNull(Generador().GenerarHuecosPalabra("ELEFANTE", 0, 3));
    }

    [Test]
    public void GenerarHuecosPalabra_ConCeroOpciones_DevuelveNull()
    {
        Assert.IsNull(Generador().GenerarHuecosPalabra("ELEFANTE", 1, 0));
    }

    [Test]
    public void GenerarHuecosPalabra_ConUnaSolaOpcion_DevuelveHuecosSinDistractores()
    {
        List<Hueco> huecos = Generador().GenerarHuecosPalabra("ELEFANTE", 1, 1);

        Assert.IsNotNull(huecos);
        Assert.AreEqual(1, huecos.Count);
        Assert.AreEqual("", huecos[0].distractores);
    }

    [Test]
    public void GenerarHuecosPalabra_PidiendoTantosHuecosComoLetrasOcultables_Alcanza()
    {
        // MESA: M, E, S, A son las cuatro ocultables
        List<Hueco> huecos = Generador().GenerarHuecosPalabra("MESA", 4, 2);

        Assert.IsNotNull(huecos);
        Assert.AreEqual(4, huecos.Count);
    }

    [Test]
    public void GenerarHuecosPalabra_PidiendoUnHuecoDeMas_DevuelveNull()
    {
        Assert.IsNull(Generador().GenerarHuecosPalabra("MESA", 5, 2));
    }

    [Test]
    public void GenerarHuecosPalabra_LaPrimeraLetraDeCadaGrupoTambienSeOculta()
    {
        // A es la primera de las vocales y C la primera de las consonantes: ambas tienen que contar
        List<Hueco> huecos = Generador().GenerarHuecosPalabra("AC", 2, 2);

        Assert.IsNotNull(huecos);
        Assert.AreEqual(2, huecos.Count);
    }

    [Test]
    public void GenerarHuecosPalabra_ConExactamenteLosDistractoresDisponibles_Alcanza()
    {
        // Cinco vocales: la correcta más cuatro distractores, sin palabras del diccionario que lo impidan
        List<Hueco> huecos = Generador().GenerarHuecosPalabra("AEIOU", 1, 5);

        Assert.IsNotNull(huecos);
        Assert.AreEqual(4, huecos[0].distractores.Length);
    }

    [Test]
    public void GenerarHuecosPalabra_ConUnDistractorDeMas_DevuelveNull()
    {
        Assert.IsNull(Generador().GenerarHuecosPalabra("AEIOU", 1, 6));
    }

    [Test]
    public void GenerarSecuenciaNumeros_PorTodasLasSemillas_SonCuatroDigitosConsecutivosHastaNueve()
    {
        for (int semilla = 0; semilla < 60; semilla++)
        {
            var (secuencia, _) = Generador(semilla).GenerarSecuenciaNumeros(3);

            Assert.AreEqual(4, secuencia.Length);
            for (int i = 0; i < 4; i++)
            {
                Assert.GreaterOrEqual(secuencia[i], '0');
                Assert.LessOrEqual(secuencia[i], '9');
                if (i > 0) Assert.AreEqual(secuencia[i - 1] + 1, secuencia[i]);
            }
        }
    }

    [Test]
    public void GenerarSecuenciaNumeros_ConDiezOpciones_IncluyeElNueveEntreLosDistractores()
    {
        var (_, hueco) = Generador().GenerarSecuenciaNumeros(10, "1234");

        Assert.AreEqual(9, hueco.distractores.Length);
        StringAssert.Contains("9", hueco.distractores);
        StringAssert.Contains("0", hueco.distractores);
    }
}
