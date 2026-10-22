using System;
using System.Collections.Generic;
using NUnit.Framework;

public class SeguidorTrazoLimitesTests
{
    const float Tolerancia = 10f;

    static List<Punto2> Recta200() => new List<Punto2> { new Punto2(0f, 0f), new Punto2(200f, 0f) };

    static List<Punto2> CaminoEnL() => new List<Punto2> { new Punto2(0f, 0f), new Punto2(100f, 0f), new Punto2(100f, 100f) };

    static void Mover(SeguidorTrazo seguidor, float x, float y)
    {
        seguidor.Actualizar(new Punto2(x, y));
    }

    [Test]
    public void Constructor_ConPuntosNulos_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SeguidorTrazo(null, Tolerancia));
    }

    [Test]
    public void Constructor_ConUnSoloPunto_LanzaArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SeguidorTrazo(new List<Punto2> { new Punto2(0f, 0f) }, Tolerancia));
    }

    [Test]
    public void Constructor_ConDosPuntos_EsElMinimoValido()
    {
        Assert.DoesNotThrow(() => new SeguidorTrazo(Recta200(), Tolerancia));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    public void Constructor_ConToleranciaCeroONegativa_LanzaArgumentException(float tolerancia)
    {
        Assert.Throws<ArgumentException>(() => new SeguidorTrazo(Recta200(), tolerancia));
    }

    [Test]
    public void Constructor_ConToleranciaMuyChicaPeroPositiva_EsValido()
    {
        Assert.DoesNotThrow(() => new SeguidorTrazo(Recta200(), 0.001f));
    }

    [Test]
    public void Constructor_ConTodosLosPuntosIguales_LanzaArgumentException()
    {
        var puntos = new List<Punto2> { new Punto2(5f, 5f), new Punto2(5f, 5f) };

        Assert.Throws<ArgumentException>(() => new SeguidorTrazo(puntos, Tolerancia));
    }

    [Test]
    public void PuedeEmpezar_AExactamenteLaTolerancia_EsValido()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);

        Assert.IsTrue(seguidor.PuedeEmpezar(new Punto2(0f, 10f)));
        Assert.IsFalse(seguidor.PuedeEmpezar(new Punto2(0f, 10.5f)));
    }

    [Test]
    public void Actualizar_InformaLaDistanciaAlCamino()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);

        Mover(seguidor, 20f, 5f);

        Assert.AreEqual(5f, seguidor.Distancia, 0.001f);
        Assert.IsFalse(seguidor.FueraDelCamino);
    }

    [Test]
    public void Actualizar_AExactamenteLaTolerancia_NoEstaFueraDelCamino()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);

        Mover(seguidor, 20f, 10f);

        Assert.IsFalse(seguidor.FueraDelCamino);
    }

    [Test]
    public void Actualizar_ApenasPasadoLaTolerancia_EstaFueraDelCaminoYNoAvanza()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);

        Mover(seguidor, 20f, 11f);

        Assert.IsTrue(seguidor.FueraDelCamino);
        Assert.AreEqual(0f, seguidor.Progreso, 0.0001f);
    }

    [Test]
    public void Progreso_EnLaMitadDeLaRecta_EsUnMedio()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);
        for (float x = 0f; x <= 100f; x += 10f) Mover(seguidor, x, 0f);

        Assert.AreEqual(0.5f, seguidor.Progreso, 0.001f);
    }

    [Test]
    public void Progreso_EnElCaminoEnL_ContabilizaElTramoVertical()
    {
        var seguidor = new SeguidorTrazo(CaminoEnL(), Tolerancia);
        for (float x = 0f; x <= 100f; x += 10f) Mover(seguidor, x, 0f);
        for (float y = 10f; y <= 50f; y += 10f) Mover(seguidor, 100f, y);

        Assert.AreEqual(0.75f, seguidor.Progreso, 0.001f);
    }

    [Test]
    public void Completo_EsFalsoAntesDelNoventaYSieteYVerdaderoDespues()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);
        for (float x = 0f; x <= 180f; x += 10f) Mover(seguidor, x, 0f);          // 90 %
        Assert.IsFalse(seguidor.Completo);

        for (float x = 190f; x <= 200f; x += 5f) Mover(seguidor, x, 0f);         // 100 %
        Assert.IsTrue(seguidor.Completo);
    }

    [Test]
    public void Actualizar_ConUnSaltoMasAllaDeLaVentana_NoAvanza()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);   // ventana = 4 x 10 = 40

        Mover(seguidor, 100f, 0f);

        Assert.IsTrue(seguidor.FueraDelCamino);
        Assert.AreEqual(0f, seguidor.Progreso, 0.0001f);
    }

    [Test]
    public void Actualizar_ConUnSaltoJustoHastaElBordeDeLaVentana_Avanza()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);

        Mover(seguidor, 40f, 0f);

        Assert.IsFalse(seguidor.FueraDelCamino);
        Assert.AreEqual(0.2f, seguidor.Progreso, 0.001f);
    }

    [Test]
    public void Actualizar_PasandoPorUnSegmentoDeLargoCero_SigueAvanzando()
    {
        // Un punto repetido genera un segmento sin largo: tiene que ignorarse sin romper el avance
        var puntos = new List<Punto2> { new Punto2(0f, 0f), new Punto2(0f, 0f), new Punto2(100f, 0f) };
        var seguidor = new SeguidorTrazo(puntos, Tolerancia);

        for (float x = 0f; x <= 50f; x += 10f) Mover(seguidor, x, 0f);

        Assert.AreEqual(0.5f, seguidor.Progreso, 0.001f);
        Assert.IsFalse(float.IsNaN(seguidor.Distancia));
    }

    [Test]
    public void Reiniciar_VuelveAlEstadoInicialYPermiteAvanzarOtraVez()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);
        for (float x = 0f; x <= 100f; x += 10f) Mover(seguidor, x, 0f);
        Mover(seguidor, 100f, 50f);

        seguidor.Reiniciar();

        Assert.AreEqual(0f, seguidor.Progreso, 0.0001f);
        Assert.IsFalse(seguidor.FueraDelCamino);
        Assert.AreEqual(0f, seguidor.Distancia, 0.0001f);
        Mover(seguidor, 10f, 0f);
        Assert.Greater(seguidor.Progreso, 0f);
    }

    [Test]
    public void DireccionActual_AlPrincipioDeUnaRectaHorizontal_ApuntaALaDerecha()
    {
        var seguidor = new SeguidorTrazo(Recta200(), Tolerancia);

        Punto2 direccion = seguidor.DireccionActual;

        Assert.AreEqual(1f, direccion.x, 0.001f);
        Assert.AreEqual(0f, direccion.y, 0.001f);
    }

    [Test]
    public void DireccionActual_AlLlegarAlCodoDelCamino_PasaAApuntarArriba()
    {
        var seguidor = new SeguidorTrazo(CaminoEnL(), Tolerancia);
        for (float x = 0f; x <= 100f; x += 10f) Mover(seguidor, x, 0f);   // justo en el codo

        Punto2 direccion = seguidor.DireccionActual;

        Assert.AreEqual(0f, direccion.x, 0.001f);
        Assert.AreEqual(1f, direccion.y, 0.001f);
    }

    [Test]
    public void DireccionActual_EsUnitaria_EnUnTramoDiagonal()
    {
        var puntos = new List<Punto2> { new Punto2(0f, 0f), new Punto2(30f, 40f) };
        var seguidor = new SeguidorTrazo(puntos, Tolerancia);

        Punto2 d = seguidor.DireccionActual;

        Assert.AreEqual(0.6f, d.x, 0.001f);
        Assert.AreEqual(0.8f, d.y, 0.001f);
    }
}
