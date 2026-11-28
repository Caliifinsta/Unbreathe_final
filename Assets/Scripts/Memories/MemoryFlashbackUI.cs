using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbreathe.Physics;
using Unbreathe.Sanity;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Memories
{
    /// <summary>
    /// Cinemática de Flashback 2D a pantalla completa (Fase 4).
    /// Muestra la imagen con un efecto Ken Burns (zoom suave 1.0 a 1.08),
    /// reproduce las líneas con máquina de escribir y devuelve el control al terminar.
    /// </summary>
    public class MemoryFlashbackUI : MonoBehaviour
    {
        [Header("Estructura de la Interfaz")]
        [Tooltip("Contenedor raíz del panel de flashback.")]
        [SerializeField] private GameObject flashbackRoot;

        [Tooltip("CanvasGroup para el fundido a negro y desvanecimiento.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Tooltip("Componente Image que muestra la foto del recuerdo.")]
        [SerializeField] private Image memoryPhoto;

        [Tooltip("Texto para las líneas del flashback con máquina de escribir.")]
        [SerializeField] private TMP_Text subtitleText;

        [Tooltip("Texto auxiliar de controles en la esquina inferior derecha.")]
        [SerializeField] private TMP_Text skipPromptText;

        [Header("Configuración Cinemática")]
        [Tooltip("Velocidad de escritura (segundos por caracter).")]
        [SerializeField] private float typewriterSpeed = 0.035f;

        [Tooltip("Duración del fade de entrada y salida (segundos).")]
        [SerializeField] private float fadeDuration = 0.8f;

        [Tooltip("Escala máxima del efecto Ken Burns.")]
        [SerializeField] private float kenBurnsMaxScale = 1.08f;

        [Header("Referencias de Sistemas")]
        [SerializeField] private PlayerController3D playerController;
        [SerializeField] private SanityManager sanityManager;

        private Coroutine activeRoutine;
        private bool isTyping = false;
        private bool isPlaying = false;
        private bool skipRequested = false;

        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            if (flashbackRoot != null)
            {
                flashbackRoot.SetActive(false);
            }

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController3D>();
            }

            if (sanityManager == null)
            {
                sanityManager = FindFirstObjectByType<SanityManager>();
            }
        }

        private void Update()
        {
            if (!isPlaying) return;

            // Escuchar escape para saltar
            bool escapePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                escapePressed = true;
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                escapePressed = true;
            }
#endif

            if (escapePressed)
            {
                skipRequested = true;
            }
        }

        public void PlayFlashback(MemoryData data, bool isFirstDiscovery, Action onComplete)
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
            }

            activeRoutine = StartCoroutine(FlashbackSequence(data, isFirstDiscovery, onComplete));
        }

        private IEnumerator FlashbackSequence(MemoryData data, bool isFirstDiscovery, Action onComplete)
        {
            isPlaying = true;
            skipRequested = false;

            ResolveReferences();

            // 1. Pausar controles y cordura
            if (playerController != null) playerController.enabled = false;
            if (sanityManager != null) sanityManager.SetAutoDecay(false);

            // 2. Preparar UI
            if (flashbackRoot != null) flashbackRoot.SetActive(true);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

            if (memoryPhoto != null)
            {
                if (data.photo != null)
                {
                    memoryPhoto.sprite = data.photo;
                    memoryPhoto.color = Color.white;
                }
                else
                {
                    // Placeholder estilizado si aún no hay sprite asignado
                    memoryPhoto.sprite = null;
                    memoryPhoto.color = new Color(0.18f, 0.22f, 0.28f, 1f);
                }
                memoryPhoto.transform.localScale = Vector3.one;
            }

            if (subtitleText != null) subtitleText.text = "";
            if (skipPromptText != null) skipPromptText.text = "[Espacio / Clic] Continuar  |  [Esc] Saltar";

            // 3. Fade In suave
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;

            // 4. Reproducir líneas de diálogo con Ken Burns continuo
            int lineIndex = 0;
            string[] lines = data.flashbackLines != null && data.flashbackLines.Length > 0
                ? data.flashbackLines
                : new string[] { data.title };

            float kenBurnsTimer = 0f;
            float totalDurationEstimate = lines.Length * 4f + 2f;

            while (lineIndex < lines.Length && !skipRequested)
            {
                string fullLine = lines[lineIndex];
                if (subtitleText != null) subtitleText.text = "";

                isTyping = true;
                int charIndex = 0;

                while (charIndex <= fullLine.Length && !skipRequested)
                {
                    if (subtitleText != null) subtitleText.text = fullLine.Substring(0, charIndex);

                    // Efecto Ken Burns
                    kenBurnsTimer += typewriterSpeed;
                    float scaleT = Mathf.Clamp01(kenBurnsTimer / totalDurationEstimate);
                    if (memoryPhoto != null)
                    {
                        memoryPhoto.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * kenBurnsMaxScale, scaleT);
                    }

                    // Chequear avance anticipado de la línea actual
                    if (CheckAdvanceInput())
                    {
                        if (subtitleText != null) subtitleText.text = fullLine;
                        yield return new WaitForSeconds(0.15f);
                        break;
                    }

                    charIndex++;
                    yield return new WaitForSeconds(typewriterSpeed);
                }

                isTyping = false;

                // Esperar a que el jugador pulse Espacio o Clic para pasar a la siguiente línea
                bool advanced = false;
                while (!advanced && !skipRequested)
                {
                    kenBurnsTimer += Time.deltaTime;
                    float scaleT = Mathf.Clamp01(kenBurnsTimer / totalDurationEstimate);
                    if (memoryPhoto != null)
                    {
                        memoryPhoto.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * kenBurnsMaxScale, scaleT);
                    }

                    if (CheckAdvanceInput())
                    {
                        advanced = true;
                    }
                    yield return null;
                }

                lineIndex++;
                yield return new WaitForSeconds(0.1f);
            }

            // 5. Fade Out suave
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                if (panelCanvasGroup != null) panelCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                yield return null;
            }

            if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;
            if (flashbackRoot != null) flashbackRoot.SetActive(false);

            // 6. Recompensa de cordura solo en la primera recolección
            if (isFirstDiscovery && sanityManager != null)
            {
                sanityManager.RecoverSanity(data.sanityReward);
            }

            // 7. Devolver control a Karol y reanudar cordura
            if (playerController != null) playerController.enabled = true;
            if (sanityManager != null) sanityManager.SetAutoDecay(true);

            isPlaying = false;
            activeRoutine = null;

            onComplete?.Invoke();
        }

        private bool CheckAdvanceInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                return true;
            }
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.E) || UnityEngine.Input.GetMouseButtonDown(0))
            {
                return true;
            }
#endif
            return false;
        }
    }
}
