// Cuenta tiempo sin actividad para disparar una pista (ej. 20 s en Encajar, 3 min en Memoria).
// Se alimenta con Time.deltaTime, así que con la pausa (timeScale = 0) no avanza.
// Dispara UNA vez por ciclo; Reiniciar() lo vuelve a armar.
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

    // true solo en el cuadro en que se cruza el umbral
    public bool Avanzar(float deltaSegundos)
    {
        if (disparado) return false;

        acumulado += deltaSegundos;
        if (acumulado < Umbral) return false;

        disparado = true;
        return true;
    }
}
