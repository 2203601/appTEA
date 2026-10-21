using System.Collections.Generic;
using NUnit.Framework;

public class SeguidorTrazoTests
{
    const float Tolerancia = 10f;

    // Recta horizontal de 0 a 200, un punto cada 10
    static List<Punto2> Recta()
    {
        var puntos = new List<Punto2>();
        for (int i = 0; i <= 20; i++) puntos.Add(new Punto2(i * 10f, 0f));
        return puntos;
    }

    // Círculo de radio 100 que arranca arriba y gira en sentido antihorario; el final queda sobre el inicio
    static List<Punto2> Circulo()
    {
        var puntos = new List<Punto2>();
        for (int i = 0; i <= 72; i++)
        {
            double angulo = System.Math.PI / 2 + i / 72.0 * 2 * System.Math.PI;
            puntos.Add(new Punto2((float)(100 * System.Math.Cos(angulo)), (float)(100 * System.Math.Sin(angulo))));
        }
        return puntos;
    }

    // Recorre el camino con el dedo, un paso fino por vez
    static void Recorrer(SeguidorTrazo seguidor, List<Punto2> camino)
    {
        seguidor.Actualizar(camino[0]);
        for (int i = 1; i < camino.Count; i++)
        {
            Punto2 a = camino[i - 1], b = camino[i];
            for (int k = 1; k <= 4; k++)
                seguidor.Actualizar(new Punto2(a.x + (b.x - a.x) * k / 4f, a.y + (b.y - a.y) * k / 4f));
        }
    }

    [Test]
    public void PuedeEmpezar_SoloCercaDelInicio()
    {
        var seguidor = new SeguidorTrazo(Recta(), Tolerancia);

        Assert.IsTrue(seguidor.PuedeEmpezar(new Punto2(0f, 0f)));
        Assert.IsTrue(seguidor.PuedeEmpezar(new Punto2(5f, 5f)));
        Assert.IsFalse(seguidor.PuedeEmpezar(new Punto2(100f, 0f)));
        Assert.IsFalse(seguidor.PuedeEmpezar(new Punto2(0f, 50f)));
    }

    [Test]
    public void AvanzaSiguiendoElCamino()
    {
        var seguidor = new SeguidorTrazo(Recta(), Tolerancia);
        float anterior = 0f;

        for (float x = 0f; x <= 100f; x += 5f)
        {
            seguidor.Actualizar(new Punto2(x, 3f));   // un poco al costado, dentro de la tolerancia

            Assert.IsFalse(seguidor.FueraDelCamino);
            Assert.GreaterOrEqual(seguidor.Progreso, anterior);
            anterior = seguidor.Progreso;
        }

        Assert.AreEqual(0.5f, seguidor.Progreso, 0.02f);
        Assert.IsFalse(seguidor.Completo);
    }

    [Test]
    public void NoAvanzaSiSeAleja()
    {
        var seguidor = new SeguidorTrazo(Recta(), Tolerancia);
        for (float x = 0f; x <= 60f; x += 5f) seguidor.Actualizar(new Punto2(x, 0f));
        float progreso = seguidor.Progreso;

        seguidor.Actualizar(new Punto2(65f, 40f));

        Assert.IsTrue(seguidor.FueraDelCamino);
        Assert.AreEqual(progreso, seguidor.Progreso);
    }

    [Test]
    public void NoRetrocede_AlVolverPorDondeYaPaso()
    {
        var seguidor = new SeguidorTrazo(Recta(), Tolerancia);
        for (float x = 0f; x <= 100f; x += 5f) seguidor.Actualizar(new Punto2(x, 0f));
        float progreso = seguidor.Progreso;

        seguidor.Actualizar(new Punto2(30f, 0f));

        Assert.AreEqual(progreso, seguidor.Progreso);
    }

    [Test]
    public void NoSePuedeSaltarAlFinal_EnUnaRecta()
    {
        var seguidor = new SeguidorTrazo(Recta(), Tolerancia);
        seguidor.Actualizar(new Punto2(0f, 0f));

        seguidor.Actualizar(new Punto2(200f, 0f));

        Assert.IsTrue(seguidor.FueraDelCamino);
        Assert.AreEqual(0f, seguidor.Progreso, 0.001f);
        Assert.IsFalse(seguidor.Completo);
    }

    [Test]
    public void NoSePuedeSaltarAlFinal_EnUnCirculo_AunqueElFinalEsteSobreElInicio()
    {
        var circulo = Circulo();
        var seguidor = new SeguidorTrazo(circulo, Tolerancia);
        seguidor.Actualizar(circulo[0]);

        // Tocar el punto final (que es el mismo lugar que el inicio) no puede completar el trazo
        seguidor.Actualizar(circulo[circulo.Count - 1]);
        Assert.IsFalse(seguidor.Completo);
        Assert.Less(seguidor.Progreso, 0.1f);

        // Ni tampoco cortar por el medio
        seguidor.Actualizar(new Punto2(0f, -100f));
        Assert.IsTrue(seguidor.FueraDelCamino);
        Assert.Less(seguidor.Progreso, 0.1f);
    }

    [Test]
    [Category("Smoke")]
    public void UnaRectaSeCompleta()
    {
        var recta = Recta();
        var seguidor = new SeguidorTrazo(recta, Tolerancia);

        Recorrer(seguidor, recta);

        Assert.IsTrue(seguidor.Completo);
        Assert.GreaterOrEqual(seguidor.Progreso, SeguidorTrazo.ProgresoCompleto);
    }

    [Test]
    public void UnCirculoSeCompleta()
    {
        var circulo = Circulo();
        var seguidor = new SeguidorTrazo(circulo, Tolerancia);

        Recorrer(seguidor, circulo);

        Assert.IsTrue(seguidor.Completo);
    }

    [Test]
    public void DireccionActual_ApuntaHaciaDondeSigueElTrazo()
    {
        var seguidor = new SeguidorTrazo(Recta(), Tolerancia);
        seguidor.Actualizar(new Punto2(50f, 0f));

        Assert.AreEqual(1f, seguidor.DireccionActual.x, 0.001f);
        Assert.AreEqual(0f, seguidor.DireccionActual.y, 0.001f);
    }

    [Test]
    public void SinPuntosSuficientes_Falla()
    {
        Assert.Throws<System.ArgumentException>(() => new SeguidorTrazo(new List<Punto2> { new Punto2(0f, 0f) }, Tolerancia));
    }
}
