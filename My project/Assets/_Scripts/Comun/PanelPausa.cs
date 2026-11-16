using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en un objeto siempre activo ("Pausa"). BotonMenu llama a Abrir().
// El panel tapa toda la pantalla, así no se puede tocar el juego mientras está en pausa.
public class PanelPausa : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button botonSeguir;
    [SerializeField] private Button botonSalir;
    [SerializeField] private string escenaSalida = "Menu";

    void Awake()
    {
        panel.SetActive(false);
        botonSeguir.onClick.AddListener(Cerrar);
        botonSalir.onClick.AddListener(Salir);
    }

    public void Abrir()
    {
        panel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Cerrar()
    {
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Salir()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaSalida);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
