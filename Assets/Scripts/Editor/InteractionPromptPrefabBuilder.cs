using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unbreathe.Interaction;
using Unbreathe.UI;

namespace Unbreathe.EditorTools
{
    /// <summary>
    /// Genera Assets/Resources/InteractionPrompt.prefab: un aviso minimalista (caja oscura
    /// semitransparente + texto TextMeshPro) centrado abajo, con CanvasGroup para el fade.
    /// Uso: menú Unbreathe > Interaccion > Crear InteractionPrompt Prefab.
    /// No abre ni modifica ninguna escena.
    /// </summary>
    public static class InteractionPromptPrefabBuilder
    {
        private const string CarpetaResources = "Assets/Resources";
        private const string RutaPrefab = "Assets/Resources/InteractionPrompt.prefab";

        [MenuItem("Unbreathe/Interaccion/Crear InteractionPrompt Prefab")]
        public static void CrearPrefab()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaResources))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            {
                bool sobrescribir = EditorUtility.DisplayDialog(
                    "InteractionPrompt ya existe",
                    "Ya existe Assets/Resources/InteractionPrompt.prefab.\n\n" +
                    "Si lo sobrescribes se pierden los cambios manuales que le hayas hecho (colores, tamaño, tiempos).",
                    "Sobrescribir",
                    "Cancelar");

                if (!sobrescribir) return;
            }

            GameObject raiz = ConstruirJerarquia();

            bool guardado;
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab, out guardado);
            Object.DestroyImmediate(raiz);

            if (!guardado)
            {
                Debug.LogError("[InteractionPromptPrefabBuilder] No se pudo guardar el prefab en " + RutaPrefab);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log("<color=cyan>[InteractionPromptPrefabBuilder] Prefab creado en " + RutaPrefab + "</color>");
        }

        private static GameObject ConstruirJerarquia()
        {
            // Raíz: Canvas + escalador + componente del aviso.
            // Sin GraphicRaycaster: el aviso nunca debe capturar clics.
            GameObject raiz = new GameObject(
                "InteractionPrompt",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // por debajo del menú de pausa (500)

            CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            InteractionPromptView vista = raiz.AddComponent<InteractionPromptView>();

            // Caja del aviso: abajo al centro, se ajusta sola al largo del texto
            GameObject aviso = CrearUI("Aviso", raiz.transform);
            RectTransform rtAviso = aviso.GetComponent<RectTransform>();
            rtAviso.anchorMin = new Vector2(0.5f, 0f);
            rtAviso.anchorMax = new Vector2(0.5f, 0f);
            rtAviso.pivot = new Vector2(0.5f, 0f);
            rtAviso.anchoredPosition = new Vector2(0f, 170f);
            rtAviso.sizeDelta = Vector2.zero;

            Image fondo = aviso.AddComponent<Image>();
            fondo.color = UnbreatheUITheme.ColorPanel;
            fondo.raycastTarget = false;

            // Borde cian fino: define la forma del aviso aunque el fondo de la escena sea claro u oscuro
            Outline borde = aviso.AddComponent<Outline>();
            borde.effectColor = UnbreatheUITheme.ColorAcento;
            borde.effectDistance = new Vector2(UnbreatheUITheme.GrosorBordeBoton, -UnbreatheUITheme.GrosorBordeBoton);

            CanvasGroup grupo = aviso.AddComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupo.interactable = false;
            grupo.blocksRaycasts = false;

            HorizontalLayoutGroup layout = aviso.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 16, 16);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter ajustador = aviso.AddComponent<ContentSizeFitter>();
            ajustador.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            ajustador.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Texto
            GameObject textoGo = CrearUI("Texto", aviso.transform);
            TextMeshProUGUI texto = textoGo.AddComponent<TextMeshProUGUI>();
            texto.text = "[E] Interactuar";
            texto.fontSize = UnbreatheUITheme.TamanoAviso;
            texto.alignment = TextAlignmentOptions.Center;
            texto.color = UnbreatheUITheme.ColorTexto;
            texto.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                texto.font = TMP_Settings.defaultFontAsset;
            }

            // Contorno oscuro del texto (se aplica en Play)
            UITextOutline contorno = textoGo.AddComponent<UITextOutline>();
            contorno.Configurar(UnbreatheUITheme.GrosorContornoTexto, UnbreatheUITheme.ColorContornoTexto);

            // Conectar referencias serializadas (campos privados) de InteractionPromptView
            SerializedObject so = new SerializedObject(vista);
            Asignar(so, "grupo", grupo);
            Asignar(so, "etiqueta", texto);
            so.ApplyModifiedPropertiesWithoutUndo();

            return raiz;
        }

        private static GameObject CrearUI(string nombre, Transform padre)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go;
        }

        private static void Asignar(SerializedObject so, string campo, Object valor)
        {
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogError("[InteractionPromptPrefabBuilder] InteractionPromptView no tiene el campo serializado '" + campo + "'.");
                return;
            }

            propiedad.objectReferenceValue = valor;
        }
    }
}
