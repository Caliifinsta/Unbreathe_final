using UnityEngine;
using TMPro;

namespace Unbreathe.Dialogue
{
    /// <summary>
    /// Vista de UI no intrusiva para los diálogos narrativos de UNBREATHE.
    /// Respeta la regla de AGENTS.md sobre interfaces minimalistas y atmosféricas.
    /// Se conecta a los eventos de DialogueManager para evitar llamadas en Update().
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueUI : MonoBehaviour
    {
        [Header("Elementos Visuales Minimalistas")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private TextMeshProUGUI promptHintText;

        [Header("Referencia al Administrador")]
        [SerializeField] private DialogueManager dialogueManager;

        private void Awake()
        {
            if (dialogueManager == null)
            {
                dialogueManager = DialogueManager.Instance;
            }

            if (promptHintText != null)
            {
                promptHintText.text = "[Espacio] Continuar";
            }

            Hide();
        }

        private void Start()
        {
            // Fallback en Start si DialogueManager se inicializó en el mismo frame
            if (dialogueManager == null)
            {
                dialogueManager = DialogueManager.Instance;
            }

            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStarted += Show;
                dialogueManager.OnLineDisplayed += DisplayLine;
                dialogueManager.OnDialogueEnded += Hide;
            }
        }

        private void OnDestroy()
        {
            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStarted -= Show;
                dialogueManager.OnLineDisplayed -= DisplayLine;
                dialogueManager.OnDialogueEnded -= Hide;
            }
        }

        private void Show()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
        }

        private void Hide()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void DisplayLine(DialogueLine line)
        {
            if (line == null) return;

            Show();

            if (speakerText != null)
            {
                speakerText.text = line.speakerName;
            }

            if (messageText != null)
            {
                messageText.text = line.message;
            }
        }
    }
}
