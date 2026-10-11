using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unbreathe.Interaction
{
    /// <summary>
    /// Componente de puerta que permite al jugador transicionar entre escenas al presionar la tecla 'E'.
    /// Cumple las directrices de AGENTS.md: audio diegético, arquitectura limpia y sin sobrecarga en Update().
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneTransitionDoor : MonoBehaviour, IInteractable
    {
        [Header("Configuración de Destino")]
        [Tooltip("Nombre de la escena a la que se transportará al jugador.")]
        [SerializeField] private string targetSceneName = "Segundo plano";

        [Tooltip("Texto descriptivo que aparece en pantalla cuando el jugador se acerca.")]
        [SerializeField] private string promptText = "[E] Entrar al edificio";

        [Header("Audio Diegético")]
        [Tooltip("Sonido de apertura de puerta o pomo al interactuar.")]
        [SerializeField] private AudioClip doorSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Efecto de Transición")]
        [Tooltip("Tiempo de espera para permitir escuchar el sonido de la puerta antes de cargar.")]
        [SerializeField] private float transitionDelay = 0.35f;

        private bool isTransitioning = false;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Asegurar que el collider sea un Trigger para detección física
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        public string GetPromptText()
        {
            return promptText;
        }

        public bool CanInteract()
        {
            return !isTransitioning;
        }

        public void Interact(GameObject interactor)
        {
            if (isTransitioning) return;

            isTransitioning = true;
            Debug.Log($"<color=cyan>[SceneTransitionDoor] Jugador interactuó con la puerta. Cargando '{targetSceneName}'...</color>");

            // Reproducir sonido diegético si está asignado
            if (doorSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(doorSound);
                }
                else
                {
                    AudioSource.PlayClipAtPoint(doorSound, transform.position);
                }
            }

            StartCoroutine(PerformTransition());
        }

        private IEnumerator PerformTransition()
        {
            if (transitionDelay > 0f)
            {
                yield return new WaitForSeconds(transitionDelay);
            }

            if (!string.IsNullOrEmpty(targetSceneName))
            {
                SceneManager.LoadScene(targetSceneName);
            }
            else
            {
                Debug.LogError("[SceneTransitionDoor] targetSceneName está vacío. No se puede cargar la escena.");
                isTransitioning = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, 1.5f);
            }
        }
    }
}
