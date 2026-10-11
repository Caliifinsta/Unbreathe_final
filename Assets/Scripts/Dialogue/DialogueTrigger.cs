using UnityEngine;

namespace Unbreathe.Dialogue
{
    /// <summary>
    /// Disparador narrativo en el entorno 3D que activa un diálogo al pulsar Espacio o por proximidad.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("Datos de Diálogo")]
        [SerializeField] private DialogueData dialogueData;

        [Header("Comportamiento")]
        [Tooltip("Si es verdadero, el diálogo inicia inmediatamente al entrar en la zona sin necesidad de pulsar Espacio.")]
        [SerializeField] private bool autoTriggerOnEnter = false;

        [Tooltip("Permite que el diálogo se reproduzca solo una vez.")]
        [SerializeField] private bool triggerOnce = true;

        private bool hasTriggered = false;

        public bool CanTrigger => !triggerOnce || !hasTriggered;
        public DialogueData DialogueData => dialogueData;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public void TriggerDialogue()
        {
            if (!CanTrigger) return;

            if (DialogueManager.Instance != null && dialogueData != null)
            {
                hasTriggered = true;
                DialogueManager.Instance.StartDialogue(dialogueData);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (autoTriggerOnEnter && CanTrigger && other.CompareTag("Player"))
            {
                TriggerDialogue();
            }
        }
    }
}
