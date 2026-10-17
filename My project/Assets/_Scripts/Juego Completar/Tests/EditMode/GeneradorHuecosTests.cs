using System.Collections.Generic;
using NUnit.Framework;

public class GeneradorHuecosTests
{
    const string Diccionario =
        "# palabras de prueba\n" +
        "\n" +
        "MASA\nMISA\nPESA\nTESA\nMOSA\n" +
        "casa\ncosa\ngato\npato\nPÍO\n";

    static readonly string[] PalabrasDePrueba =
        { "MESA", "CASA", "GATO", "ELEFANTE", "ZAPATO", "JIRAFA", "HUEVO", "VENTANA", "MARIPOSA" };

    GeneradorHuecos CrearGenerador(int semilla)
    {
        return new GeneradorHuecos(Diccionario, new System.Random(semilla));
    }

    [Test]
    public void Diccionario_IgnoraComentariosYLineasVacias_YNormaliza()
    {
        var generador = CrearGenerador(1);

        Assert.IsTrue(generador.EsPalabra("MASA"));
        Assert.IsTrue(generador.EsPalabra("casa"));
        Assert.IsTrue(generador.EsPalabra("PIO"));
        Assert.IsFalse(generador.EsPalabra("# palabras de prueba"));
        Assert.IsFalse(generador.EsPalabra(""));
    }

    [Test]
    public void Normalizar_QuitaTildes_YMantieneLaEnie()
    {
        Assert.AreEqual("ARBOL", GeneradorHuecos.Normalizar("árbol"));
        Assert.AreEqual("PINGUINO", GeneradorHuecos.Normalizar("pingüino"));
        Assert.AreEqual("NIÑO", GeneradorHuecos.Normalizar("niño"));
        Assert.AreEqual("CAFE", GeneradorHuecos.Normalizar("Café"));
    }

    [Test]
    public void NuncaOcultaLetrasNoOcultables()
    {
        for (int semilla = 0; semilla < 100; semilla++)
        {
            var generador = CrearGenerador(semilla);
            foreach (string palabra in PalabrasDePrueba)
            {
                var huecos = generador.GenerarHuecosPalabra(palabra, 2, 3);
                Assert.IsNotNull(huecos, palabra);

                foreach (var hueco in huecos)
                {
                    Assert.AreEqual(-1, GeneradorHuecos.NO_OCULTABLES.IndexOf(hueco.correcta),
                        $"{palabra}: se ocultó '{hueco.correcta}'");
                    Assert.AreEqual(palabra[hueco.indice], hueco.correcta);
                }
            }
        }
    }

    [Test]
    public void NingunDistractorFormaUnaPalabraDelDiccionario()
    {
        for (int semilla = 0; semilla < 100; semilla++)
        {
            var generador = CrearGenerador(semilla);
            foreach (string palabra in new[] { "MESA", "CASA", "GATO" })
            {
                var huecos = generador.GenerarHuecosPalabra(palabra, 1, 3);
                Assert.IsNotNull(huecos, palabra);

                foreach (var hueco in huecos)
                {
                    foreach (char distractor in hueco.distractores)
                    {
                        char[] letras = palabra.ToCharArray();
                        letras[hueco.indice] = distractor;
                        string reemplazada = new string(letras);
                        Assert.IsFalse(generador.EsPalabra(reemplazada),
                            $"{palabra}: el distractor '{distractor}' forma {reemplazada}");
                    }
                }
            }
        }
    }

    [Test]
    public void LosHuecosQuedanOrdenadosDeIzquierdaADerecha()
    {
        for (int semilla = 0; semilla < 100; semilla++)
        {
            var huecos = CrearGenerador(semilla).GenerarHuecosPalabra("ELEFANTE", 3, 3);
            Assert.IsNotNull(huecos);
            Assert.AreEqual(3, huecos.Count);

            for (int i = 1; i < huecos.Count; i++)
                Assert.Greater(huecos[i].indice, huecos[i - 1].indice);
        }
    }

    [Test]
    public void NoHayDistractoresRepetidos_NiIgualesALaCorrecta_YSonDelMismoTipo()
    {
        for (int semilla = 0; semilla < 100; semilla++)
        {
            var generador = CrearGenerador(semilla);
            foreach (string palabra in PalabrasDePrueba)
            {
                var huecos = generador.GenerarHuecosPalabra(palabra, 2, 3);
                foreach (var hueco in huecos)
                {
                    Assert.AreEqual(2, hueco.distractores.Length);

                    var vistos = new HashSet<char>();
                    foreach (char distractor in hueco.distractores)
                    {
                        Assert.IsTrue(vistos.Add(distractor), $"{palabra}: '{distractor}' repetido");
                        Assert.AreNotEqual(hueco.correcta, distractor);

                        bool correctaEsVocal = GeneradorHuecos.VOCALES.IndexOf(hueco.correcta) >= 0;
                        bool distractorEsVocal = GeneradorHuecos.VOCALES.IndexOf(distractor) >= 0;
                        Assert.AreEqual(correctaEsVocal, distractorEsVocal);
                    }
                }
            }
        }
    }

    [Test]
    public void DevuelveNull_SiNoHayLetrasOcultablesSuficientes()
    {
        var generador = CrearGenerador(1);

        Assert.IsNull(generador.GenerarHuecosPalabra("BVZ", 1, 3));
        Assert.IsNull(generador.GenerarHuecosPalabra("PATO", 5, 3));
    }

    [Test]
    public void DevuelveNull_SiNoSePuedenArmarLosDistractores()
    {
        // Con 3 huecos en "AAA" siempre se oculta la primera A, y todos sus reemplazos de vocal son palabras.
        var generador = new GeneradorHuecos("EAA\nIAA\nOAA\nUAA", new System.Random(1));

        Assert.IsNull(generador.GenerarHuecosPalabra("AAA", 3, 3));
    }

    [Test]
    public void SecuenciaNumeros_EsAscendenteDeCuatroDigitosSinPasarDeNueve()
    {
        for (int semilla = 0; semilla < 200; semilla++)
        {
            var (secuencia, hueco) = CrearGenerador(semilla).GenerarSecuenciaNumeros(3);

            Assert.AreEqual(4, secuencia.Length);
            for (int i = 1; i < 4; i++)
                Assert.AreEqual((int)secuencia[i - 1] + 1, (int)secuencia[i]);
            Assert.LessOrEqual(secuencia[3], '9');

            Assert.That(hueco.indice, Is.InRange(0, 3));
            Assert.AreEqual(secuencia[hueco.indice], hueco.correcta);
        }
    }

    [Test]
    public void SecuenciaNumeros_DistractoresDistintosYSinRepetir()
    {
        for (int semilla = 0; semilla < 200; semilla++)
        {
            var (_, hueco) = CrearGenerador(semilla).GenerarSecuenciaNumeros(4);

            Assert.AreEqual(3, hueco.distractores.Length);
            var vistos = new HashSet<char>();
            foreach (char distractor in hueco.distractores)
            {
                Assert.IsTrue(char.IsDigit(distractor));
                Assert.AreNotEqual(hueco.correcta, distractor);
                Assert.IsTrue(vistos.Add(distractor));
            }
        }
    }
}
