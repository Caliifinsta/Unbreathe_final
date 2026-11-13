using UnityEngine;
using Unbreathe.Dialogue;
using Unbreathe.Input;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Physics
{
    /// <summary>
    /// Controlador 3D del jugador para "UNBREATHE".
    /// Responsabilidades:
    /// 1. Movimiento: Procesa el input WASD relativo a la cámara y aplica gravedad con CharacterController.
    /// 2. Rotación: Rota suavemente el modelo hijo (Personaje@Getting Up) con Quaternion.Slerp hacia la dirección de avance.
    /// 3. Animación: Captura el Animator del modelo hijo y envía la magnitud de velocidad al parámetro Float "Speed".
    /// 
    /// Cumple estrictamente con las directrices de AGENTS.md:
    /// - Cero GetComponent() o Find() en Update().
    /// - Resolución segura de cámara (tag MainCamera con respaldo FindFirstObjectByType).
    /// - Silenciamiento de movimiento durante secuencias de diálogo/lectura.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public class PlayerController3D : MonoBehaviour
    {
        [Header("Configuración de Movimiento")]
        [Tooltip("Velocidad de desplazamiento del jugador en unidades/segundo.")]
        [SerializeField] private float moveSpeed = 4.5f;

        [Tooltip("Aceleración suave para arrancar y frenar.")]
        [SerializeField] private float acceleration = 12f;

        [Header("Físicas y Gravedad")]
        [Tooltip("Fuerza gravitatoria aplicada verticalmente en el CharacterController.")]
        [SerializeField] private float gravity = -18f;

        [Tooltip("Fuerza descendente aplicada cuando está en el suelo para pisar firme en pendientes.")]
        [SerializeField] private float groundedGravity = -2f;

        [Header("Rotación Suave")]
        [Tooltip("Tiempo de amortiguación para girar suavemente hacia la nueva dirección (en segundos).")]
        [SerializeField] private float rotationSmoothTime = 0.12f;

        [Tooltip("Transform del modelo 3D hijo (Personaje@Getting Up). Si es null, se detecta automáticamente en Awake().")]
        [SerializeField] private Transform characterModel;

        [Header("Animación")]
        [Tooltip("Componente Animator del modelo 3D hijo. Si es null, se detecta automáticamente en Awake().")]
        [SerializeField] private Animator characterAnimator;

        [Tooltip("Nombre del parámetro Float en el Animator que controla la transición Idle/Walk.")]
        [SerializeField] private string speedParameterName = "Speed";

        [Tooltip("Suavizado de la variable de animación en el Animator (0 para asignación inmediata).")]
        [SerializeField] private float animationDampTime = 0.1f;

        [Header("Orientación de Cámara")]
        [Tooltip("Si está activo, el movimiento WASD se orienta según hacia dónde mira la cámara.")]
        [SerializeField] private bool cameraRelativeMovement = true;

        [SerializeField] private Camera referenceCamera;

        [Header("Control de Cursor")]
        [Tooltip("Bloquea y oculta el cursor en el centro para una experiencia de cámara orbital fluida.")]
        [SerializeField] private bool lockCursor = true;

        // Referencias cacheadas en Awake (Regla AGENTS.md: Cero GetComponent en Update)
        private CharacterController characterController;
        private PlayerInputHandler inputHandler;
        private Transform cameraTransform;
        private int speedHash;

        // Variables de estado físico
        private float verticalVelocity = 0f;
        private Vector3 currentHorizontalVelocity = Vector3.zero;
        private float rotationVelocity;

        // Propiedades públicas para compatibilidad con sistemas diegéticos (Audio, Footsteps, UI)
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;
        public Vector3 HorizontalVelocity => currentHorizontalVelocity;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public bool IsMoving => currentHorizontalVelocity.sqrMagnitude > 0.01f;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            inputHandler = GetComponent<PlayerInputHandler>();
            if (inputHandler == null)
            {
                inputHandler = GetComponentInParent<PlayerInputHandler>();
            }

            // 1. Resolver modelo hijo (Personaje@Getting Up)
            ResolveModelAndAnimator();

            // 2. Cachear hash del parámetro Speed del Animator
            speedHash = Animator.StringToHash(speedParameterName);

            // 3. Resolver referencia de cámara segura según directriz AGENTS.md
            ResolveCamera();
        }

        private void Start()
        {
            if (cameraTransform == null)
            {
                ResolveCamera();
            }

            if (characterAnimator == null || characterModel == null)
            {
                ResolveModelAndAnimator();
            }

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void ResolveModelAndAnimator()
        {
            if (characterModel == null)
            {
                // Buscar hijo llamado "Personaje@Getting Up" o con "Personaje" en el nombre
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.Contains("Personaje") || child.name.Contains("Getting Up"))
                    {
                        characterModel = child;
                        break;
                    }
                }

                // Fallback: primer hijo que tenga Animator o SkinnedMeshRenderer
                if (characterModel == null && transform.childCount > 0)
                {
                    Animator childAnim = GetComponentInChildren<Animator>();
                    if (childAnim != null)
                    {
                        characterModel = childAnim.transform;
                    }
                    else
                    {
                        characterModel = transform.GetChild(0);
                    }
                }
            }

            if (characterAnimator == null && characterModel != null)
            {
                characterAnimator = characterModel.GetComponent<Animator>();
            }

            if (characterAnimator == null)
            {
                characterAnimator = GetComponentInChildren<Animator>();
            }

            if (characterAnimator != null)
            {
                characterAnimator.applyRootMotion = false;
            }

            if (characterModel != null)
            {
                characterModel.localPosition = Vector3.zero;
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
            HandleCursorLock();
            HandleMovementAndGravity();
        }

        private void HandleCursorLock()
        {
            if (!lockCursor) return;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
#endif
        }

        private void HandleMovementAndGravity()
        {
            if (characterController == null) return;

            // 1. Leer input WASD (compatible con New Input System y Legacy Input Manager)
            Vector2 input = Vector2.zero;

            if (inputHandler != null)
            {
                input = inputHandler.MoveInput;
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.wKey.isPressed) input.y += 1f;
                    if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                    if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                    if (Keyboard.current.dKey.isPressed) input.x += 1f;
                    if (input.sqrMagnitude > 1f) input.Normalize();
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (UnityEngine.Input.GetKey(KeyCode.W)) input.y += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) input.y -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A)) input.x -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D)) input.x += 1f;
                if (input.sqrMagnitude > 1f) input.Normalize();
#else
                try
                {
                    if (UnityEngine.Input.GetKey(KeyCode.W)) input.y += 1f;
                    if (UnityEngine.Input.GetKey(KeyCode.S)) input.y -= 1f;
                    if (UnityEngine.Input.GetKey(KeyCode.A)) input.x -= 1f;
                    if (UnityEngine.Input.GetKey(KeyCode.D)) input.x += 1f;
                    if (input.sqrMagnitude > 1f) input.Normalize();
                }
                catch
                {
                    // Evitar que lance excepción si Active Input Handling está exclusivamente en Input System Package
                }
#endif
            }

            // Regla AGENTS.md: Pausar movimiento mientras el jugador lee un documento, libreta o diálogo
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                input = Vector2.zero;
            }

            // 2. Calcular dirección de movimiento en 3D
            Vector3 targetDirection = Vector3.zero;

            if (input.sqrMagnitude > 0.001f)
            {
                input.Normalize();

                if (cameraRelativeMovement && cameraTransform != null)
                {
                    // WASD relativo a la orientación horizontal de la cámara
                    Vector3 camForward = cameraTransform.forward;
                    Vector3 camRight = cameraTransform.right;
                    camForward.y = 0f;
                    camRight.y = 0f;
                    camForward.Normalize();
                    camRight.Normalize();

                    targetDirection = (camForward * input.y + camRight * input.x).normalized;
                }
                else
                {
                    // WASD en coordenadas de mundo
                    targetDirection = new Vector3(input.x, 0f, input.y).normalized;
                }
            }

            // 3. Aceleración suave horizontal
            Vector3 targetHorizontalVelocity = targetDirection * moveSpeed;
            currentHorizontalVelocity = Vector3.MoveTowards(
                currentHorizontalVelocity,
                targetHorizontalVelocity,
                acceleration * Time.deltaTime
            );

            // 4. Gestión de Gravedad y contacto con el suelo
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

            // 5. Aplicar desplazamiento físico 3D mediante CharacterController
            Vector3 displacement = (currentHorizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime;
            characterController.Move(displacement);

            // 6. Rotación suave del modelo hijo con Quaternion.Slerp hacia la dirección de avance
            ApplyModelRotation(targetDirection);

            // 7. Actualización del parámetro "Speed" en el Animator del hijo
            UpdateAnimationSpeed();
        }

        private void ApplyModelRotation(Vector3 movementDirection)
        {
            if (characterModel == null) return;

            // Solo rotar cuando hay una dirección de desplazamiento intencional
            if (movementDirection.sqrMagnitude > 0.001f)
            {
                // Ángulo objetivo en el plano XZ hacia donde se desplaza el personaje
                float targetAngle = Mathf.Atan2(movementDirection.x, movementDirection.z) * Mathf.Rad2Deg;

                // Suavizado de ángulo con Mathf.SmoothDampAngle para eliminar tirones y giros bruscos
                float smoothedAngle = Mathf.SmoothDampAngle(
                    characterModel.eulerAngles.y,
                    targetAngle,
                    ref rotationVelocity,
                    rotationSmoothTime
                );

                characterModel.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);
            }
        }

        private void UpdateAnimationSpeed()
        {
            if (characterAnimator == null) return;

            // Magnitud de la velocidad horizontal (0 cuando está quieto, ~moveSpeed cuando camina)
            float speedMagnitude = currentHorizontalVelocity.magnitude;

            if (animationDampTime > 0.001f)
            {
                characterAnimator.SetFloat(speedHash, speedMagnitude, animationDampTime, Time.deltaTime);
            }
            else
            {
                characterAnimator.SetFloat(speedHash, speedMagnitude);
            }
        }

        /// <summary>
        /// Utilidad para ajustar automáticamente el CharacterController para que cubra con precisión
        /// la malla y altura del personaje humanoide 3D.
        /// </summary>
        [ContextMenu("Ajustar CharacterController al Modelo 3D")]
        public void AutoAdjustCharacterController()
        {
            CharacterController cc = GetComponent<CharacterController>();
            if (cc == null) cc = gameObject.AddComponent<CharacterController>();

            ResolveModelAndAnimator();

            if (characterModel != null)
            {
                Renderer[] renderers = characterModel.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds b = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++)
                    {
                        b.Encapsulate(renderers[i].bounds);
                    }

                    float height = b.size.y;
                    float radius = Mathf.Max(b.extents.x, b.extents.z) * 0.45f;
                    radius = Mathf.Clamp(radius, 0.25f, 0.45f);

                    cc.height = Mathf.Max(height, 1.7f);
                    cc.radius = radius;
                    cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
                    cc.skinWidth = 0.08f;
                    cc.stepOffset = 0.35f;
                    cc.slopeLimit = 45f;
                    return;
                }
            }

            // Calibración estándar para humanoide 3D (altura 1.80m, base en los pies)
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.08f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
        }

        private void Reset()
        {
            AutoAdjustCharacterController();
        }
    }
}
