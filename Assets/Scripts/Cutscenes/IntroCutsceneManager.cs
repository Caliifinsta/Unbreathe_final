using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbreathe.Physics;
using Unbreathe.Sanity;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Cutscenes
{
    /// <summary>
    /// Gestiona la cinemática de apertura (Fase 1) en "Primer plano".
    /// Karol despierta en la calle vacía. Controla el desvanecimiento de negro,
    /// las barras cinematográficas (letterbox), el monólogo interior con efecto
    /// máquina de escribir y devuelve el control al terminar o saltar.
    /// </summary>
    public class IntroCutsceneManager : MonoBehaviour
    {
        [Header("Referencias de Jugador y Sistemas")]
        [Tooltip("Controlador de movimiento y cámara de Karol a desactivar durante la intro.")]
        [SerializeField] private PlayerController3D playerController;

        [Tooltip("Gestor de cordura para pausar el decremento temporalmente.")]
        [SerializeField] private SanityManager sanityManager;

        [Tooltip("Canvas del HUD de cordura para ocultarlo durante la cinemática.")]
        [SerializeField] private GameObject sanityHUDCanvas;

        [Header("Objetivos")]
        [Tooltip("Indicador de objetivos para activar la libreta al terminar.")]
        [SerializeField] private Unbreathe.UI.ObjectiveIndicator objectiveIndicator;
        [SerializeField] private Transform initialObjectiveTarget;
        [SerializeField] private string initialObjectiveText = "Objetivo: Revisa la libreta en la banca";

        [Header("Referencias de Interfaz Cinemática")]
        [Tooltip("Panel negro de pantalla completa para el Fade In inicial.")]
        [SerializeField] private CanvasGroup fadePanelGroup;

        [Tooltip("Grupo que contiene las barras de cine superior e inferior.")]
        [SerializeField] private CanvasGroup letterboxGroup;

        [Tooltip("Grupo que contiene los subtítulos y el texto de ayuda.")]
        [SerializeField] private CanvasGroup subtitleGroup;

        [Tooltip("Texto principal donde se reproduce el monólogo interior.")]
        [SerializeField] private TMP_Text subtitleText;

        [Tooltip("Texto secundario de ayuda [Espacio] Continuar | [Esc] Saltar.")]
        [SerializeField] private TMP_Text skipPromptText;

        [Header("Configuración Cinemática")]
        [Tooltip("Duración del Fade In lento desde negro al inicio (segundos).")]
        [SerializeField] private float fadeDuration = 2.0f;

        [Tooltip("Velocidad de escritura en segundos por caracter.")]
        [SerializeField] private float typewriterSpeed = 0.035f;

        [Tooltip("Duración del desvanecimiento final de barras y subtítulos.")]
        [SerializeField] private float exitFadeDuration = 1.0f;

        [Header("Líneas de Monólogo")]
        [TextArea(2, 4)]
        [SerializeField] private string[] introLines = new string[]
        {
            "¿Qué hora es...? No recuerdo cómo llegué hasta aquí.",
            "Esta es mi calle... la tienda, las bancas, los edificios... todo sigue igual, pero no hay nadie.",
            "Solo este silencio... como si el mundo se hubiera detenido.",
            "Espera... hay algo sobre aquella banca. Debería acercarme a revisar."
        };

        private bool isTyping = false;
        private bool isCutsceneFinished = false;
        private Coroutine cutsceneRoutine;

        public bool IsTyping => isTyping;
        public bool IsCutsceneFinished => isCutsceneFinished;

        private void Awake()
        {
            ResolveReferences();

            // Preparar estado inicial de la cinemática
            if (fadePanelGroup != null)
            {
                fadePanelGroup.alpha = 1f;
                fadePanelGroup.blocksRaycasts = true;
            }

            if (letterboxGroup != null)
            {
                letterboxGroup.alpha = 1f;
                letterboxGroup.blocksRaycasts = false;
            }

            if (subtitleGroup != null)
            {
                subtitleGroup.alpha = 1f;
                subtitleGroup.blocksRaycasts = false;
            }

            if (subtitleText != null)
            {
                subtitleText.text = "";
            }

            // Ocultar barra de cordura y pausar decremento
            if (sanityHUDCanvas != null)
            {
                sanityHUDCanvas.SetActive(false);
            }

            if (sanityManager != null)
            {
                sanityManager.SetAutoDecay(false);
            }

            // Bloquear control del jugador
            if (playerController != null)
            {
                playerController.enabled = false;
            }
        }

        private void Start()
        {
            cutsceneRoutine = StartCoroutine(PlayCutscene());
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                if (playerObj != null) playerController = playerObj.GetComponent<PlayerController3D>();
            }

            if (sanityManager == null)
            {
                sanityManager = SanityManager.Instance;
                if (sanityManager == null)
                {
                    var sObj = GameObject.Find("SanitySystem");
                    if (sObj != null) sanityManager = sObj.GetComponent<SanityManager>();
                }
            }

            if (sanityHUDCanvas == null)
            {
                sanityHUDCanvas = GameObject.Find("SanityHUD");
            }

            if (objectiveIndicator == null)
            {
                objectiveIndicator = FindFirstObjectByType<Unbreathe.UI.ObjectiveIndicator>();
            }

            if (initialObjectiveTarget == null)
            {
                var magicBook = GameObject.Find("Magic Book");
                if (magicBook != null) initialObjectiveTarget = magicBook.transform;
            }
        }

        private void Update()
        {
            if (isCutsceneFinished) return;

            // Detección de salto de cinemática con tecla Escape
            if (CheckSkipInput())
            {
                SkipCutscene();
            }
        }

        private bool CheckAdvanceInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetMouseButtonDown(0)) return true;
            }
            catch
            {
                // Silenciar excepciones si el modo legacy no está habilitado
            }

            return false;
        }

        private bool CheckSkipInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
#endif
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch
            {
                // Silenciar excepciones si el modo legacy no está habilitado
            }

            return false;
        }

        private IEnumerator PlayCutscene()
        {
            // 1. Fade Out lento desde negro hacia la calle mientras Karol despierta
            yield return new WaitForSeconds(0.2f);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (fadePanelGroup != null)
                {
                    fadePanelGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                }
                yield return null;
            }

            if (fadePanelGroup != null)
            {
                fadePanelGroup.alpha = 0f;
                fadePanelGroup.blocksRaycasts = false;
            }

            yield return new WaitForSeconds(0.5f);

            // 2. Ciclo de subtítulos con efecto máquina de escribir
            for (int i = 0; i < introLines.Length; i++)
            {
                if (isCutsceneFinished) yield break;

                string fullText = introLines[i];
                yield return StartCoroutine(TypewriterRoutine(fullText));

                // Esperar a que el jugador pulse Espacio o Clic para continuar
                while (!CheckAdvanceInput() && !isCutsceneFinished)
                {
                    yield return null;
                }

                yield return null; // Esperar un frame para evitar lecturas dobles
            }

            // 3. Finalizar la cinemática
            yield return StartCoroutine(EndCutsceneRoutine());
        }

        private IEnumerator TypewriterRoutine(string line)
        {
            isTyping = true;
            if (subtitleText != null) subtitleText.text = "";

            for (int i = 0; i <= line.Length; i++)
            {
                if (isCutsceneFinished) yield break;

                // Si el jugador presiona Espacio o Clic mientras escribe, completar la frase al instante
                if (CheckAdvanceInput())
                {
                    if (subtitleText != null) subtitleText.text = line;
                    break;
                }

                if (subtitleText != null) subtitleText.text = line.Substring(0, i);
                yield return new WaitForSeconds(typewriterSpeed);
            }

            isTyping = false;
        }

        /// <summary>
        /// Salta inmediatamente el monólogo y devuelve el control al jugador.
        /// </summary>
        public void SkipCutscene()
        {
            if (isCutsceneFinished) return;
            isCutsceneFinished = true;

            if (cutsceneRoutine != null) StopCoroutine(cutsceneRoutine);
            StartCoroutine(EndCutsceneRoutine());
        }

        private IEnumerator EndCutsceneRoutine()
        {
            isCutsceneFinished = true;

            // Asegurar que el fade negro esté en 0
            if (fadePanelGroup != null)
            {
                fadePanelGroup.alpha = 0f;
                fadePanelGroup.blocksRaycasts = false;
            }

            // Desvanecer barras de cine y panel de subtítulos
            float elapsed = 0f;
            float startLetterboxAlpha = letterboxGroup != null ? letterboxGroup.alpha : 1f;
            float startSubAlpha = subtitleGroup != null ? subtitleGroup.alpha : 1f;

            while (elapsed < exitFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / exitFadeDuration;

                if (letterboxGroup != null) letterboxGroup.alpha = Mathf.Lerp(startLetterboxAlpha, 0f, t);
                if (subtitleGroup != null) subtitleGroup.alpha = Mathf.Lerp(startSubAlpha, 0f, t);

                yield return null;
            }

            if (letterboxGroup != null)
            {
                letterboxGroup.alpha = 0f;
                letterboxGroup.gameObject.SetActive(false);
            }

            if (subtitleGroup != null)
            {
                subtitleGroup.alpha = 0f;
                subtitleGroup.gameObject.SetActive(false);
            }

            // Reactivar HUD de cordura
            if (sanityHUDCanvas != null)
            {
                sanityHUDCanvas.SetActive(true);
            }

            // Reanudar pérdida de cordura
            if (sanityManager != null)
            {
                sanityManager.SetAutoDecay(true);
            }

            // Devolver control de movimiento y giro a Karol
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Asignar objetivo inicial
            if (objectiveIndicator != null && initialObjectiveTarget != null)
            {
                objectiveIndicator.SetObjective(initialObjectiveTarget, initialObjectiveText);
            }
        }
    }
}
