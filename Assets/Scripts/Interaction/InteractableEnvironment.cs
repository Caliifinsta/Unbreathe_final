using UnityEngine;
using UnityEngine.Events;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Componente para examinar elementos del entorno 3D (paredes, válvulas, terminales, puertas) con la tecla 'E'.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractableEnvironment : MonoBehaviour, IInteractable
    {
        [Header("Descripción Ambiental")]
        [SerializeField] private string targetName = "Mecanismo Extraño";
        [SerializeField] private string actionPrompt = "Examinar";

        [TextArea(2, 5)]
        [SerializeField] private string inspectionNarrative = "Una estructura de metal corroída por la humedad... Hay marcas de agua salada.";

        [Header("Eventos de Interacción")]
        [SerializeField] private UnityEvent onInteracted;

        [Header("Opciones")]
        [SerializeField] private bool canInteractMultipleTimes = true;
        [SerializeField] private AudioSource diegeticSound;

        private bool hasInteracted = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (diegeticSound == null)
            {
                diegeticSound = GetComponent<AudioSource>();
            }
        }

        public string GetPromptText()
        {
            return $"[E] {actionPrompt} {targetName}";
        }

        public bool CanInteract()
        {
            return canInteractMultipleTimes || !hasInteracted;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract()) return;

            hasInteracted = true;
            Debug.Log($"[{nameof(InteractableEnvironment)}] Se ha examinado: {targetName}. Nota: \"{inspectionNarrative}\"");

            if (diegeticSound != null)
            {
                diegeticSound.Play();
            }

            onInteracted?.Invoke();
        }
    }
}
