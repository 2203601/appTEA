using System;
using System.Linq;
using NUnit.Framework;

public class RegistroDeSesionTests
{
    static readonly Guid Juego = new Guid("00000000-0000-0000-0000-000000000001");
    static readonly DateTime T0 = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);

    static RegistroDeSesion Nuevo(bool contarAciertos = true) =>
        new RegistroDeSesion(Juego, "facil", 1, T0, contarAciertos);

    [Test]
    public void LaDuracionActivaNoCuentaLaPausa()
    {
        var r = Nuevo();
        r.Pausar(T0.AddSeconds(20));
        r.Reanudar(T0.AddSeconds(50));
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(90), true);

        Assert.AreEqual(60, s.DuracionActivaSeg);
        Assert.AreEqual(30, s.PausadoSeg);
    }

    [Test]
    public void SiTerminaEnPausaLaPausaSeCierraAlFinalizar()
    {
        var r = Nuevo();
        r.Pausar(T0.AddSeconds(10));
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(40), false);

        Assert.AreEqual(10, s.DuracionActivaSeg);
        Assert.AreEqual(30, s.PausadoSeg);
        Assert.IsFalse(s.Completada);
    }

    [Test]
    public void CuentaAciertosErroresYPistas()
    {
        var r = Nuevo();
        r.RegistrarAcierto(); r.RegistrarAcierto(); r.RegistrarError(); r.RegistrarPista();
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(5), true);

        Assert.AreEqual(2, s.Metricas.Aciertos);
        Assert.AreEqual(1, s.Metricas.Errores);
        Assert.AreEqual(1, s.Metricas.PistasUsadas);
    }

    [Test]
    public void SinAciertosSeGuardanComoNull()
    {
        var r = Nuevo(contarAciertos: false);
        r.RegistrarAcierto();
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(5), true);

        Assert.IsNull(s.Metricas.Aciertos);
        Assert.IsNull(s.Metricas.Errores);
    }

    [Test]
    public void SumarDetalleAcumulaYFijarDetalleReemplaza()
    {
        var r = Nuevo();
        r.SumarDetalle("salidas", 1);
        r.SumarDetalle("salidas", 2);
        r.FijarDetalle("precision", 80);
        r.FijarDetalle("precision", 92.5);
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(5), true);

        Assert.AreEqual(3, s.Detalles.Single(d => d.Clave == "salidas").Valor);
        Assert.AreEqual(92.5, s.Detalles.Single(d => d.Clave == "precision").Valor);
        Assert.AreEqual(2, s.Detalles.Count);
    }

    [Test]
    public void FinalizarDosVecesDevuelveLaMismaSesion()
    {
        var r = Nuevo();
        SesionDeJuego a = r.Finalizar(T0.AddSeconds(5), true);
        SesionDeJuego b = r.Finalizar(T0.AddSeconds(99), false);

        Assert.AreSame(a, b);
        Assert.IsTrue(r.Terminada);
    }

    [Test]
    public void DespuesDeFinalizarNoSeRegistraNada()
    {
        var r = Nuevo();
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(5), true);
        r.RegistrarError();
        r.SumarDetalle("x", 1);

        Assert.AreEqual(0, s.Metricas.Errores);
        Assert.AreEqual(0, s.Detalles.Count);
    }

    [Test]
    public void LaSesionGeneradaPasaLaValidacionDelContrato()
    {
        var r = Nuevo();
        r.RegistrarAcierto();
        r.FijarDetalle("confusion:enojo>tristeza", 2);
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(30), true);

        Assert.AreEqual(0, ContratoSubida.Validar(Guid.NewGuid(), s).Count);
    }

    [Test]
    public void UnRelojQueRetrocedeNoDaDuracionesNegativas()
    {
        var r = Nuevo();
        SesionDeJuego s = r.Finalizar(T0.AddSeconds(-10), false);

        Assert.AreEqual(0, s.DuracionActivaSeg);
        Assert.GreaterOrEqual(s.Fin.Value, s.Inicio);
    }
}
