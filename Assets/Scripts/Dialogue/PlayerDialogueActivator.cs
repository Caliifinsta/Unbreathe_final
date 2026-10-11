using System.Collections.Generic;
using UnityEngine;
using Unbreathe.Input;

namespace Unbreathe.Dialogue
{
    /// <summary>
    /// Maneja la activación y el avance de diálogos mediante la tecla 'Espacio'.
    /// Regla AGENTS.md: Narrativa: Tecla 'Espacio' (avanzar o activar sistema de diálogos).
    /// Regla de rendimiento: Almacenar referencias en Awake() y sin GetComponent en Update().
    /// </summary>
    [RequireComponent(typeof(PlayerInputHandler))]
    [DisallowMultipleComponent]
    public class PlayerDialogueActivator : MonoBehaviour
    {
        [Header("Referencias Opcionales (Se autovinculan en Awake)")]
        [SerializeField] private DialogueManager dialogueManager;

        private PlayerInputHandler inputHandler;
        private readonly List<DialogueTrigger> nearbyTriggers = new List<DialogueTrigger>();

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            inputHandler = GetComponent<PlayerInputHandler>();

            if (dialogueManager == null)
            {
                dialogueManager = DialogueManager.Instance;
            }
        }

        private void OnEnable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnDialogueAdvancePressed += HandleDialogueAdvancePressed;
            }
        }

        private void OnDisable()
        {
            if (inputHandler != null)
            {
                inputHandler.OnDialogueAdvancePressed -= HandleDialogueAdvancePressed;
            }
        }

        private void HandleDialogueAdvancePressed()
        {
            // Si el DialogueManager aún no estaba listo en Awake, resolverlo una única vez
            if (dialogueManager == null)
            {
                dialogueManager = DialogueManager.Instance;
            }

            if (dialogueManager != null && dialogueManager.IsDialogueActive)
            {
                // Tecla Espacio: Avanza el diálogo actual
                dialogueManager.AdvanceDialogue();
                return;
            }

            // Si no hay diálogo activo, intentar activar uno cercano con Espacio
            nearbyTriggers.RemoveAll(t => t == null);

            for (int i = 0; i < nearbyTriggers.Count; i++)
            {
                DialogueTrigger trigger = nearbyTriggers[i];
                if (trigger != null && trigger.CanTrigger)
                {
                    trigger.TriggerDialogue();
                    break;
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Regla AGENTS.md: Detección por colisiones, cero llamadas en Update()
            DialogueTrigger trigger = other.GetComponent<DialogueTrigger>();
            if (trigger != null && !nearbyTriggers.Contains(trigger))
            {
                nearbyTriggers.Add(trigger);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            DialogueTrigger trigger = other.GetComponent<DialogueTrigger>();
            if (trigger != null && nearbyTriggers.Contains(trigger))
            {
                nearbyTriggers.Remove(trigger);
            }
        }
    }
}
