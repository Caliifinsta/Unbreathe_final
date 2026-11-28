using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.UI
{
    /// <summary>
    /// Interfaz modal de la libreta de Karol (uGUI).
    /// Muestra un diario minimalista semitransparente oscuro con un sistema de 2 páginas.
    /// Admite avance con [Espacio], [E] o botón 'Siguiente', y cierre con [Esc] o botón 'Cerrar'.
    /// </summary>
    public class NotebookPanelUI : MonoBehaviour
    {
        [Header("Estructura de la Interfaz")]
        [Tooltip("Contenedor raíz del panel de la libreta.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("CanvasGroup para posibles transiciones visuales.")]
        [SerializeField] private CanvasGroup panelGroup;

        [Header("Textos")]
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text pageNumberLabel;
        [SerializeField] private TMP_Text contentLabel;
        [SerializeField] private TMP_Text footerPromptLabel;

        [Header("Botones Opcionales")]
        [SerializeField] private Button actionButton;
        [SerializeField] private Button closeButton;

        private int currentPage = 0;
        private bool isOpen = false;
        private Action onClosedCallback;

        private readonly string[] pageTitles = new string[]
        {
            "Libreta de Karol",
            "Libreta de Karol"
        };

        private readonly string[] pageContents = new string[]
        {
            // Página 1: Historia
            "La ciudad quedó en silencio de golpe. Dejé de responder mensajes, evité mirar atrás y terminé atrapada aquí...\n\nSi estás leyendo esto, no te dejes vencer por la niebla. No estás tan sola como crees.",

            // Página 2: Cómo jugar
            "Explora la calle y busca los recuerdos ocultos.\n\nCada recuerdo que recuperes devolverá claridad a tu mente y alejará las sombras. Completa tu álbum para encontrar la salida."
        };

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (actionButton != null)
            {
                actionButton.onClick.AddListener(OnActionButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        private void Update()
        {
            if (!isOpen) return;

            // Detección de teclas compatibles con New Input System y Legacy
            bool advancePressed = false;
            bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)
                {
                    advancePressed = true;
                }
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    escapePressed = true;
                }
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                advancePressed = true;
            }
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif

            if (escapePressed)
            {
                Close();
            }
            else if (advancePressed)
            {
                OnActionButtonClicked();
            }
        }

        public void Open(Action onClose)
        {
            onClosedCallback = onClose;
            currentPage = 0;
            isOpen = true;

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 1f;
                panelGroup.interactable = true;
                panelGroup.blocksRaycasts = true;
            }

            UpdatePageDisplay();
        }

        public void Close()
        {
            if (!isOpen) return;
            isOpen = false;

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
            }

            Action callback = onClosedCallback;
            onClosedCallback = null;
            callback?.Invoke();
        }

        private void OnActionButtonClicked()
        {
            if (currentPage < pageContents.Length - 1)
            {
                currentPage++;
                UpdatePageDisplay();
            }
            else
            {
                Close();
            }
        }

        private void UpdatePageDisplay()
        {
            if (titleLabel != null)
            {
                titleLabel.text = pageTitles[currentPage];
            }

            if (pageNumberLabel != null)
            {
                pageNumberLabel.text = $"Página {currentPage + 1} / {pageContents.Length}";
            }

            if (contentLabel != null)
            {
                contentLabel.text = pageContents[currentPage];
            }

            if (footerPromptLabel != null)
            {
                if (currentPage < pageContents.Length - 1)
                {
                    footerPromptLabel.text = "[Espacio / E] Siguiente  |  [Esc] Cerrar";
                }
                else
                {
                    footerPromptLabel.text = "[Espacio / E] Cerrar  |  [Esc] Cerrar";
                }
            }
        }
    }
}
