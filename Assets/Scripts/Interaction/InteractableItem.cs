using UnityEngine;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Componente para objetos 3D recolectables en el entorno de UNBREATHE mediante la tecla 'E'.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableItem : MonoBehaviour, IInteractable
    {
        [Header("Información del Objeto")]
        [SerializeField] private string itemName = "Objeto Sumergido";
        [SerializeField] private string actionPrompt = "Recoger";

        [Header("Respuesta de Recolección")]
        [Tooltip("Destruye el objeto inmediatamente al recogerlo o lo desactiva.")]
        [SerializeField] private bool destroyOnCollect = true;

        [Tooltip("Efecto de sonido diegético opcional al recoger.")]
        [SerializeField] private AudioSource audioFeedback;

        private bool isCollected = false;

        private void Awake()
        {
            // Garantizar que el collider actúe como trigger si es para interacción
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (audioFeedback == null)
            {
                audioFeedback = GetComponent<AudioSource>();
            }
        }

        public string GetPromptText()
        {
            return $"[E] {actionPrompt} {itemName}";
        }

        public bool CanInteract()
        {
            return !isCollected;
        }

        public void Interact(GameObject interactor)
        {
            if (isCollected) return;

            isCollected = true;
            Debug.Log($"[{nameof(InteractableItem)}] Objeto recogido: '{itemName}' por {interactor.name}");

            if (audioFeedback != null)
            {
                audioFeedback.Play();
            }

            if (destroyOnCollect)
            {
                Destroy(gameObject, audioFeedback != null && audioFeedback.isPlaying ? audioFeedback.clip.length : 0.05f);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
