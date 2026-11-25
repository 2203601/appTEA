using UnityEngine;

public class Tablerodestinos : MonoBehaviour
{
    [Header("Configuración del grid")]
    [SerializeField] private int filas = 3;
    [SerializeField] private int columnas = 3;
    [SerializeField] private Vector2 tamañoCelda = new Vector2(1f, 1f);
    [SerializeField] private Vector3 esquinaSuperiorIzquierda = Vector3.zero;

    private void GenerarGrid()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject hijo = transform.GetChild(i).gameObject;
    #if UNITY_EDITOR
                DestroyImmediate(hijo);
    #else
                Destroy(hijo);
    #endif
            }

            for (int fila = 0; fila < filas; fila++)
            {
                for (int col = 0; col < columnas; col++)
                {
                    GameObject destino = new GameObject($"Destino_{fila}_{col}");
                    destino.transform.SetParent(transform);
                    destino.transform.position = esquinaSuperiorIzquierda
                        + new Vector3(col * tamañoCelda.x, -fila * tamañoCelda.y, 0f);
                }
            }
        }
}

