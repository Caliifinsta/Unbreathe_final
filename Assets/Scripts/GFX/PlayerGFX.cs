using UnityEngine;
using Unbreathe.Physics;

namespace Unbreathe.GFX
{
    /// <summary>
    /// Componente GFX para Karol con soporte completo para 4 direcciones (Front, Back, Left, Right).
    /// Maneja la reproducción de secuencias de frames según el vector de movimiento 3D relativo a la cámara.
    /// Respeta estrictamente las directrices de rendimiento de AGENTS.md (cero GetComponent en Update).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerGFX : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private PlayerMovementController movementController;

        [Header("Secuencias de Animación (4 Direcciones)")]
        [Tooltip("Caminar hacia el frente / abajo (hacia la cámara).")]
        [SerializeField] private Sprite[] walkDownFrames;

        [Tooltip("Caminar hacia atrás / arriba (alejándose de la cámara).")]
        [SerializeField] private Sprite[] walkUpFrames;

        [Tooltip("Caminar hacia la izquierda.")]
        [SerializeField] private Sprite[] walkLeftFrames;

        [Tooltip("Caminar hacia la derecha.")]
        [SerializeField] private Sprite[] walkRightFrames;

        [Header("Configuración de Reproducción")]
        [Tooltip("Velocidad de reproducción de la animación (frames por segundo).")]
        [SerializeField] private float animationFps = 14f;

        [Tooltip("Umbral de velocidad horizontal para considerar que el personaje se mueve.")]
        [SerializeField] private float movementThreshold = 0.05f;

        [Header("Efectos Diegéticos")]
        [SerializeField] private ParticleSystem movementParticles;

        private enum FacingDirection { Down, Up, Left, Right }
        private FacingDirection currentDirection = FacingDirection.Down;

        private float animationTimer = 0f;
        private int currentFrameIndex = 0;
        private Camera cachedCamera;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (movementController == null)
            {
                movementController = GetComponentInParent<PlayerMovementController>();
            }

            cachedCamera = Camera.main;

            // Mostrar frame inicial
            SetInitialSprite();
        }

        private void Start()
        {
            if (cachedCamera == null)
            {
                cachedCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (movementController == null || spriteRenderer == null) return;

            Vector3 velocity = movementController.HorizontalVelocity;
            float speed = velocity.magnitude;
            bool isMoving = speed > movementThreshold;

            if (isMoving)
            {
                DetermineFacingDirection(velocity);
            }

            UpdateSpriteAnimation(isMoving);

            // Control de partículas
            if (movementParticles != null)
            {
                if (isMoving && !movementParticles.isPlaying) movementParticles.Play();
                else if (!isMoving && movementParticles.isPlaying) movementParticles.Stop();
            }
        }

        private void DetermineFacingDirection(Vector3 velocity)
        {
            if (cachedCamera == null) cachedCamera = Camera.main;

            Vector3 moveDir = velocity.normalized;

            if (cachedCamera != null)
            {
                // Calcular la dirección relativa al plano de la cámara
                Vector3 camForward = cachedCamera.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = cachedCamera.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                float forwardDot = Vector3.Dot(moveDir, camForward);
                float rightDot = Vector3.Dot(moveDir, camRight);

                // Determinar el eje dominante
                if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
                {
                    currentDirection = forwardDot > 0f ? FacingDirection.Up : FacingDirection.Down;
                }
                else
                {
                    currentDirection = rightDot > 0f ? FacingDirection.Right : FacingDirection.Left;
                }
            }
            else
            {
                // En caso de no haber cámara, basarse en los ejes del mundo
                if (Mathf.Abs(moveDir.z) > Mathf.Abs(moveDir.x))
                {
                    currentDirection = moveDir.z > 0f ? FacingDirection.Up : FacingDirection.Down;
                }
                else
                {
                    currentDirection = moveDir.x > 0f ? FacingDirection.Right : FacingDirection.Left;
                }
            }
        }

        private void UpdateSpriteAnimation(bool isMoving)
        {
            Sprite[] activeSequence = GetCurrentSequence();
            if (activeSequence == null || activeSequence.Length == 0) return;

            if (isMoving)
            {
                animationTimer += Time.deltaTime;
                if (animationTimer >= (1f / animationFps))
                {
                    animationTimer = 0f;
                    currentFrameIndex = (currentFrameIndex + 1) % activeSequence.Length;
                    spriteRenderer.sprite = activeSequence[currentFrameIndex];
                }
            }
            else
            {
                // En reposo (Idle)
                animationTimer = 0f;
                currentFrameIndex = 0;
                spriteRenderer.sprite = activeSequence[0];
            }
        }

        private Sprite[] GetCurrentSequence()
        {
            switch (currentDirection)
            {
                case FacingDirection.Up:
                    return walkUpFrames != null && walkUpFrames.Length > 0 ? walkUpFrames : walkDownFrames;
                case FacingDirection.Left:
                    return walkLeftFrames != null && walkLeftFrames.Length > 0 ? walkLeftFrames : walkDownFrames;
                case FacingDirection.Right:
                    return walkRightFrames != null && walkRightFrames.Length > 0 ? walkRightFrames : walkDownFrames;
                case FacingDirection.Down:
                default:
                    return walkDownFrames;
            }
        }

        private void SetInitialSprite()
        {
            if (spriteRenderer != null && walkDownFrames != null && walkDownFrames.Length > 0)
            {
                spriteRenderer.sprite = walkDownFrames[0];
            }
        }

        public void SetAllAnimationFrames(Sprite[] down, Sprite[] up, Sprite[] left, Sprite[] right)
        {
            walkDownFrames = down;
            walkUpFrames = up;
            walkLeftFrames = left;
            walkRightFrames = right;
            SetInitialSprite();
        }
    }
}
