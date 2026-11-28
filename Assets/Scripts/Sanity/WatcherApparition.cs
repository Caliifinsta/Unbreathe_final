using System.Collections;
using UnityEngine;

namespace Unbreathe.Sanity
{
    /// <summary>
    /// Comportamiento del maniquí observador en UNBREATHE.
    /// No persigue, no ataca ni inflige daño; simplemente rota sobre el eje Y
    /// para mantener su mirada fija en el jugador en todo momento mientras
    /// ejecuta sus animaciones inquietantes.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class WatcherApparition : MonoBehaviour
    {
        [Header("Objetivo de Mirada")]
        [Tooltip("Transform del Player a observar.")]
        [SerializeField] private Transform targetPlayer;

        [Header("Cinemática de Rotación")]
        [Tooltip("Velocidad de rotación suave en el eje Y hacia el jugador.")]
        [SerializeField] private float rotationSpeed = 4.0f;

        [Header("Aparición y Desaparición")]
        [Tooltip("Duración de la transición de escala al aparecer y desaparecer.")]
        [SerializeField] private float transitionDuration = 0.8f;

        private Animator cachedAnimator;
        private Vector3 targetLocalScale = Vector3.one;
        private bool isDismissing = false;
        private Coroutine transitionCoroutine;

        private void Awake()
        {
            // Cachear componentes según las reglas de AGENTS.md
            cachedAnimator = GetComponent<Animator>();
            if (transform.localScale != Vector3.zero)
            {
                targetLocalScale = transform.localScale;
            }
            else
            {
                targetLocalScale = Vector3.one;
            }

            // Iniciar en escala cero para la entrada gradual
            transform.localScale = Vector3.zero;
        }

        private void Start()
        {
            if (targetPlayer == null)
            {
                var playerObj = GameObject.FindWithTag("Player");
                if (playerObj == null) playerObj = GameObject.Find("Player");
                if (playerObj != null) targetPlayer = playerObj.transform;
            }

            if (targetLocalScale == Vector3.zero)
            {
                targetLocalScale = Vector3.one;
            }

            if (cachedAnimator == null) cachedAnimator = GetComponent<Animator>();
            if (cachedAnimator != null && !cachedAnimator.enabled)
            {
                cachedAnimator.enabled = true;
            }

            // Iniciar rotando de inmediato hacia el jugador si ya está asignado
            if (targetPlayer != null)
            {
                Vector3 direction = targetPlayer.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction.normalized);
                }
            }

            // Entrada suave
            transitionCoroutine = StartCoroutine(ScaleRoutine(Vector3.zero, targetLocalScale, null));
        }

        /// <summary>
        /// Configura el objetivo al momento de la instanciación.
        /// </summary>
        public void Initialize(Transform target)
        {
            targetPlayer = target;
            if (targetPlayer != null)
            {
                Vector3 direction = targetPlayer.position - transform.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction.normalized);
                }
            }
        }

        private void Update()
        {
            RotateTowardsTarget();
        }

        /// <summary>
        /// Rota suavemente en el plano horizontal (eje Y) para mantener la mirada en el jugador.
        /// </summary>
        private void RotateTowardsTarget()
        {
            if (targetPlayer == null) return;

            Vector3 direction = targetPlayer.position - transform.position;
            direction.y = 0f; // Restricción estricta al eje horizontal

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Hace que el maniquí se desvanezca suavemente encogiéndose antes de autodestruirse.
        /// </summary>
        public void Dismiss()
        {
            if (isDismissing) return;
            isDismissing = true;

            if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
            StartCoroutine(ScaleRoutine(transform.localScale, Vector3.zero, () =>
            {
                Destroy(gameObject);
            }));
        }

        private IEnumerator ScaleRoutine(Vector3 start, Vector3 end, System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
                transform.localScale = Vector3.Lerp(start, end, t);
                yield return null;
            }

            transform.localScale = end;
            onComplete?.Invoke();
        }

        public bool IsDismissing => isDismissing;
    }
}
