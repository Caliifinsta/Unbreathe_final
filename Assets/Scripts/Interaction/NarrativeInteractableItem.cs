using UnityEngine;
using Unbreathe.Dialogue;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Elemento interactivo del entorno que dispara un evento narrativo o diálogo al pulsar 'E'.
    /// Cumple las directrices de AGENTS.md: audio diegético, sin llamadas a GetComponent en Update.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NarrativeInteractableItem : MonoBehaviour, IInteractable
    {
        [Header("Configuración Visual y Prompt")]
        [Tooltip("Texto que se muestra en pantalla al acercarse al objeto.")]
        [SerializeField] private string promptText = "[E] Leer libreta";

        [Header("Contenido Narrativo")]
        [Tooltip("Secuencia de diálogos o notas que se mostrarán.")]
        [SerializeField] private DialogueData dialogueData;

        [Header("Audio Diegético")]
        [Tooltip("Sonido característico al interactuar (ej. pasar página, pitido de pantalla).")]
        [SerializeField] private AudioClip inspectSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Reglas")]
        [Tooltip("Si está activo, solo se puede leer una vez.")]
        [SerializeField] private bool inspectOnlyOnce = false;
        private bool hasBeenInspected = false;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public string GetPromptText()
        {
            return promptText;
        }

        public bool CanInteract()
        {
            if (inspectOnlyOnce && hasBeenInspected) return false;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return false;
            return dialogueData != null && dialogueData.lines != null && dialogueData.lines.Length > 0;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract()) return;

            hasBeenInspected = true;
            Debug.Log($"<color=cyan>[NarrativeInteractableItem] Jugador interactuó con '{gameObject.name}'. Iniciando lectura...</color>");

            // Sonido diegético
            if (inspectSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(inspectSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(inspectSound, transform.position);
                }
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.StartDialogue(dialogueData);
            }
            else
            {
                Debug.LogWarning("[NarrativeInteractableItem] No se encontró DialogueManager en la escena.");
            }
        }

        public void SetDialogueData(DialogueData data, string prompt)
        {
            dialogueData = data;
            if (!string.IsNullOrEmpty(prompt))
            {
                promptText = prompt;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1.2f);
            }
        }
    }
}
