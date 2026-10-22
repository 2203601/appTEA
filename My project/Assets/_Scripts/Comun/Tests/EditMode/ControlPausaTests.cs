using NUnit.Framework;

// Regresión de D-01: en Encajar (Difícil) se podían mover las piezas con la pausa abierta.
public class ControlPausaTests
{
    [Test]
    [Category("Smoke")]
    public void ConTimeScaleCero_EstaEnPausa()
    {
        // Arrange
        float timeScale = 0f;

        // Act
        bool enPausa = ControlPausa.EnPausa(timeScale);

        // Assert
        Assert.IsTrue(enPausa);
    }

    [TestCase(1f)]
    [TestCase(0.5f)]
    [TestCase(2f)]
    public void ConTimeScaleMayorACero_NoEstaEnPausa(float timeScale)
    {
        Assert.IsFalse(ControlPausa.EnPausa(timeScale));
    }

    [Test]
    [Category("Smoke")]
    public void EnPausa_LaPistaNoAvanzaNiConPasoDeTiempoGrande()
    {
        // Arrange
        var temporizador = new TemporizadorInactividad(20f);
        bool disparo = false;

        // Act
        for (int i = 0; i < 100; i++)
        {
            if (ControlPausa.EnPausa(0f)) continue;
            disparo |= temporizador.Avanzar(1f);
        }

        // Assert
        Assert.IsFalse(disparo);
    }
}
