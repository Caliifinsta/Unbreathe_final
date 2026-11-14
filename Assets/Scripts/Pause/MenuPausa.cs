using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unbreathe.Dialogue;
using Unbreathe.Input;

namespace Unbreathe.UI
{
    /// <summary>
    /// Menú de pausa de UNBREATHE.
    /// - Esc abre/cierra el menú (Input System nuevo).
    /// - Time.timeScale = 0 al pausar, y vuelve al valor previo al reanudar.
    /// - AudioListener.pause pausa música, pasos y voces (NO toca AudioListener.volume,
    ///   que usa el menú principal para el volumen maestro).
    /// - Muestra el cursor al pausar y lo deja como estaba al reanudar.
    /// - No pausa mientras hay un diálogo activo (solo LEE DialogueManager.IsDialogueActive).
    /// No modifica ningún script ni escena existente: se apoya en propiedades públicas
    /// (PlayerInputHandler.InputEnabled) y se instancia desde PauseMenuBootstrap.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuPausa : MonoBehaviour
    {
        [Header("Referencias de UI")]
        [SerializeField] private GameObject panelPausa;
        [SerializeField] private Button botonContinuar;
        [SerializeField] private Button botonReiniciar;
        [SerializeField] private Button botonMenuPrincipal;
        [SerializeField] private Button botonSalir;

        [Header("Escenas (deben estar en Build Settings)")]
        [Tooltip("Escena a la que vuelve el botón 'Menú principal'.")]
        [SerializeField] private string escenaMenuPrincipal = "MainMenu";

        [Tooltip("Escena que carga el botón 'Reiniciar'. Si queda vacío, recarga la escena actual.")]
        [SerializeField] private string escenaReinicio = "Primer Plano";

        [Header("Audio de botones")]
        [Tooltip("AudioSource propio del menú (ignora la pausa del AudioListener).")]
        [SerializeField] private AudioSource sfxAudioSource;

        [Tooltip("Sonido de clic de los botones. Opcional: si está vacío, no suena nada.")]
        [SerializeField] private AudioClip clicSonido;

        // Estado de la pausa
        private bool estaPausado;
        private bool cargandoEscena;
        private float escalaTiempoPrevia = 1f;

        // Referencias cacheadas en Start (regla AGENTS.md: nada de Find en Update)
        private PlayerInputHandler inputJugador;
        private bool inputJugadorPrevio = true;
        private CinemachineInputAxisController controladorCamara;
        private bool controladorCamaraPrevio = true;

        // Cursor: se guarda el estado del FRAME ANTERIOR porque PlayerController3D
        // libera el cursor con Esc en su propio Update (orden de ejecución no definido).
        private CursorLockMode ultimoLockState;
        private bool ultimoCursorVisible;
        private bool restaurarCursorPendiente;

        public bool EstaPausado => estaPausado;

        private void Awake()
        {
            if (panelPausa != null)
            {
                panelPausa.SetActive(false);
            }

            ConfigurarAudio();
            ConectarBotones();
            AsegurarEventSystem();

            ultimoLockState = Cursor.lockState;
            ultimoCursorVisible = Cursor.visible;
        }

        private void Start()
        {
            // Pueden ser null (por ejemplo, Tercer Plano no tiene Player ni Cinemachine).
            inputJugador = FindFirstObjectByType<PlayerInputHandler>();
            controladorCamara = FindFirstObjectByType<CinemachineInputAxisController>();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (estaPausado)
            {
                Reanudar();
            }
            else if (PuedePausar())
            {
                Pausar();
            }
        }

        private void LateUpdate()
        {
            // LateUpdate corre después de todos los Update, así que gana sobre PlayerController3D.
            if (estaPausado)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (restaurarCursorPendiente)
            {
                Cursor.lockState = ultimoLockState;
                Cursor.visible = ultimoCursorVisible;
                restaurarCursorPendiente = false;
                return;
            }

            ultimoLockState = Cursor.lockState;
            ultimoCursorVisible = Cursor.visible;
        }

        private void OnDestroy()
        {
            // Seguridad: si la escena se descarga estando en pausa, no dejar el juego congelado.
            if (estaPausado)
            {
                RestaurarEstadoGlobal();
            }
        }

        // ------------------------------------------------------------------
        // Pausa / reanudar
        // ------------------------------------------------------------------

        private bool PuedePausar()
        {
            if (cargandoEscena) return false;

            // Solo lectura: no se modifica el sistema de diálogos.
            DialogueManager dialogos = DialogueManager.Instance;
            return dialogos == null || !dialogos.IsDialogueActive;
        }

        public void Pausar()
        {
            if (estaPausado) return;
            estaPausado = true;

            escalaTiempoPrevia = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (inputJugador != null)
            {
                inputJugadorPrevio = inputJugador.InputEnabled;
                inputJugador.InputEnabled = false; // evita E / Espacio mientras está pausado
            }

            if (controladorCamara != null)
            {
                controladorCamaraPrevio = controladorCamara.enabled;
                controladorCamara.enabled = false; // la cámara no orbita en pausa
            }

            if (panelPausa != null)
            {
                panelPausa.SetActive(true);
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Reanudar()
        {
            if (!estaPausado) return;
            estaPausado = false;

            Time.timeScale = escalaTiempoPrevia;
            AudioListener.pause = false;

            if (panelPausa != null)
            {
                panelPausa.SetActive(false);
            }

            if (inputJugador != null)
            {
                inputJugador.InputEnabled = inputJugadorPrevio;
            }

            if (controladorCamara != null)
            {
                controladorCamara.enabled = controladorCamaraPrevio;
            }

            // El cursor se restaura en LateUpdate (ver comentario arriba).
            restaurarCursorPendiente = true;
        }

        // ------------------------------------------------------------------
        // Botones
        // ------------------------------------------------------------------

        private void ConectarBotones()
        {
            if (botonContinuar != null) botonContinuar.onClick.AddListener(OnContinuarClick);
            if (botonReiniciar != null) botonReiniciar.onClick.AddListener(OnReiniciarClick);
            if (botonMenuPrincipal != null) botonMenuPrincipal.onClick.AddListener(OnMenuPrincipalClick);
            if (botonSalir != null) botonSalir.onClick.AddListener(OnSalirClick);
        }

        private void OnContinuarClick()
        {
            ReproducirClic();
            Reanudar();
        }

        private void OnReiniciarClick()
        {
            ReproducirClic();
            CargarEscena(escenaReinicio);
        }

        private void OnMenuPrincipalClick()
        {
            ReproducirClic();
            CargarEscena(escenaMenuPrincipal);
        }

        private void OnSalirClick()
        {
            ReproducirClic();
            RestaurarEstadoGlobal();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void CargarEscena(string nombreEscena)
        {
            if (cargandoEscena) return;

            if (string.IsNullOrEmpty(nombreEscena))
            {
                nombreEscena = SceneManager.GetActiveScene().name;
            }

            if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
            {
                Debug.LogError($"[MenuPausa] La escena '{nombreEscena}' no está en Build Settings. Se queda en pausa.");
                return;
            }

            cargandoEscena = true;
            RestaurarEstadoGlobal(); // timeScale, audio y cursor son estáticos: sobreviven al cambio de escena
            SceneManager.LoadScene(nombreEscena);
        }

        /// <summary>
        /// Deja timeScale, audio y cursor en estado "limpio" antes de salir de la escena.
        /// El cursor queda libre y visible (lo necesita el MainMenu); si la escena destino
        /// tiene Player, PlayerController3D lo vuelve a bloquear en su Start().
        /// </summary>
        private void RestaurarEstadoGlobal()
        {
            estaPausado = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ------------------------------------------------------------------
        // Audio y EventSystem
        // ------------------------------------------------------------------

        private void ConfigurarAudio()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
            }

            if (sfxAudioSource != null)
            {
                sfxAudioSource.playOnAwake = false;
                sfxAudioSource.loop = false;
                sfxAudioSource.spatialBlend = 0f;
                sfxAudioSource.ignoreListenerPause = true; // el clic suena aunque el audio esté pausado
            }
        }

        private void ReproducirClic()
        {
            if (sfxAudioSource != null && clicSonido != null)
            {
                sfxAudioSource.PlayOneShot(clicSonido);
            }
        }

        /// <summary>
        /// Primer Plano, Segundo plano y Tercer Plano no tienen EventSystem; sin él los botones
        /// no reciben clics. Solo se crea uno si la escena no tiene.
        /// </summary>
        private void AsegurarEventSystem()
        {
            if (EventSystem.current != null || FindFirstObjectByType<EventSystem>() != null) return;

            GameObject go = new GameObject("EventSystem_Pausa", typeof(EventSystem));
            go.transform.SetParent(transform, false);

            InputSystemUIInputModule modulo = go.AddComponent<InputSystemUIInputModule>();
            modulo.AssignDefaultActions();
        }
    }
}
