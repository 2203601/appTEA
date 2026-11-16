using UnityEngine;
using UnityEngine.UI;

// Destello suave para las pistas (adaptación TEA: lento, sin saltos, sin sonido).
// - Si el objeto es UI (Image): un borde cálido que respira (~2,5 s por ciclo).
// - Si es un sprite (SpriteRenderer, ej. piezas del rompecabezas): late apenas de tamaño.
// Usa Time.deltaTime: con la pausa (timeScale = 0) queda quieto.
public class PistaBrillo : MonoBehaviour
{
    static readonly Color ColorBorde = new Color(1f, 0.85f, 0.3f, 1f);
    const float CicloSegundos = 2.5f;

    Outline contorno;
    bool contornoPropio;
    Vector3 escalaOriginal;
    bool esUI;
    float t;

    public static void Activar(GameObject objetivo)
    {
        if (objetivo == null) return;
        var pista = objetivo.GetComponent<PistaBrillo>();
        if (pista == null) objetivo.AddComponent<PistaBrillo>();
    }

    public static void Desactivar(GameObject objetivo)
    {
        if (objetivo == null) return;
        var pista = objetivo.GetComponent<PistaBrillo>();
        if (pista != null) Destroy(pista);
    }

    void OnEnable()
    {
        esUI = GetComponent<Graphic>() != null;
        if (esUI)
        {
            contorno = GetComponent<Outline>();
            if (contorno == null)
            {
                contorno = gameObject.AddComponent<Outline>();
                contornoPropio = true;
            }
            contorno.effectDistance = new Vector2(7f, -7f);
            contorno.effectColor = new Color(ColorBorde.r, ColorBorde.g, ColorBorde.b, 0.2f);
        }
        else
        {
            escalaOriginal = transform.localScale;
        }
    }

    void Update()
    {
        t += Time.deltaTime;
        float onda = (1f - Mathf.Cos(t * 2f * Mathf.PI / CicloSegundos)) * 0.5f;   // 0..1

        if (esUI)
            contorno.effectColor = new Color(ColorBorde.r, ColorBorde.g, ColorBorde.b, Mathf.Lerp(0.2f, 0.75f, onda));
        else
            transform.localScale = escalaOriginal * (1f + 0.06f * onda);
    }

    void OnDisable()
    {
        if (esUI)
        {
            if (contornoPropio && contorno != null) Destroy(contorno);
            contorno = null;
            contornoPropio = false;
        }
        else
        {
            transform.localScale = escalaOriginal;
        }
    }
}
