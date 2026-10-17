// Punto 2D propio: la lógica no usa UnityEngine. El GameManager convierte desde/hacia Vector2.
public struct Punto2
{
    public float x;
    public float y;

    public Punto2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }
}
