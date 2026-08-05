
using UnityEngine;
using UnityEngine.SceneManagement;

public class CrearCuentaUI : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject panelProfesional;
    public GameObject panelFamilia;

    public GameObject soyPadre;
    public GameObject soyProfesional;

    public GameObject atras;

    void Start()
    {
        // Arrancan ocultos hasta que el usuario elija
        panelProfesional.SetActive(false);
        panelFamilia.SetActive(false);
    }

    // Boton "Profesinonal" -> OnClick() -> CrearCuentaUI.MostrarPanelProfesional
    public void MostrarPanelProfesional()
    {
        panelProfesional.SetActive(true);
        panelFamilia.SetActive(false);
        soyPadre.SetActive(false);
        soyProfesional.SetActive(false);
    }

    // Boton "Familia" -> OnClick() -> CrearCuentaUI.MostrarPanelFamilia
    public void MostrarPanelFamilia()
    {
        panelFamilia.SetActive(true);
        panelProfesional.SetActive(false);
        soyPadre.SetActive(false);
        soyProfesional.SetActive(false);
    }

    public void Atras()
    {
        bool familiaActiva = panelFamilia.activeSelf;
        bool profesionalActiva = panelProfesional.activeSelf;

        if ( familiaActiva || profesionalActiva)
        {
            panelProfesional.SetActive(false);
            panelFamilia.SetActive(false);
            soyPadre.SetActive(true);
            soyProfesional.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene("IniciarSesion");
        }
    }
}
 

