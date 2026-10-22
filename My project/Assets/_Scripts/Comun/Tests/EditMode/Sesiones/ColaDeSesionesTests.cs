using NUnit.Framework;

public class ColaDeSesionesTests
{
    const string Token = "token-de-prueba";

    static ColaDeSesiones ColaCon(int cantidad)
    {
        var cola = new ColaDeSesiones();
        for (int i = 1; i <= cantidad; i++) cola.Encolar(SesionesDePrueba.Perfil, SesionesDePrueba.Numero(i));
        return cola;
    }

    [Test]
    public void Encolar_LaMismaSesionDosVeces_QuedaUnaSola()
    {
        var cola = new ColaDeSesiones();
        SesionDeJuego sesion = SesionesDePrueba.Basica();

        bool primera = cola.Encolar(SesionesDePrueba.Perfil, sesion);
        bool segunda = cola.Encolar(SesionesDePrueba.Perfil, sesion);

        Assert.IsTrue(primera);
        Assert.IsFalse(segunda);
        Assert.AreEqual(1, cola.Pendientes);
    }

    [Test]
    public void Encolar_SesionInvalida_NoSeAgrega()
    {
        var cola = new ColaDeSesiones();
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.PausadoSeg = -1;

        Assert.IsFalse(cola.Encolar(SesionesDePrueba.Perfil, sesion));
        Assert.AreEqual(0, cola.Pendientes);
    }

    [Test]
    public void Vaciar_ConTodoOk_SubeEnElOrdenEncoladoYQuedaVacia()
    {
        var transporte = new TransporteFalso().LuegoSiempre(200);
        ColaDeSesiones cola = ColaCon(3);

        ResumenVaciado resumen = cola.Vaciar(new ClienteSubida(transporte), Token);

        Assert.AreEqual(3, resumen.Subidas);
        Assert.AreEqual(0, cola.Pendientes);
        Assert.IsFalse(resumen.Detenida);
        StringAssert.Contains("000000000001", transporte.Cuerpos[0]);
        StringAssert.Contains("000000000003", transporte.Cuerpos[2]);
    }

    [Test]
    [Category("Smoke")]
    public void Vaciar_ConFallaDeRed_SeDetieneYConservaLasPendientes()
    {
        // Arrange
        var transporte = new TransporteFalso().Responde(200).SinRed();
        ColaDeSesiones cola = ColaCon(3);

        // Act
        ResumenVaciado resumen = cola.Vaciar(new ClienteSubida(transporte), Token);

        // Assert
        Assert.AreEqual(1, resumen.Subidas);
        Assert.AreEqual(2, resumen.Pendientes);
        Assert.IsTrue(resumen.Detenida);
        Assert.AreEqual(ResultadoSubida.Reintentar, resumen.Motivo);
        Assert.AreEqual(2, transporte.Cuerpos.Count);
    }

    [Test]
    public void Vaciar_CuandoVuelveLaRed_SubeLasPendientesSinDuplicar()
    {
        var transporte = new TransporteFalso().SinRed().LuegoSiempre(200);
        ColaDeSesiones cola = ColaCon(2);
        var cliente = new ClienteSubida(transporte);

        cola.Vaciar(cliente, Token);
        ResumenVaciado segundo = cola.Vaciar(cliente, Token);

        Assert.AreEqual(2, segundo.Subidas);
        Assert.AreEqual(0, cola.Pendientes);
        // 1 intento fallido de la primera + 2 subidas: la primera se mandó dos veces con el mismo cuerpo
        Assert.AreEqual(3, transporte.Cuerpos.Count);
        Assert.AreEqual(transporte.Cuerpos[0], transporte.Cuerpos[1]);
    }

    [Test]
    public void Vaciar_UnaRechazadaSeAparta_YLasDemasSiguen()
    {
        var transporte = new TransporteFalso().Responde(200).Responde(422).Responde(200);
        ColaDeSesiones cola = ColaCon(3);

        ResumenVaciado resumen = cola.Vaciar(new ClienteSubida(transporte), Token);

        Assert.AreEqual(2, resumen.Subidas);
        Assert.AreEqual(1, resumen.Rechazadas);
        Assert.AreEqual(0, cola.Pendientes);
        Assert.AreEqual(1, cola.Rechazadas.Count);
        Assert.IsFalse(resumen.Detenida);
    }

    [Test]
    public void Vaciar_TokenVencido_SeDetieneSinPerderNada()
    {
        var transporte = new TransporteFalso().Responde(401);
        ColaDeSesiones cola = ColaCon(2);

        ResumenVaciado resumen = cola.Vaciar(new ClienteSubida(transporte), Token);

        Assert.AreEqual(ResultadoSubida.NoAutorizado, resumen.Motivo);
        Assert.AreEqual(2, cola.Pendientes);
        Assert.AreEqual(0, cola.Rechazadas.Count);
    }

    [Test]
    public void Vaciar_ColaVacia_NoEnviaNada()
    {
        var transporte = new TransporteFalso();

        ResumenVaciado resumen = new ColaDeSesiones().Vaciar(new ClienteSubida(transporte), Token);

        Assert.AreEqual(0, resumen.Subidas);
        Assert.AreEqual(0, transporte.Cuerpos.Count);
    }
}
