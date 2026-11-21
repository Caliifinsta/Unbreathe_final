using UnityEngine;

namespace Unbreathe.UI
{
    /// <summary>
    /// Copia el estado activo/inactivo del objeto donde está puesto hacia otro objeto.
    /// Lo usa MainMenuStyler para que la capa que oscurece el fondo aparezca y desaparezca
    /// junto con el panel de Opciones, sin tocar el código del menú principal.
    /// </summary>
    [DisallowMultipleComponent]
    public class UIActiveMirror : MonoBehaviour
    {
        [SerializeField] private GameObject objetivo;

        public void SetObjetivo(GameObject nuevoObjetivo)
        {
            objetivo = nuevoObjetivo;
        }

        private void OnEnable()
        {
            if (objetivo != null)
            {
                objetivo.SetActive(true);
            }
        }

        private void OnDisable()
        {
            // Al descargar la escena el objetivo puede haberse destruido ya.
            if (objetivo != null)
            {
                objetivo.SetActive(false);
            }
        }
    }
}
