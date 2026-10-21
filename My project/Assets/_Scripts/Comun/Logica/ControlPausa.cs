// Regla de pausa compartida por los juegos: con Time.timeScale en 0 no avanza nada
// (ni las pistas ni el arrastre de piezas). Está separada del MonoBehaviour para poder probarla.
public static class ControlPausa
{
    public static bool EnPausa(float timeScale)
    {
        return timeScale <= 0f;
    }
}
