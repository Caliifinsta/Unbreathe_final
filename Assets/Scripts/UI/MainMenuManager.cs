using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.UI
{
    /// <summary>
    /// Controlador del Menú Principal de UNBREATHE.
    /// Maneja:
    /// 1. Visualización inicial limpia del fondo (Menu.jpeg) durante unos segundos.
    /// 2. Entrada suave (fade-in) de los botones principales.
    /// 3. Música atmosférica de inicio con AudioSource.
    /// 4. Transición a la pantalla de prólogo (InicioPlay.jpeg) al pulsar Play.
    /// 5. Carga de la escena jugable 'Primer Plano'.
    /// 6. Panel de opciones (volumen general y pantalla completa).
    /// Cumple estrictamente con las directrices de rendimiento de AGENTS.md.
    /// </summary>
    [DisallowMultipleComponent]
    public class MainMenuManager : MonoBehaviour
    {
        [Header("Imágenes de Fondo")]
        [SerializeField] private Image menuBackgroundImage;
        [SerializeField] private GameObject introPlayPanel;
        [SerializeField] private Image introPlayImage;

        [Header("Contenedor de Botones")]
        [Tooltip("CanvasGroup que contiene los botones para animar su aparición.")]
        [SerializeField] private CanvasGroup buttonsCanvasGroup;

        [Header("Tiempos de Secuencia")]
        [Tooltip("Segundos que permanece solo la imagen de fondo antes de mostrar los botones.")]
        [SerializeField] private float initialWaitTime = 2.5f;

        [Tooltip("Velocidad de aparición (fade-in) de los botones.")]
        [SerializeField] private float buttonsFadeSpeed = 1.5f;

        [Tooltip("Tiempo que se muestra la imagen de InicioPlay antes de cargar la escena.")]
        [SerializeField] private float introPlayDuration = 3.5f;

        [Header("Escena del Juego")]
        [Tooltip("Nombre de la escena a cargar al pulsar Play.")]
        [SerializeField] private string gameplaySceneName = "Primer Plano";

        [Header("Panel de Opciones")]
        [SerializeField] private GameObject optionsPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TextMeshProUGUI volumePercentText;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Audio")]
        [SerializeField] private AudioSource menuAudioSource;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioSource sfxAudioSource;

        private bool isStartingGame = false;

        private void Awake()
        {
            // Regla AGENTS.md: Almacenar referencias en caché en Awake()
            if (menuAudioSource == null)
            {
                menuAudioSource = GetComponent<AudioSource>();
            }

            // Inicializar estados visuales
            if (buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.alpha = 0f;
                buttonsCanvasGroup.interactable = false;
                buttonsCanvasGroup.blocksRaycasts = false;
            }

            if (introPlayPanel != null)
            {
                introPlayPanel.SetActive(false);
            }

            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }

            // Inicializar y cargar volumen guardado
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            AudioListener.volume = savedVolume;

            if (volumeSlider != null)
            {
                volumeSlider.value = savedVolume;
                volumeSlider.onValueChanged.AddListener(SetMasterVolume);
            }

            UpdateVolumeUI(savedVolume);

            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
                fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
            }
        }

        private void Start()
        {
            // Iniciar la secuencia de aparición de los botones tras unos segundos
            StartCoroutine(ShowButtonsRoutine());
        }

        private IEnumerator ShowButtonsRoutine()
        {
            // 1. Mostrar solo la imagen de fondo durante unos segundos
            yield return new WaitForSeconds(initialWaitTime);

            // 2. Transición suave de entrada para los botones
            if (buttonsCanvasGroup != null)
            {
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * buttonsFadeSpeed;
                    buttonsCanvasGroup.alpha = Mathf.Clamp01(t);
                    yield return null;
                }

                buttonsCanvasGroup.alpha = 1f;
                buttonsCanvasGroup.interactable = true;
                buttonsCanvasGroup.blocksRaycasts = true;
            }
        }

        #region Botones Principales

        /// <summary>
        /// Acción del botón JUGAR (Play):
        /// Muestra la imagen de InicioPlay y luego carga la escena Primer Plano.
        /// </summary>
        public void OnPlayClicked()
        {
            if (isStartingGame) return;
            isStartingGame = true;

            PlaySfx(buttonClickSound);
            StartCoroutine(PlayTransitionRoutine());
        }

        private IEnumerator PlayTransitionRoutine()
        {
            // Ocultar botones
            if (buttonsCanvasGroup != null)
            {
                buttonsCanvasGroup.interactable = false;
                buttonsCanvasGroup.blocksRaycasts = false;
                float t = 1f;
                while (t > 0f)
                {
                    t -= Time.deltaTime * 3f;
                    buttonsCanvasGroup.alpha = Mathf.Clamp01(t);
                    yield return null;
                }
            }

            // Mostrar imagen de InicioPlay
            if (introPlayPanel != null)
            {
                introPlayPanel.SetActive(true);

                // Fade in de la imagen de inicio
                CanvasGroup introCg = introPlayPanel.GetComponent<CanvasGroup>();
                if (introCg != null)
                {
                    introCg.alpha = 0f;
                    float t = 0f;
                    while (t < 1f)
                    {
                        t += Time.deltaTime * 2f;
                        introCg.alpha = Mathf.Clamp01(t);
                        yield return null;
                    }
                }

                // Esperar la duración del prólogo o hasta que el usuario pulse cualquier tecla
                float timer = 0f;
                while (timer < introPlayDuration)
                {
                    timer += Time.deltaTime;

                    bool anyKeyPressed = false;
#if ENABLE_INPUT_SYSTEM
                    if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) anyKeyPressed = true;
                    if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) anyKeyPressed = true;
#elif ENABLE_LEGACY_INPUT_MANAGER
                    anyKeyPressed = UnityEngine.Input.anyKeyDown;
#else
                    try
                    {
                        anyKeyPressed = UnityEngine.Input.anyKeyDown;
                    }
                    catch { }
#endif

                    if (anyKeyPressed && timer > 0.8f)
                    {
                        break;
                    }
                    yield return null;
                }

                // Desvanecer música antes de cambiar de escena
                if (menuAudioSource != null)
                {
                    float startVol = menuAudioSource.volume;
                    float fadeT = 0f;
                    while (fadeT < 1f)
                    {
                        fadeT += Time.deltaTime * 2f;
                        menuAudioSource.volume = Mathf.Lerp(startVol, 0f, fadeT);
                        yield return null;
                    }
                }
            }

            // Cargar la escena del juego
            SceneManager.LoadScene(gameplaySceneName);
        }

        /// <summary>
        /// Abre el panel de opciones.
        /// </summary>
        public void OnOptionsClicked()
        {
            PlaySfx(buttonClickSound);
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Cierra el panel de opciones y vuelve al menú.
        /// </summary>
        public void OnCloseOptionsClicked()
        {
            PlaySfx(buttonClickSound);
            if (optionsPanel != null)
            {
                optionsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Cierra el juego.
        /// </summary>
        public void OnQuitClicked()
        {
            PlaySfx(buttonClickSound);
            Debug.Log("[MainMenu] Saliendo del juego...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Configuración y Sonido

        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("MasterVolume", volume);
            PlayerPrefs.Save();
            UpdateVolumeUI(volume);
        }

        private void UpdateVolumeUI(float volume)
        {
            if (volumePercentText != null)
            {
                volumePercentText.text = $"{Mathf.RoundToInt(volume * 100f)}%";
            }
        }

        public void SetFullscreen(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        public void OnButtonHover()
        {
            PlaySfx(buttonHoverSound);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (sfxAudioSource != null && clip != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
        }

        #endregion
    }
}
