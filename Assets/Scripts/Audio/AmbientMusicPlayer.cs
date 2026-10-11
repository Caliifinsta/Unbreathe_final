using System.Collections;
using UnityEngine;

namespace Unbreathe.Audio
{
    /// <summary>
    /// Reproductor de música ambiental atmosférica para las escenas de UNBREATHE.
    /// Realiza fade-in suave y reproducción continua en bucle.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public class AmbientMusicPlayer : MonoBehaviour
    {
        [Header("Pista de Audio")]
        [Tooltip("Música de fondo o atmósfera de la escena.")]
        [SerializeField] private AudioClip ambientTrack;

        [Header("Control de Volumen")]
        [Range(0f, 1f)]
        [SerializeField] private float targetVolume = 0.3f;

        [Tooltip("Duración en segundos de la transición suave al iniciar la escena.")]
        [SerializeField] private float fadeInDuration = 2.0f;

        private AudioSource audioSource;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Audio 2D envolvente
            audioSource.volume = 0f;

            if (ambientTrack != null)
            {
                audioSource.clip = ambientTrack;
            }
        }

        private void Start()
        {
            if (audioSource.clip != null)
            {
                audioSource.Play();
                StartCoroutine(FadeInMusic());
            }
        }

        private IEnumerator FadeInMusic()
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
                yield return null;
            }
            audioSource.volume = targetVolume;
        }

        public void SetVolume(float newVolume)
        {
            targetVolume = Mathf.Clamp01(newVolume);
            if (audioSource != null)
            {
                audioSource.volume = targetVolume;
            }
        }
    }
}
