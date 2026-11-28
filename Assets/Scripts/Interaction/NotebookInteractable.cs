using UnityEngine;
using Unbreathe.Physics;
using Unbreathe.Sanity;
using Unbreathe.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Objeto interactivo de la libreta en la banca (Fase 2).
    /// Cuenta con una luz cálida suave con pulso diegético, detección robusta por proximidad
    /// e integración con el sistema oficial IInteractable / InteractionPromptView.
    /// Al abrirla despliega la interfaz modal de lectura pausando el movimiento y la cordura.
    /// Al cerrarla, recupera 15 puntos de cordura y actualiza el objetivo en el HUD.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class NotebookInteractable : MonoBehaviour, IInteractable
    {
        [Header("Detección de Proximidad")]
        [Tooltip("Distancia máxima en metros para interactuar con la libreta.")]
        [SerializeField] private float interactionDistance = 3.2f;

        [Header("Luz Guía Diegética")]
        [Tooltip("Luz puntual cálida que resalta la libreta en medio de la niebla.")]
        [SerializeField] private Light guideLight;
        [SerializeField] private float baseIntensity = 2.0f;
        [SerializeField] private float pulseAmount = 0.5f;
        [SerializeField] private float pulseSpeed = 2.5f;

        [Header("Referencias de Interfaz")]
        [Tooltip("Panel modal de lectura de la libreta.")]
        [SerializeField] private NotebookPanelUI notebookPanel;

        [Tooltip("Indicador de objetivo del HUD para actualizar el texto al cerrar.")]
        [SerializeField] private ObjectiveIndicator objectiveIndicator;

        [Header("Referencias de Jugador y Sistemas")]
        [SerializeField] private PlayerController3D playerController;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private SanityManager sanityManager;

        [Header("Ajustes")]
        [Tooltip("Cantidad de cordura restaurada al leer por primera vez.")]
        [SerializeField] private float sanityRecovery = 15f;

        private bool isPlayerInRange = false;
        private bool isReading = false;
        private bool hasRecoveredSanity = false;

        private void Awake()
        {
            // Asegurar que el collider sea Trigger para CharacterController
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Rigidbody cinemático según directrices de AGENTS.md
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;

            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController3D>();
            }

            if (playerTransform == null && playerController != null)
            {
                playerTransform = playerController.transform;
            }

            if (playerTransform == null)
            {
                var p = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
                if (p != null) playerTransform = p.transform;
            }

            if (sanityManager == null)
            {
                sanityManager = FindFirstObjectByType<SanityManager>();
            }

            if (objectiveIndicator == null)
            {
                objectiveIndicator = FindFirstObjectByType<ObjectiveIndicator>();
            }

            if (notebookPanel == null)
            {
                notebookPanel = FindFirstObjectByType<NotebookPanelUI>();
            }
        }

        private void Update()
        {
            // 1. Pulso suave de la luz guía cálida
            if (guideLight != null)
            {
                guideLight.intensity = baseIntensity + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            }

            // 2. Detección continua por distancia
            if (playerTransform == null)
            {
                ResolveReferences();
            }

            if (playerTransform != null && !isReading)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                isPlayerInRange = dist <= interactionDistance;
            }

            // 3. Si el jugador está dentro del rango y no está leyendo, escuchar la tecla 'E'
            if (isPlayerInRange && !isReading)
            {
                bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactPressed = true;
                }
#else
                if (UnityEngine.Input.GetKeyDown(KeyCode.E))
                {
                    interactPressed = true;
                }
#endif

                if (interactPressed)
                {
                    OpenNotebook();
                }
            }
        }

        public void OpenNotebook()
        {
            if (isReading) return;
            isReading = true;

            ResolveReferences();

            // Pausar movimiento de Karol
            if (playerController != null)
            {
                playerController.enabled = false;
            }

            // Pausar decremento de cordura
            if (sanityManager != null)
            {
                sanityManager.SetAutoDecay(false);
            }

            // Abrir interfaz de lectura
            if (notebookPanel != null)
            {
                notebookPanel.Open(OnNotebookClosed);
            }
        }

        private void OnNotebookClosed()
        {
            isReading = false;

            // Devolver control a Karol
            if (playerController != null)
            {
                playerController.enabled = true;
            }

            // Reanudar cordura
            if (sanityManager != null)
            {
                sanityManager.SetAutoDecay(true);

                // Recuperar 15 puntos de cordura por primera lectura
                if (!hasRecoveredSanity)
                {
                    hasRecoveredSanity = true;
                    sanityManager.RecoverSanity(sanityRecovery);
                }
            }

            // Actualizar objetivo en el HUD
            if (objectiveIndicator != null)
            {
                objectiveIndicator.SetObjective(null, "Objetivo: Explora la calle y busca tus recuerdos (0/3)");
            }
        }

        // Implementación de IInteractable (utilizado por el InteractionPromptView oficial grande)
        public string GetPromptText() => "[E] Examinar libreta";
        public bool CanInteract() => !isReading;
        public void Interact(GameObject interactor) => OpenNotebook();
    }
}
