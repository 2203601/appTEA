// Cuenta tiempo sin actividad para disparar una pista. Dispara una sola vez hasta que se llame a Reiniciar().
public class TemporizadorInactividad
{
    public float Umbral { get; set; }

    float acumulado;
    bool disparado;

    public TemporizadorInactividad(float umbralSegundos)
    {
        Umbral = umbralSegundos;
    }

    public void Reiniciar()
    {
        acumulado = 0f;
        disparado = false;
    }

    public bool Avanzar(float deltaSegundos)
    {
        if (disparado) return false;

        acumulado += deltaSegundos;
        if (acumulado < Umbral) return false;

        disparado = true;
        return true;
    }
}
