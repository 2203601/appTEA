using NUnit.Framework;

public class TemporizadorInactividadTests
{
    [Test]
    public void AntesDelUmbral_NoDispara()
    {
        // Arrange
        var temporizador = new TemporizadorInactividad(20f);

        // Act
        bool disparo = temporizador.Avanzar(19.9f);

        // Assert
        Assert.IsFalse(disparo);
    }

    [Test]
    [Category("Smoke")]
    public void EnElUmbralExacto_Dispara()
    {
        var temporizador = new TemporizadorInactividad(20f);

        Assert.IsTrue(temporizador.Avanzar(20f));
    }

    [Test]
    public void AcumulaVariosCuadros_HastaCruzarElUmbral()
    {
        var temporizador = new TemporizadorInactividad(1f);

        Assert.IsFalse(temporizador.Avanzar(0.4f));
        Assert.IsFalse(temporizador.Avanzar(0.4f));
        Assert.IsTrue(temporizador.Avanzar(0.4f));
    }

    [Test]
    public void DisparaUnaSolaVez_HastaReiniciar()
    {
        var temporizador = new TemporizadorInactividad(5f);
        temporizador.Avanzar(5f);

        Assert.IsFalse(temporizador.Avanzar(5f));

        temporizador.Reiniciar();
        Assert.IsFalse(temporizador.Avanzar(4f));
        Assert.IsTrue(temporizador.Avanzar(1f));
    }

    [Test]
    public void Reiniciar_AntesDelUmbral_VuelveAEmpezarLaCuenta()
    {
        var temporizador = new TemporizadorInactividad(10f);
        temporizador.Avanzar(9f);

        temporizador.Reiniciar();

        Assert.IsFalse(temporizador.Avanzar(9f));
    }

    [Test]
    public void ConDeltaCero_NoAvanza_ComoEnPausa()
    {
        // En pausa Time.deltaTime vale 0: la cuenta no debe moverse.
        var temporizador = new TemporizadorInactividad(3f);

        for (int i = 0; i < 1000; i++) Assert.IsFalse(temporizador.Avanzar(0f));
        Assert.IsTrue(temporizador.Avanzar(3f));
    }
}
