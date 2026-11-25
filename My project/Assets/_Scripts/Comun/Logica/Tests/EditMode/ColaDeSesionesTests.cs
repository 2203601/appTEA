using System;
using System.Collections.Generic;
using NUnit.Framework;

public class ColaDeSesionesTests
{
    class AlmacenEnMemoria : IAlmacenDeCola
    {
        public List<string> Lineas = new List<string>();
        public List<string> Cargar() => new List<string>(Lineas);
        public void Guardar(IList<string> lineas) => Lineas = new List<string>(lineas);
    }

    class TransporteFalso : ITransporte
    {
        public int Codigo = 200;
        public bool SinRed;
        public List<string> Enviados = new List<string>();
        public RespuestaHttp Enviar(string ruta, string json, string token)
        {
            if (SinRed) throw new ErrorDeRedException("sin red");
            Enviados.Add(json);
            return new RespuestaHttp(Codigo);
        }
    }

    static readonly DateTime T0 = new DateTime(2026, 11, 25, 10, 0, 0, DateTimeKind.Utc);

    static SesionDeJuego Sesion()
    {
        var r = new RegistroDeSesion(Guid.NewGuid(), "facil", 1, T0);
        r.RegistrarAcierto();
        return r.Finalizar(T0.AddSeconds(30), true);
    }

    [Test]
    public void LaColaSobreviveAUnReinicio()
    {
        var almacen = new AlmacenEnMemoria();
        var cola = new ColaDeSesiones(almacen);
        Assert.IsTrue(cola.Encolar(Guid.NewGuid(), Sesion()));
        Assert.IsTrue(cola.Encolar(Guid.NewGuid(), Sesion()));

        var reabierta = new ColaDeSesiones(almacen);   // "se cerró y abrió la app"
        Assert.AreEqual(2, reabierta.Pendientes);
    }

    [Test]
    public void ReenviaExactamenteElMismoJson()
    {
        var almacen = new AlmacenEnMemoria();
        var perfil = Guid.NewGuid();
        var sesion = Sesion();
        new ColaDeSesiones(almacen).Encolar(perfil, sesion);

        var transporte = new TransporteFalso();
        var reabierta = new ColaDeSesiones(almacen);
        reabierta.Vaciar(new ClienteSubida(transporte), "token");

        Assert.AreEqual(1, transporte.Enviados.Count);
        Assert.AreEqual(ContratoSubida.ConstruirPayload(perfil, sesion), transporte.Enviados[0]);
    }

    [Test]
    public void SinRedLaSesionSeConservaEnDisco()
    {
        var almacen = new AlmacenEnMemoria();
        var cola = new ColaDeSesiones(almacen);
        cola.Encolar(Guid.NewGuid(), Sesion());

        ResumenVaciado r = cola.Vaciar(new ClienteSubida(new TransporteFalso { SinRed = true }), "token");

        Assert.IsTrue(r.Detenida);
        Assert.AreEqual(1, r.Pendientes);
        Assert.AreEqual(1, almacen.Lineas.Count);
    }

    [Test]
    public void LaSesionSubidaSeSacaDelDisco()
    {
        var almacen = new AlmacenEnMemoria();
        var cola = new ColaDeSesiones(almacen);
        cola.Encolar(Guid.NewGuid(), Sesion());

        ResumenVaciado r = cola.Vaciar(new ClienteSubida(new TransporteFalso()), "token");

        Assert.AreEqual(1, r.Subidas);
        Assert.AreEqual(0, almacen.Lineas.Count);
    }

    [Test]
    public void UnaSesionRechazadaSeApartaYNoBloqueaLasDemas()
    {
        var cola = new ColaDeSesiones(new AlmacenEnMemoria());
        cola.Encolar(Guid.NewGuid(), Sesion());
        ResumenVaciado r = cola.Vaciar(new ClienteSubida(new TransporteFalso { Codigo = 400 }), "token");

        Assert.AreEqual(1, r.Rechazadas);
        Assert.AreEqual(0, r.Pendientes);
        Assert.AreEqual(1, cola.Rechazadas.Count);
    }

    [Test]
    public void NoEncolaDosVecesLaMismaSesion()
    {
        var cola = new ColaDeSesiones(new AlmacenEnMemoria());
        var perfil = Guid.NewGuid();
        var s = Sesion();

        Assert.IsTrue(cola.Encolar(perfil, s));
        Assert.IsFalse(cola.Encolar(perfil, s));
        Assert.AreEqual(1, cola.Pendientes);
    }

    [Test]
    public void LasLineasDanadasSeDescartan()
    {
        var almacen = new AlmacenEnMemoria();
        almacen.Lineas.Add("basura sin tab");
        almacen.Lineas.Add("no-es-guid\t{}");
        Assert.AreEqual(0, new ColaDeSesiones(almacen).Pendientes);
    }

    [Test]
    public void SinTokenNoSeEnviaNadaYSeConserva()
    {
        var transporte = new TransporteFalso();
        var cola = new ColaDeSesiones(new AlmacenEnMemoria());
        cola.Encolar(Guid.NewGuid(), Sesion());

        ResumenVaciado r = cola.Vaciar(new ClienteSubida(transporte), null);

        Assert.AreEqual(ResultadoSubida.NoAutorizado, r.Motivo);
        Assert.AreEqual(0, transporte.Enviados.Count);
        Assert.AreEqual(1, r.Pendientes);
    }
}
