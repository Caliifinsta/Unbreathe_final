using UnityEngine;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Interfaz para cualquier elemento interactuable en el entorno 3D de UNBREATHE (objetos recogibles, interruptores, notas, etc.).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Texto o descripción no intrusiva de la interacción (ej. "[E] Recoger Linterna").
        /// </summary>
        string GetPromptText();

        /// <summary>
        /// Determina si el objeto se encuentra disponible para interactuar.
        /// </summary>
        bool CanInteract();

        /// <summary>
        /// Ejecuta la acción al pulsar la tecla 'E'.
        /// </summary>
        /// <param name="interactor">GameObject del jugador que inicia la interacción.</param>
        void Interact(GameObject interactor);
    }
}
