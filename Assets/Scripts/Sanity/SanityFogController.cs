using UnityEngine;

namespace Unbreathe.Sanity
{
    /// <summary>
    /// Controla la niebla atmosférica en base a la cordura del jugador.
    /// A menor cordura, la densidad de la niebla aumenta gradualmente cerrando la visión
    /// con un tono gris frío oscuro para intensificar la atmósfera claustrofóbica.
    /// </summary>
    public class SanityFogController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private SanityManager sanityManager;

        [Header("Configuración de Niebla")]
        [Tooltip("Activar el sistema de niebla volumétrica/distancia en RenderSettings.")]
        [SerializeField] private bool manageFog = true;

        [Tooltip("Modo de atenuación de la niebla (ExponentialSquared recomendado).")]
        [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;

        [Tooltip("Color de la niebla (gris frío oscuro para noche psicológica).")]
        [SerializeField] private Color fogColor = new Color(0.12f, 0.14f, 0.18f, 1.0f);

        [Header("Densidad Dinámica")]
        [Tooltip("Densidad mínima de la niebla con cordura alta (100).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float minDensity = 0.005f;

        [Tooltip("Densidad máxima de la niebla con cordura crítica (0 - 30).")]
        [Range(0.02f, 0.2f)]
        [SerializeField] private float maxDensity = 0.08f;

        [Tooltip("Velocidad de transición suave de la densidad.")]
        [SerializeField] private float transitionSpeed = 1.5f;

        private float currentDensity;

        private void Awake()
        {
            if (sanityManager == null)
            {
                sanityManager = GetComponent<SanityManager>();
                if (sanityManager == null) sanityManager = SanityManager.Instance;
            }

            currentDensity = minDensity;
        }

        private void Start()
        {
            ApplyInitialFogSettings();
        }

        private void ApplyInitialFogSettings()
        {
            if (!manageFog) return;

            RenderSettings.fog = true;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = currentDensity;
        }

        private void Update()
        {
            if (!manageFog) return;

            if (sanityManager == null)
            {
                sanityManager = SanityManager.Instance;
                if (sanityManager == null) return;
            }

            // Calcular densidad objetivo basada en la cordura normalizada (0 = crítico, 1 = lúcido)
            float sanityNorm = sanityManager.NormalizedSanity;

            // Curva no lineal para que a partir de 70-50 empiece a cerrarse fuertemente
            // (1 - sanityNorm) sube de 0 a 1 conforme baja la cordura
            float distress = 1f - sanityNorm;
            float targetDensity = Mathf.Lerp(minDensity, maxDensity, Mathf.Pow(distress, 1.2f));

            currentDensity = Mathf.MoveTowards(currentDensity, targetDensity, transitionSpeed * Time.deltaTime);

            RenderSettings.fog = true;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = currentDensity;
        }

        private void OnDisable()
        {
            // Dejar niebla en estado base si se desactiva
            if (manageFog)
            {
                RenderSettings.fogDensity = minDensity;
            }
        }
    }
}
