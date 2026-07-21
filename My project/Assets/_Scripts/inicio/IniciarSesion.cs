using UnityEngine;
using UnityEngine.SceneManagement;

public class IniciarSesion : MonoBehaviour
{

    public float hoverScale = 1.2f;

    Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void OnMouseEnter()
    {
        transform.localScale = originalScale * hoverScale;
    }

    void OnMouseExit()
    {
        transform.localScale = originalScale;
    }

    void OnMouseDown()
    {
        SceneManager.LoadScene(2);
    }
}

