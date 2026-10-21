using NUnit.Framework;

// Regresión del defecto: en Encajar (Difícil) se podían mover las piezas con el panel de pausa abierto.
public class ControlPausaTests
{
    [Test]
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
    public void EnPausa_LaPistaNoAvanzaNiConPasoDeTiempoGrande()
    {
        // Arrange: se simula el Update del juego, que corta el cuadro si está en pausa
        var temporizador = new TemporizadorInactividad(20f);
        bool disparo = false;

        // Act: 100 cuadros de 1 s con el juego en pausa
        for (int i = 0; i < 100; i++)
        {
            if (ControlPausa.EnPausa(0f)) continue;
            disparo |= temporizador.Avanzar(1f);
        }

        // Assert
        Assert.IsFalse(disparo);
    }
}
