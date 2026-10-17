using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Animaciones lentas y sin saltos bruscos (adaptación TEA).
// Usan el tiempo normal: al pausar (timeScale = 0) quedan quietas.
public static class AnimacionSuave
{
    public static IEnumerator Escalar(Transform objetivo, float desde, float hasta, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            objetivo.localScale = Vector3.one * Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracion));
            yield return null;
        }
        objetivo.localScale = Vector3.one * hasta;
    }

    public static IEnumerator Alpha(CanvasGroup grupo, float hasta, float duracion)
    {
        float desde = grupo.alpha;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracion));
            yield return null;
        }
        grupo.alpha = hasta;
    }

    // Latido lento de escala (un ciclo cada ~2 s, +5 % como máximo)
    public static IEnumerator Latido(Transform objetivo)
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;
            float onda = (1f - Mathf.Cos(t * Mathf.PI)) * 0.5f;
            objetivo.localScale = Vector3.one * (1f + 0.05f * onda);
            yield return null;
        }
    }
}
