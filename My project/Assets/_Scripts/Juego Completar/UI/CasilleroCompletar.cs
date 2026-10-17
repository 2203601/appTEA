using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Va en el prefab CasilleroLetra: una letra/número de la palabra, o un hueco por completar.

[RequireComponent(typeof(Image))]
public class CasilleroCompletar : MonoBehaviour
{
    [SerializeField] private Image fondo;
    [SerializeField] private Image letra;

    [Header("Colores")]
    [SerializeField] private Color colorHueco = new Color(0.85f, 0.92f, 1f, 1f);

    Coroutine latido;

    public bool EsHueco { get; private set; }

    void Awake()
    {
        if (fondo == null) fondo = GetComponent<Image>();
        if (letra == null) letra = transform.Find("Letra").GetComponent<Image>();
    }

    public void Configurar(Sprite sprite, bool esHueco)
    {
        EsHueco = esHueco;
        letra.sprite = sprite;
        letra.preserveAspect = true;
        letra.enabled = !esHueco;
        if (esHueco) fondo.color = colorHueco;
    }

    public void Resaltar(bool activo)
    {
        if (latido != null) StopCoroutine(latido);
        latido = null;
        transform.localScale = Vector3.one;
        if (activo) latido = StartCoroutine(AnimacionSuave.Latido(transform));
    }

    public void Rellenar(Sprite sprite)
    {
        Resaltar(false);
        EsHueco = false;
        letra.sprite = sprite;
        letra.enabled = true;
        fondo.color = Color.white;
        StartCoroutine(Aparecer());
    }

    IEnumerator Aparecer()
    {
        letra.transform.localScale = Vector3.one * 0.6f;
        yield return AnimacionSuave.Escalar(letra.transform, 0.6f, 1f, 0.5f);
    }
}
