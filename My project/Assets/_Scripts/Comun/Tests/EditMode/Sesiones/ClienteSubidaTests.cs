using NUnit.Framework;

public class ClienteSubidaTests
{
    const string Token = "token-de-prueba";

    [Test]
    public void Subir_RespuestaExitosa_EnviaUnPostALaRutaDelContratoConElToken()
    {
        // Arrange
        var transporte = new TransporteFalso().Responde(200);
        var cliente = new ClienteSubida(transporte);

        // Act
        ResultadoSubida resultado = cliente.Subir(SesionesDePrueba.Perfil, SesionesDePrueba.ConMetricas(), Token);

        // Assert
        Assert.AreEqual(ResultadoSubida.Subida, resultado);
        Assert.AreEqual(1, transporte.Cuerpos.Count);
        Assert.AreEqual(ContratoSubida.Ruta, transporte.Rutas[0]);
        Assert.AreEqual(Token, transporte.Tokens[0]);
    }

    [TestCase(200, ResultadoSubida.Subida)]
    [TestCase(201, ResultadoSubida.Subida)]
    [TestCase(204, ResultadoSubida.Subida)]
    [TestCase(401, ResultadoSubida.NoAutorizado)]
    [TestCase(400, ResultadoSubida.Rechazada)]
    [TestCase(403, ResultadoSubida.Rechazada)]
    [TestCase(404, ResultadoSubida.Rechazada)]
    [TestCase(409, ResultadoSubida.Rechazada)]
    [TestCase(422, ResultadoSubida.Rechazada)]
    [TestCase(408, ResultadoSubida.Reintentar)]
    [TestCase(429, ResultadoSubida.Reintentar)]
    [TestCase(500, ResultadoSubida.Reintentar)]
    [TestCase(503, ResultadoSubida.Reintentar)]
    [TestCase(302, ResultadoSubida.Reintentar)]
    public void Subir_ClasificaElCodigoHttp(int codigo, ResultadoSubida esperado)
    {
        var cliente = new ClienteSubida(new TransporteFalso().Responde(codigo));

        Assert.AreEqual(esperado, cliente.Subir(SesionesDePrueba.Perfil, SesionesDePrueba.Basica(), Token));
    }

    [Test]
    public void Subir_ValoresLimiteDeLosCodigos_199Y300ComoRespuestaInesperada()
    {
        Assert.AreEqual(ResultadoSubida.Reintentar, ClienteSubida.Clasificar(199));
        Assert.AreEqual(ResultadoSubida.Subida, ClienteSubida.Clasificar(299));
        Assert.AreEqual(ResultadoSubida.Reintentar, ClienteSubida.Clasificar(300));
        Assert.AreEqual(ResultadoSubida.Rechazada, ClienteSubida.Clasificar(499));
        Assert.AreEqual(ResultadoSubida.Reintentar, ClienteSubida.Clasificar(599));
    }

    [Test]
    public void Subir_SinConexion_DevuelveReintentarSinLanzarExcepcion()
    {
        var cliente = new ClienteSubida(new TransporteFalso().SinRed());

        Assert.AreEqual(ResultadoSubida.Reintentar, cliente.Subir(SesionesDePrueba.Perfil, SesionesDePrueba.Basica(), Token));
    }

    [TestCase(null)]
    [TestCase("")]
    public void Subir_SinToken_NoAutorizadoYNoSeEnvia(string token)
    {
        var transporte = new TransporteFalso();
        var cliente = new ClienteSubida(transporte);

        ResultadoSubida resultado = cliente.Subir(SesionesDePrueba.Perfil, SesionesDePrueba.Basica(), token);

        Assert.AreEqual(ResultadoSubida.NoAutorizado, resultado);
        Assert.AreEqual(0, transporte.Cuerpos.Count);
    }

    [Test]
    public void Subir_SesionInvalida_NoSeEnvia()
    {
        var transporte = new TransporteFalso();
        var cliente = new ClienteSubida(transporte);
        SesionDeJuego sesion = SesionesDePrueba.Basica();
        sesion.DuracionActivaSeg = -5;

        ResultadoSubida resultado = cliente.Subir(SesionesDePrueba.Perfil, sesion, Token);

        Assert.AreEqual(ResultadoSubida.Invalida, resultado);
        Assert.AreEqual(0, transporte.Cuerpos.Count);
    }

    [Test]
    public void Subir_UnReintento_EnviaExactamenteElMismoCuerpo()
    {
        // Idempotencia: la sesión conserva sus ids, así el servidor no duplica al recibirla dos veces
        var transporte = new TransporteFalso().SinRed().Responde(200);
        var cliente = new ClienteSubida(transporte);
        SesionDeJuego sesion = SesionesDePrueba.ConMetricas();

        ResultadoSubida primero = cliente.Subir(SesionesDePrueba.Perfil, sesion, Token);
        ResultadoSubida segundo = cliente.Subir(SesionesDePrueba.Perfil, sesion, Token);

        Assert.AreEqual(ResultadoSubida.Reintentar, primero);
        Assert.AreEqual(ResultadoSubida.Subida, segundo);
        Assert.AreEqual(transporte.Cuerpos[0], transporte.Cuerpos[1]);
    }
}
