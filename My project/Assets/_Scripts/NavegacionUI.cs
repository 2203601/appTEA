using UnityEngine;
using UnityEngine.SceneManagement;
 
// Poner este script en un objeto de cada escena (ej: el Canvas)
// para manejar los botones que cambian de escena.
public class NavegacionUI : MonoBehaviour
{
    // Boton "No tenes cuenta? Registrate" (escena IniciarSesion)
    public void IrACrearCuenta()
    {
        SceneManager.LoadScene("CrearCuenta");
    }
 
     public void IrAInicio()
    {
        SceneManager.LoadScene("INICIO");
    }
 
 
}
