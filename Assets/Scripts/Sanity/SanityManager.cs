using UnityEngine;
using Unbreathe.Dialogue;

namespace Unbreathe.Sanity
{
    /// <summary>
    /// Gestiona la cordura del protagonista en UNBREATHE.
    /// La cordura decrece paulatinamente con el tiempo para generar tensión psicológica
    /// y puede recuperarse mediante la interacción con objetos, recuerdos o notas.
    /// </summary>
    public class SanityManager : MonoBehaviour
    {
        public static SanityManager Instance { get; private set; }

        [Header("Configuración de Cordura")]
        [Tooltip("Cordura máxima del personaje.")]
        [SerializeField] private float maxSanity = 100f;

        [Tooltip("Cordura actual del personaje (0 - 100).")]
        [SerializeField] private float currentSanity = 100f;

        [Tooltip("Velocidad de pérdida de cordura por segundo.")]
        [SerializeField] private float decayRate = 0.4f;

        [Tooltip("¿Permitir que la cordura disminuya automáticamente con el tiempo?")]
        [SerializeField] private bool autoDecay = true;

        [Tooltip("Pausar el descenso de cordura durante secuencias de diálogo o notas.")]
        [SerializeField] private bool pauseDuringDialogue = true;

        // Eventos para suscriptores (UI, audio, generadores)
        public delegate void SanityChangedHandler(float current, float max);
        public event SanityChangedHandler OnSanityChanged;

        public float CurrentSanity => currentSanity;
        public float MaxSanity => maxSanity;
        public float NormalizedSanity => maxSanity > 0f ? Mathf.Clamp01(currentSanity / maxSanity) : 0f;
        public float DecayRate => decayRate;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            HandleSanityDecay();
        }

        private void HandleSanityDecay()
        {
            if (!autoDecay || decayRate <= 0f) return;

            // Regla AGENTS.md: Respetar la pausa de juego/diálogos
            if (pauseDuringDialogue && DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                return;
            }

            if (currentSanity > 0f)
            {
                currentSanity -= decayRate * Time.deltaTime;
                if (currentSanity < 0f) currentSanity = 0f;

                OnSanityChanged?.Invoke(currentSanity, maxSanity);
            }
        }

        /// <summary>
        /// Recupera una cantidad determinada de cordura (por ejemplo, al interactuar con recuerdos).
        /// </summary>
        /// <param name="amount">Puntos de cordura a recuperar.</param>
        public void RecoverSanity(float amount)
        {
            if (amount <= 0f) return;

            currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
            OnSanityChanged?.Invoke(currentSanity, maxSanity);
        }

        /// <summary>
        /// Reduce bruscamente la cordura ante eventos traumáticos o visiones.
        /// </summary>
        public void ReduceSanity(float amount)
        {
            if (amount <= 0f) return;

            currentSanity = Mathf.Clamp(currentSanity - amount, 0f, maxSanity);
            OnSanityChanged?.Invoke(currentSanity, maxSanity);
        }

        /// <summary>
        /// Modifica o restablece directamente el valor de la cordura.
        /// </summary>
        public void SetSanity(float value)
        {
            currentSanity = Mathf.Clamp(value, 0f, maxSanity);
            OnSanityChanged?.Invoke(currentSanity, maxSanity);
        }

        /// <summary>
        /// Pausa o reanuda la pérdida continua de cordura.
        /// </summary>
        public void SetAutoDecay(bool enabled)
        {
            autoDecay = enabled;
        }
    }
}
