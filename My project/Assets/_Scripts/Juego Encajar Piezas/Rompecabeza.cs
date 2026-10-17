using UnityEngine;

[CreateAssetMenu(fileName = "Rompecabeza", menuName = "Scriptable Objects/Rompecabeza")]
public class Rompecabeza : ScriptableObject
{
    public string nombre;
 
    [Tooltip("La imagen completa armada. Sirve como referencia/fondo tenue mientras se arma, si querés usarla.")]
    public Sprite imagenCompleta;
 
    [Tooltip("Las 9 piezas EN ORDEN DE LECTURA: índice 0 = fila0_col0 (arriba-izquierda), " +
             "1 = fila0_col1, 2 = fila0_col2, 3 = fila1_col0 ... 8 = fila2_col2 (abajo-derecha).")]
    public Sprite[] piezas = new Sprite[9];
}
