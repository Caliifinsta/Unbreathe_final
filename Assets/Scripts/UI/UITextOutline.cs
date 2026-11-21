using System.Collections;
using TMPro;
using UnityEngine;

namespace Unbreathe.UI
{
    /// <summary>
    /// Aplica un contorno oscuro a un texto TextMeshPro cuando el objeto está activo.
    /// Los textos que viven dentro de paneles inactivos (como Panel_Opciones) todavía no
    /// inicializaron su material al cargar la escena; cambiarles el contorno en ese momento
    /// lanza un NullReferenceException. Por eso el contorno se aplica un frame después de
    /// que el objeto se activa, cuando el texto ya está listo.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public class UITextOutline : MonoBehaviour
    {
        [SerializeField] private float grosor = 0.18f;
        [SerializeField] private Color32 color = new Color32(5, 8, 15, 255);

        private TMP_Text texto;
        private Coroutine rutina;

        public void Configurar(float nuevoGrosor, Color32 nuevoColor)
        {
            grosor = nuevoGrosor;
            color = nuevoColor;

            if (isActiveAndEnabled)
            {
                Programar();
            }
        }

        private void OnEnable()
        {
            Programar();
        }

        private void OnDisable()
        {
            rutina = null;
        }

        private void Programar()
        {
            if (rutina != null)
            {
                StopCoroutine(rutina);
            }

            rutina = StartCoroutine(AplicarSiguienteFrame());
        }

        private IEnumerator AplicarSiguienteFrame()
        {
            yield return null;

            if (texto == null)
            {
                texto = GetComponent<TMP_Text>();
            }

            if (texto != null)
            {
                texto.outlineWidth = grosor;
                texto.outlineColor = color;
            }

            rutina = null;
        }
    }
}
