using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Cameras
{
    /// <summary>
    /// Controlador de cámara en 3ª persona / órbita con ratón para UNBREATHE.
    /// Permite rotar libremente la cámara con el ratón (Free Look estilo Cinemachine)
    /// y seguir suavemente al personaje.
    /// Respeta estrictamente las directrices de rendimiento de AGENTS.md:
    /// Almacena referencias en Awake() y tiene cero llamadas a Find o GetComponent en Update/LateUpdate.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Objetivo a Seguir")]
        [Tooltip("Transform del Player que la cámara seguirá.")]
        [SerializeField] private Transform target;

        [Header("Control Orbital con Ratón (Free Look)")]
        [Tooltip("Activa la rotación orbital de la cámara con el movimiento del ratón.")]
        [SerializeField] private bool enableMouseOrbit = true;

        [Tooltip("Sensibilidad de giro del ratón.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float mouseSensitivity = 2.5f;

        [Tooltip("Límite inferior de inclinación vertical (grados).")]
        [SerializeField] private float minPitch = -10f;

        [Tooltip("Límite superior de inclinación vertical (grados).")]
        [SerializeField] private float maxPitch = 65f;

        [Header("Distancia y Altura")]
        [Tooltip("Distancia de la cámara respecto al jugador.")]
        [SerializeField] private float distance = 6f;

        [Tooltip("Altura del punto de enfoque sobre el jugador.")]
        [SerializeField] private float heightOffset = 1.6f;

        [Header("Suavizado de Seguimiento")]
        [Tooltip("Tiempo de respuesta del seguimiento de posición.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float positionSmoothTime = 0.08f;

        [Tooltip("Suavizado del giro orbital.")]
        [Range(1f, 30f)]
        [SerializeField] private float rotationSmoothSpeed = 15f;

        [Header("Cursor")]
        [Tooltip("Bloquea y oculta el cursor en el centro de la pantalla al entrar en modo juego.")]
        [SerializeField] private bool lockCursor = true;

        // Variables de estado
        private float yaw = 0f;
        private float pitch = 15f;
        private float targetYaw = 0f;
        private float targetPitch = 15f;
        private Vector3 currentVelocity;
        private Vector3 smoothedTargetPosition;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            if (target == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    target = playerObj.transform;
                }
                else
                {
                    var playerMovement = FindFirstObjectByType<Unbreathe.Physics.PlayerMovementController>();
                    if (playerMovement != null)
                    {
                        target = playerMovement.transform;
                    }
                }
            }

            Vector3 angles = transform.eulerAngles;
            targetYaw = angles.y;
            targetPitch = angles.x;
            yaw = targetYaw;
            pitch = targetPitch;

            if (target != null)
            {
                smoothedTargetPosition = target.position;
            }

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            // Permitir liberar el cursor con la tecla Escape
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleCursorLock();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleCursorLock();
            }
#else
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape)) ToggleCursorLock();
            }
            catch { }
#endif

            ReadMouseInput();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Suavizado orbital
            yaw = Mathf.Lerp(yaw, targetYaw, Time.deltaTime * rotationSmoothSpeed);
            pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * rotationSmoothSpeed);

            Quaternion currentRotation = Quaternion.Euler(pitch, yaw, 0f);

            // Suavizado de posición del objetivo
            Vector3 targetPivot = target.position + Vector3.up * heightOffset;
            smoothedTargetPosition = Vector3.SmoothDamp(
                smoothedTargetPosition,
                targetPivot,
                ref currentVelocity,
                positionSmoothTime
            );

            // Posición final de la cámara orbitando hacia atrás
            Vector3 desiredPosition = smoothedTargetPosition - (currentRotation * Vector3.forward * distance);

            transform.position = desiredPosition;
            transform.rotation = currentRotation;
        }

        private void ReadMouseInput()
        {
            if (!enableMouseOrbit || Cursor.lockState != CursorLockMode.Locked) return;

            float mouseX = 0f;
            float mouseY = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                mouseX = delta.x * 0.1f * mouseSensitivity;
                mouseY = delta.y * 0.1f * mouseSensitivity;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            mouseX = UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseY = UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity;
#else
            try
            {
                mouseX = UnityEngine.Input.GetAxis("Mouse X") * mouseSensitivity;
                mouseY = UnityEngine.Input.GetAxis("Mouse Y") * mouseSensitivity;
            }
            catch { }
#endif

            targetYaw += mouseX;
            targetPitch -= mouseY;
            targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);
        }

        private void ToggleCursorLock()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                smoothedTargetPosition = target.position;
            }
        }
    }
}
