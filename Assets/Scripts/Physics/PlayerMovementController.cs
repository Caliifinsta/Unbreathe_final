using UnityEngine;
using Unbreathe.Input;

namespace Unbreathe.Physics
{
    /// <summary>
    /// Controlador de físicas del jugador en entorno 3D.
    /// Responsabilidad: calcular el desplazamiento físico 3D, gravedad y colisiones del personaje.
    /// Respeta la regla de rendimiento: almacena referencias en Awake() y nunca usa GetComponent en Update().
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [DisallowMultipleComponent]
    public class PlayerMovementController : MonoBehaviour
    {
        [Header("Parámetros de Movimiento 3D")]
        [Tooltip("Velocidad de movimiento en metros por segundo.")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Suavizado de la aceleración del movimiento.")]
        [SerializeField] private float acceleration = 12f;

        [Header("Físicas y Gravedad 3D")]
        [Tooltip("Fuerza de gravedad aplicada en el eje Y negativo.")]
        [SerializeField] private float gravity = -18f;

        [Tooltip("Fuerza hacia abajo cuando está en el suelo para mantener estabilidad en pendientes.")]
        [SerializeField] private float groundedGravity = -2f;

        [Header("Orientación respecto a la Cámara (Opcional)")]
        [Tooltip("Si está activo, el movimiento WASD se orienta según la rotación de la cámara (W va hacia donde mira la cámara).")]
        [SerializeField] private bool cameraRelativeMovement = true;

        [SerializeField] private Camera referenceCamera;

        // Referencias cacheadas en Awake()
        private CharacterController characterController;
        private PlayerInputHandler inputHandler;
        private Transform cameraTransform;

        // Variables de estado físico
        private Vector3 currentVelocity;
        private float verticalVelocity;

        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;
        public Vector3 HorizontalVelocity { get; private set; }
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsMoving => HorizontalVelocity.sqrMagnitude > 0.01f;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            characterController = GetComponent<CharacterController>();
            inputHandler = GetComponent<PlayerInputHandler>();

            ResolveCamera();
        }

        private void Start()
        {
            // Verificación secundaria en Start por si la cámara se inicializó después
            if (cameraTransform == null)
            {
                ResolveCamera();
            }
        }

        private void ResolveCamera()
        {
            if (referenceCamera == null)
            {
                referenceCamera = Camera.main;
            }

            if (referenceCamera == null)
            {
                referenceCamera = FindFirstObjectByType<Camera>();
            }

            if (referenceCamera != null)
            {
                cameraTransform = referenceCamera.transform;
            }
        }

        private void Update()
        {
            ApplyMovement();
        }

        private void ApplyMovement()
        {
            if (characterController == null || inputHandler == null) return;

            // Si por alguna razón la cámara aún no estaba en caché, resolverla una vez
            if (cameraTransform == null)
            {
                ResolveCamera();
            }

            Vector2 input = inputHandler.MoveInput;

            // Pausar movimiento mientras el jugador lee una libreta, teléfono o diálogo
            if (Dialogue.DialogueManager.Instance != null && Dialogue.DialogueManager.Instance.IsDialogueActive)
            {
                input = Vector2.zero;
            }

            Vector3 targetDirection;

            if (cameraRelativeMovement && cameraTransform != null)
            {
                // Movimiento 100% relativo a la vista de la cámara:
                // W: Avanzar hacia donde apunta la cámara
                // S: Retroceder hacia la cámara
                // A: Desplazarse a la izquierda de la cámara
                // D: Desplazarse a la derecha de la cámara
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                targetDirection = (forward * input.y + right * input.x);
            }
            else
            {
                // Fallback a coordenadas de mundo si no existe ninguna cámara en escena
                targetDirection = new Vector3(input.x, 0f, input.y);
            }

            if (targetDirection.sqrMagnitude > 1f)
            {
                targetDirection.Normalize();
            }

            Vector3 targetHorizontalVelocity = targetDirection * moveSpeed;

            // Aceleración suave
            HorizontalVelocity = Vector3.MoveTowards(
                HorizontalVelocity,
                targetHorizontalVelocity,
                acceleration * Time.deltaTime
            );

            // Gestión de gravedad en 3D
            if (characterController.isGrounded)
            {
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = groundedGravity;
                }
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 finalMotion = HorizontalVelocity + Vector3.up * verticalVelocity;

            // Desplazamiento mediante el motor de físicas de Unity 3D
            characterController.Move(finalMotion * Time.deltaTime);
        }
    }
}
