using System.Collections.Generic;
using NUnit.Framework;

public class TableroMemoriaTests
{
    // posiciones: 0 gato, 1 perro, 2 gato, 3 perro
    static TableroMemoria Tablero() => new TableroMemoria(new[] { "gato", "perro", "gato", "perro" });

    [Test]
    public void PrimeraCarta_QuedaSeleccionada()
    {
        var tablero = Tablero();

        Assert.AreEqual(ResultadoSeleccion.PrimeraCarta, tablero.Seleccionar(0));
    }

    [Test]
    [Category("Smoke")]
    public void SegundaCartaIgual_ArmaElPar_YSumaUnEncontrado()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0);

        var resultado = tablero.Seleccionar(2);

        Assert.AreEqual(ResultadoSeleccion.Par, resultado);
        Assert.AreEqual(1, tablero.Encontrados);
        Assert.IsTrue(tablero.EstaEncontrada(0));
        Assert.IsTrue(tablero.EstaEncontrada(2));
    }

    [Test]
    public void SegundaCartaDistinta_EsNoPar_YNoSumaEncontrados()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0);

        var resultado = tablero.Seleccionar(1);

        Assert.AreEqual(ResultadoSeleccion.NoPar, resultado);
        Assert.AreEqual(0, tablero.Encontrados);
        Assert.IsFalse(tablero.EstaEncontrada(0));
    }

    [Test]
    public void TrasUnNoPar_NoSeAceptanMasCartas_HastaTaparlas()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0);
        tablero.Seleccionar(1);

        Assert.AreEqual(ResultadoSeleccion.Ignorada, tablero.Seleccionar(2));

        tablero.TaparSeleccion();
        Assert.AreEqual(ResultadoSeleccion.PrimeraCarta, tablero.Seleccionar(2));
    }

    [Test]
    public void MismaCartaDosVeces_SeIgnora()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0);

        Assert.AreEqual(ResultadoSeleccion.Ignorada, tablero.Seleccionar(0));
        Assert.AreEqual(0, tablero.Encontrados);
    }

    [Test]
    public void CartaYaEncontrada_SeIgnora()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0);
        tablero.Seleccionar(2);

        Assert.AreEqual(ResultadoSeleccion.Ignorada, tablero.Seleccionar(0));
    }

    [TestCase(-1)]
    [TestCase(4)]
    [TestCase(100)]
    public void PosicionFueraDelTablero_SeIgnora(int posicion)
    {
        Assert.AreEqual(ResultadoSeleccion.Ignorada, Tablero().Seleccionar(posicion));
    }

    [Test]
    [Category("Smoke")]
    public void EncontrarTodosLosPares_TerminaElTablero()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0); tablero.Seleccionar(2);
        Assert.IsFalse(tablero.Terminado);

        tablero.Seleccionar(1); tablero.Seleccionar(3);

        Assert.IsTrue(tablero.Terminado);
        Assert.AreEqual(2, tablero.Pares);
    }

    [Test]
    public void TableroVacio_NoSeConsideraTerminado()
    {
        var tablero = new TableroMemoria(new string[0]);

        Assert.AreEqual(0, tablero.Pares);
        Assert.IsFalse(tablero.Terminado);
    }

    [Test]
    public void Pista_SinCartasDadasVuelta_ApuntaALaPrimeraSinEncontrar()
    {
        var tablero = Tablero();

        Assert.AreEqual(0, tablero.PosicionParaPista());

        tablero.Seleccionar(0); tablero.Seleccionar(2);
        Assert.AreEqual(1, tablero.PosicionParaPista());
    }

    [Test]
    [Category("Smoke")]
    public void Pista_ConUnaCartaDadaVuelta_ApuntaASuPar()
    {
        var tablero = Tablero();
        tablero.Seleccionar(1);

        Assert.AreEqual(3, tablero.PosicionParaPista());
    }

    [Test]
    public void Pista_ConDosCartasDadasVuelta_NoHayPista()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0);
        tablero.Seleccionar(1);

        Assert.AreEqual(-1, tablero.PosicionParaPista());

        tablero.TaparSeleccion();
        Assert.AreEqual(0, tablero.PosicionParaPista());
    }

    [Test]
    public void Pista_CuandoTerminoElTablero_NoHayPista()
    {
        var tablero = Tablero();
        tablero.Seleccionar(0); tablero.Seleccionar(2);
        tablero.Seleccionar(1); tablero.Seleccionar(3);

        Assert.AreEqual(-1, tablero.PosicionParaPista());
    }

    [Test]
    public void CantidadPares_UsaLoQuePideLaDificultad_SiHayCartasSuficientes()
    {
        Assert.AreEqual(4, TableroMemoria.CantidadPares(4, 10));
        Assert.AreEqual(6, TableroMemoria.CantidadPares(6, 10));
        Assert.AreEqual(8, TableroMemoria.CantidadPares(8, 8));
    }

    [Test]
    public void CantidadPares_SiFaltanCartas_UsaLasDisponibles()
    {
        Assert.AreEqual(3, TableroMemoria.CantidadPares(8, 3));
        Assert.AreEqual(0, TableroMemoria.CantidadPares(8, 0));
        Assert.AreEqual(0, TableroMemoria.CantidadPares(-2, 5));
    }

    [Test]
    public void ArmarMazo_DuplicaCadaCarta_YRespetaLaCantidad()
    {
        var disponibles = new List<string> { "a", "b", "c", "d", "e", "f" };

        for (int semilla = 0; semilla < 100; semilla++)
        {
            var mazo = TableroMemoria.ArmarMazo(disponibles, 4, new System.Random(semilla));

            Assert.AreEqual(8, mazo.Count);
            var grupos = new Dictionary<string, int>();
            foreach (string carta in mazo) grupos[carta] = grupos.TryGetValue(carta, out int n) ? n + 1 : 1;
            Assert.AreEqual(4, grupos.Count);
            foreach (var par in grupos) Assert.AreEqual(2, par.Value, par.Key);
        }
    }

    [Test]
    public void ArmarMazo_ConPocasCartas_UsaTodasLasDisponibles()
    {
        var mazo = TableroMemoria.ArmarMazo(new List<string> { "a", "b" }, 8, new System.Random(1));

        Assert.AreEqual(4, mazo.Count);
    }

    [Test]
    public void ArmarMazo_SinCartas_DevuelveMazoVacio()
    {
        Assert.AreEqual(0, TableroMemoria.ArmarMazo(new List<string>(), 4, new System.Random(1)).Count);
    }

    [Test]
    public void ArmarMazo_MismaSemilla_MismoMazo()
    {
        var disponibles = new List<string> { "a", "b", "c", "d", "e" };

        var uno = TableroMemoria.ArmarMazo(disponibles, 4, new System.Random(7));
        var dos = TableroMemoria.ArmarMazo(disponibles, 4, new System.Random(7));

        CollectionAssert.AreEqual(uno, dos);
    }
}
