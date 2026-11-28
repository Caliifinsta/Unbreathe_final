using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbreathe.Sanity;

namespace Unbreathe.UI
{
    /// <summary>
    /// Controlador de la barra de cordura en el HUD (uGUI).
    /// Muestra el nivel actual de cordura en tiempo real con una barra estética
    /// y etiqueta indicadora.
    /// </summary>
    public class SanityBarUI : MonoBehaviour
    {
        [Header("Referencias UI")]
        [Tooltip("Slider que representa la barra de cordura (opcional si se usa fillImage).")]
        [SerializeField] private Slider sanitySlider;

        [Tooltip("Imagen con tipo Fill para el relleno de la barra (opcional si se usa Slider).")]
        [SerializeField] private Image fillImage;

        [Tooltip("Texto indicador de cordura (ej. 'Cordura' o 'Cordura: 100%').")]
        [SerializeField] private TMP_Text labelText;

        [Header("Estilo Visual")]
        [Tooltip("Color de la barra cuando la cordura está alta (cian/grisáceo tranquilo).")]
        [SerializeField] private Color normalColor = new Color(0.35f, 0.75f, 0.85f, 1.0f);

        [Tooltip("Color de la barra cuando la cordura está crítica (rojo/púrpura de tensión).")]
        [SerializeField] private Color criticalColor = new Color(0.85f, 0.25f, 0.30f, 1.0f);

        [Tooltip("Velocidad de suavizado visual.")]
        [SerializeField] private float smoothSpeed = 8f;

        private SanityManager sanityManager;
        private float displayedNormalized = 1f;

        private void Awake()
        {
            if (sanitySlider == null) sanitySlider = GetComponentInChildren<Slider>();
            if (fillImage == null && sanitySlider == null)
            {
                var images = GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    if (img.type == Image.Type.Filled)
                    {
                        fillImage = img;
                        break;
                    }
                }
            }
        }

        private void Start()
        {
            sanityManager = SanityManager.Instance;
            if (sanityManager == null)
            {
                var sanityGo = GameObject.Find("SanitySystem");
                if (sanityGo != null) sanityManager = sanityGo.GetComponent<SanityManager>();
            }

            if (sanityManager != null)
            {
                sanityManager.OnSanityChanged += OnSanityChanged;
                UpdateDisplayInstant(sanityManager.CurrentSanity, sanityManager.MaxSanity);
            }
        }

        private void OnDestroy()
        {
            if (sanityManager != null)
            {
                sanityManager.OnSanityChanged -= OnSanityChanged;
            }
        }

        private void Update()
        {
            // Reintentar resolución si inició antes
            if (sanityManager == null)
            {
                sanityManager = SanityManager.Instance;
                if (sanityManager != null)
                {
                    sanityManager.OnSanityChanged += OnSanityChanged;
                    UpdateDisplayInstant(sanityManager.CurrentSanity, sanityManager.MaxSanity);
                }
            }

            if (sanityManager != null)
            {
                float targetNorm = sanityManager.NormalizedSanity;
                displayedNormalized = Mathf.Lerp(displayedNormalized, targetNorm, smoothSpeed * Time.deltaTime);

                ApplyNormalizedValue(displayedNormalized);
            }
        }

        private void OnSanityChanged(float current, float max)
        {
            // El suavizado se calcula en Update
        }

        private void UpdateDisplayInstant(float current, float max)
        {
            displayedNormalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            ApplyNormalizedValue(displayedNormalized);
        }

        private void ApplyNormalizedValue(float norm)
        {
            if (sanitySlider != null)
            {
                sanitySlider.value = norm;
                if (sanitySlider.fillRect != null)
                {
                    var img = sanitySlider.fillRect.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = Color.Lerp(criticalColor, normalColor, norm);
                    }
                }
            }

            if (fillImage != null)
            {
                fillImage.fillAmount = norm;
                fillImage.color = Color.Lerp(criticalColor, normalColor, norm);
            }

            if (labelText != null && sanityManager != null)
            {
                int percent = Mathf.RoundToInt(sanityManager.CurrentSanity);
                labelText.text = $"Cordura  {percent}%";
            }
        }
    }
}
