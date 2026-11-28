using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unbreathe.Interaction;

namespace Unbreathe.UI
{
    /// <summary>
    /// Fundido a negro entre escenas de UNBREATHE.
    /// - Vive en un Canvas de pantalla completa que sobrevive entre escenas (DontDestroyOnLoad)
    ///   y queda por encima de todas las demás interfaces (pausa, aviso, diálogos...).
    /// - Cada vez que carga una escena (la inicie quien la inicie) la pantalla arranca en negro,
    ///   espera un instante a que la escena se asiente y hace fade in.
    /// - ScreenFader.CargarEscena("nombre"): fade out, carga la escena y el fade in lo hace solo.
    /// - Sin editar SceneTransitionDoor: vigila las puertas de la escena y, cuando una pasa a
    ///   "en transición" (su CanInteract() se vuelve false), empieza el fade out mientras la puerta
    ///   espera su retardo para cargar.
    /// - Usa tiempo sin escala: funciona igual con el menú de pausa (Time.timeScale = 0).
    /// - Bloquea clics mientras la pantalla no es completamente transparente.
    /// - Opcional: baja y sube el volumen general (AudioListener.volume) junto con el fundido.
    /// Se crea solo con ScreenFaderBootstrap desde Resources/ScreenFader.prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScreenFader : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("CanvasGroup del Canvas negro. Se autovincula si queda vacío.")]
        [SerializeField] private CanvasGroup grupo;

        [Header("Tiempos (segundos, no dependen de Time.timeScale)")]
        [Tooltip("Cuánto tarda la pantalla en ponerse negra al salir de una escena. Debe ser menor que el retardo de las puertas (0.35 s).")]
        [SerializeField] private float duracionFadeOut = 0.3f;

        [Tooltip("Tiempo que se queda en negro al entrar a una escena, para tapar el tirón de la carga.")]
        [SerializeField] private float duracionNegro = 0.15f;

        [Tooltip("Cuánto tarda la escena nueva en aparecer desde el negro.")]
        [SerializeField] private float duracionFadeIn = 0.5f;

        [Header("Curva")]
        [Tooltip("Suavizado del fundido (0 = inicio, 1 = final). Por defecto: entrada y salida suaves.")]
        [SerializeField] private AnimationCurve curva = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Audio")]
        [Tooltip("Baja el volumen general al salir y lo sube al entrar (usa AudioListener.volume).")]
        [SerializeField] private bool fundirVolumen = true;

        [Header("Seguridad")]
        [Tooltip("Si una puerta empieza a transicionar pero la escena no carga en este tiempo, se cancela el fundido.")]
        [SerializeField] private float tiempoMaximoEsperaPuerta = 8f;

        // Un frame largo (por ejemplo, el de la carga) no debe "comerse" el fundido.
        private const float MaxDeltaFade = 0.05f;

        public static ScreenFader Instancia { get; private set; }

        /// <summary>True mientras la pantalla se está fundiendo (salida o entrada).</summary>
        public static bool EstaTransicionando => Instancia != null && Instancia.ocupado;

        private bool ocupado;
        private Coroutine rutina;
        private bool volumenReducido;
        private float volumenMaestro = 1f;
        private readonly List<SceneTransitionDoor> puertas = new List<SceneTransitionDoor>();

        // ------------------------------------------------------------------
        // API pública
        // ------------------------------------------------------------------

        /// <summary>
        /// Fade out, carga la escena y el fade in lo hace solo al terminar la carga.
        /// Devuelve false si no pudo encargarse (no hay ScreenFader, ya hay una transición en curso
        /// o la escena no existe); en ese caso quien llama debe cargar la escena por su cuenta.
        /// </summary>
        public static bool CargarEscena(string nombreEscena)
        {
            if (Instancia == null || Instancia.grupo == null) return false;
            return Instancia.IniciarCarga(nombreEscena);
        }

        // ------------------------------------------------------------------
        // Ciclo de vida
        // ------------------------------------------------------------------

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
            DontDestroyOnLoad(gameObject);

            if (grupo == null)
            {
                grupo = GetComponent<CanvasGroup>();
            }

            // La aplicación arranca en negro; el fade in llega con la primera carga de escena.
            AplicarAlpha(1f);
            SceneManager.sceneLoaded += AlCargarEscena;
        }

        private void Start()
        {
            // Respaldo: si por algún motivo no hubo evento de carga, no dejar la pantalla en negro.
            if (rutina == null && grupo != null && grupo.alpha > 0f)
            {
                IniciarRutina(RutinaFadeIn(duracionNegro));
            }
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;

            if (Instancia == this)
            {
                Instancia = null;
                RestaurarVolumen();
            }
        }

        private void Update()
        {
            if (ocupado) return;

            // Detecta puertas que empezaron a transicionar (sin tocar SceneTransitionDoor).
            for (int i = 0; i < puertas.Count; i++)
            {
                SceneTransitionDoor puerta = puertas[i];
                if (puerta != null && !puerta.CanInteract())
                {
                    IniciarRutina(RutinaSalidaPorPuerta());
                    return;
                }
            }
        }

        // ------------------------------------------------------------------
        // Eventos de escena
        // ------------------------------------------------------------------

        private void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            if (grupo == null) return;

            RegistrarPuertas();

            // La escena nueva siempre arranca en negro, sin importar quién la cargó.
            AplicarAlpha(1f);

            // La escena nueva pudo ajustar el volumen en su Awake; el fade in lo sube desde 0.
            if (volumenReducido && fundirVolumen)
            {
                AudioListener.volume = 0f;
            }

            IniciarRutina(RutinaFadeIn(duracionNegro));
        }

        private void RegistrarPuertas()
        {
            puertas.Clear();
            puertas.AddRange(FindObjectsByType<SceneTransitionDoor>(FindObjectsSortMode.None));
        }

        // ------------------------------------------------------------------
        // Carga propia (ScreenFader.CargarEscena)
        // ------------------------------------------------------------------

        private bool IniciarCarga(string nombreEscena)
        {
            if (string.IsNullOrEmpty(nombreEscena))
            {
                nombreEscena = SceneManager.GetActiveScene().name;
            }

            if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
            {
                Debug.LogError("[ScreenFader] La escena '" + nombreEscena + "' no está en Build Settings.");
                return false;
            }

            if (ocupado) return false;

            IniciarRutina(RutinaCarga(nombreEscena));
            return true;
        }

        private IEnumerator RutinaCarga(string nombreEscena)
        {
            IEnumerator salida = RutinaFadeOut();
            while (salida.MoveNext())
            {
                yield return salida.Current;
            }

            // El fade in lo inicia AlCargarEscena cuando la escena nueva está lista.
            SceneManager.LoadScene(nombreEscena);
        }

        // ------------------------------------------------------------------
        // Salida iniciada por una puerta
        // ------------------------------------------------------------------

        private IEnumerator RutinaSalidaPorPuerta()
        {
            IEnumerator salida = RutinaFadeOut();
            while (salida.MoveNext())
            {
                yield return salida.Current;
            }

            // La puerta carga la escena tras su retardo; AlCargarEscena detiene esta rutina.
            float espera = 0f;
            while (espera < tiempoMaximoEsperaPuerta)
            {
                if (!HayPuertaEnTransicion())
                {
                    // La puerta no llegó a cargar nada (por ejemplo, destino vacío): mostrar la escena otra vez.
                    IEnumerator recuperar = RutinaFadeIn(0f);
                    while (recuperar.MoveNext())
                    {
                        yield return recuperar.Current;
                    }

                    yield break;
                }

                espera += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.LogWarning("[ScreenFader] La puerta no cargó la escena a tiempo; se cancela el fundido.");

            IEnumerator cancelar = RutinaFadeIn(0f);
            while (cancelar.MoveNext())
            {
                yield return cancelar.Current;
            }
        }

        private bool HayPuertaEnTransicion()
        {
            for (int i = 0; i < puertas.Count; i++)
            {
                SceneTransitionDoor puerta = puertas[i];
                if (puerta != null && !puerta.CanInteract())
                {
                    return true;
                }
            }

            return false;
        }

        // ------------------------------------------------------------------
        // Fundidos
        // ------------------------------------------------------------------

        private IEnumerator RutinaFadeOut()
        {
            ocupado = true;

            if (fundirVolumen && !volumenReducido)
            {
                volumenMaestro = AudioListener.volume;
                volumenReducido = true;
            }

            float alphaInicial = grupo.alpha;
            float duracion = Mathf.Max(0.01f, duracionFadeOut);
            float t = 0f;

            while (t < 1f)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, MaxDeltaFade) / duracion;
                float k = curva.Evaluate(Mathf.Clamp01(t));

                AplicarAlpha(Mathf.Lerp(alphaInicial, 1f, k));
                if (volumenReducido && fundirVolumen)
                {
                    AudioListener.volume = Mathf.Lerp(volumenMaestro, 0f, k);
                }

                yield return null;
            }

            AplicarAlpha(1f);
            if (volumenReducido && fundirVolumen)
            {
                AudioListener.volume = 0f;
            }
        }

        private IEnumerator RutinaFadeIn(float retencion)
        {
            ocupado = true;
            AplicarAlpha(1f);

            if (retencion > 0f)
            {
                yield return new WaitForSecondsRealtime(retencion);
            }

            float duracion = Mathf.Max(0.01f, duracionFadeIn);
            float t = 0f;

            while (t < 1f)
            {
                t += Mathf.Min(Time.unscaledDeltaTime, MaxDeltaFade) / duracion;
                float k = curva.Evaluate(Mathf.Clamp01(t));

                AplicarAlpha(1f - k);
                if (volumenReducido && fundirVolumen)
                {
                    AudioListener.volume = Mathf.Lerp(0f, volumenMaestro, k);
                }

                yield return null;
            }

            AplicarAlpha(0f);
            RestaurarVolumen();

            ocupado = false;
            rutina = null;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        private void IniciarRutina(IEnumerator nueva)
        {
            if (rutina != null)
            {
                StopCoroutine(rutina);
            }

            rutina = StartCoroutine(nueva);
        }

        private void AplicarAlpha(float alpha)
        {
            if (grupo == null) return;

            grupo.alpha = alpha;
            grupo.blocksRaycasts = alpha > 0.001f; // en negro (o a medio fundir) no se puede hacer clic detrás
        }

        private void RestaurarVolumen()
        {
            if (volumenReducido)
            {
                AudioListener.volume = volumenMaestro;
                volumenReducido = false;
            }
        }
    }
}
