using UnityEngine;
using TMPro;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Interfaz no intrusiva que muestra la tecla de interacción 'E' y la descripción del elemento cercano.
    /// Funciona 100% orientada a eventos, garantizando cero llamadas innecesarias en Update().
    /// </summary>
    [DisallowMultipleComponent]
    public class InteractionPromptUI : MonoBehaviour
    {
        [Header("Referencias Visuales")]
        [SerializeField] private GameObject promptContainer;
        [SerializeField] private TextMeshProUGUI promptLabel;

        [Header("Jugador")]
        [SerializeField] private PlayerInteraction playerInteraction;

        private void Awake()
        {
            if (promptContainer != null)
            {
                promptContainer.SetActive(false);
            }
        }

        private void Start()
        {
            if (playerInteraction == null)
            {
                playerInteraction = FindFirstObjectByType<PlayerInteraction>();
            }

            if (playerInteraction != null)
            {
                playerInteraction.OnInteractableTargetChanged += UpdatePrompt;
            }
        }

        private void OnDestroy()
        {
            if (playerInteraction != null)
            {
                playerInteraction.OnInteractableTargetChanged -= UpdatePrompt;
            }
        }

        private void UpdatePrompt(IInteractable interactable)
        {
            if (interactable != null && interactable.CanInteract())
            {
                if (promptLabel != null)
                {
                    promptLabel.text = interactable.GetPromptText();
                }

                if (promptContainer != null)
                {
                    promptContainer.SetActive(true);
                }
            }
            else
            {
                if (promptContainer != null)
                {
                    promptContainer.SetActive(false);
                }
            }
        }
    }
}
