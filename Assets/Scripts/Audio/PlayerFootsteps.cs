using UnityEngine;
using Unbreathe.Physics;

namespace Unbreathe.Audio
{
    /// <summary>
    /// Sistema de audio diegético de pasos para Karol en UNBREATHE.
    /// Cumple las directrices de AGENTS.md:
    /// - Almacena referencias en caché en Awake().
    /// - Prohibido GetComponent() o Find() en Update().
    /// - Soporta clips de audio personalizados y genera un sonido sutil procedimental si no hay clips asignados.
    /// </summary>
    [RequireComponent(typeof(PlayerMovementController))]
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Efectos de Sonido")]
        [Tooltip("Lista de clips de audio para los pasos. Si está vacía, se generará un sonido diegético procedural.")]
        [SerializeField] private AudioClip[] footstepClips;

        [Header("Configuración de Pasos")]
        [Tooltip("Intervalo de tiempo entre cada paso al caminar.")]
        [SerializeField] private float stepInterval = 0.45f;

        [Range(0.05f, 1f)]
        [SerializeField] private float stepVolume = 0.35f;

        [Tooltip("Variación de tono aleatoria para mayor realismo y evitar repetición mecánica.")]
        [SerializeField] private float pitchVariation = 0.1f;

        // Referencias cacheadas en Awake()
        private PlayerMovementController movementController;
        private AudioSource audioSource;

        private float stepTimer = 0f;
        private AudioClip proceduralStepClip;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            movementController = GetComponent<PlayerMovementController>();
            audioSource = GetComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.3f; // Audio semi-posicional diegético

            // Generar clip sutil de respaldo procedimental en memoria por si no hay clips asignados
            GenerateProceduralStepClip();
        }

        private void Update()
        {
            if (movementController == null || audioSource == null) return;

            // Verificar si el personaje está en movimiento en el suelo
            if (movementController.IsMoving && movementController.IsGrounded)
            {
                stepTimer += Time.deltaTime;

                if (stepTimer >= stepInterval)
                {
                    PlayFootstep();
                    stepTimer = 0f;
                }
            }
            else
            {
                // Resetear temporizador para que el primer paso suene apenas empiece a caminar
                stepTimer = stepInterval * 0.7f;
            }
        }

        private void PlayFootstep()
        {
            AudioClip clipToPlay = null;

            if (footstepClips != null && footstepClips.Length > 0)
            {
                int index = Random.Range(0, footstepClips.Length);
                clipToPlay = footstepClips[index];
            }
            else
            {
                clipToPlay = proceduralStepClip;
            }

            if (clipToPlay != null)
            {
                audioSource.pitch = 1.0f + Random.Range(-pitchVariation, pitchVariation);
                audioSource.PlayOneShot(clipToPlay, stepVolume * Random.Range(0.85f, 1.05f));
            }
        }

        private void GenerateProceduralStepClip()
        {
            // Sonido de paso sutil (impacto sordo de baja frecuencia tipo asfalto/madera)
            int sampleRate = 44100;
            float duration = 0.08f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float envelope = Mathf.Exp(-t * 28f);
                float wave = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.4f;
                float noise = (Random.value * 2f - 1f) * 0.15f;
                samples[i] = (wave + noise) * envelope;
            }

            proceduralStepClip = AudioClip.Create("ProceduralFootstep", totalSamples, 1, sampleRate, false);
            proceduralStepClip.SetData(samples, 0);
        }
    }
}
