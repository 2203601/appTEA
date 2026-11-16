using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Cartel de fin de juego. Va en un objeto siempre activo ("PanelFin");
// el GameManager llama a Mostrar() cuando terminan las rondas.
public class PanelFinJuego : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button botonMenu;
    [SerializeField] private string escenaMenu = "Menu";

    void Awake()
    {
        panel.SetActive(false);
        botonMenu.onClick.AddListener(VolverAlMenu);
    }

    public void Mostrar()
    {
        panel.SetActive(true);
    }

    void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenu);
    }
}
