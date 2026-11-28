using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unbreathe.UI;

namespace Unbreathe.EditorTools
{
    /// <summary>
    /// Genera Assets/Resources/ScreenFader.prefab: un Canvas de pantalla completa en negro con
    /// CanvasGroup (para el fundido) y el componente ScreenFader, ordenado por encima de todas
    /// las demás interfaces (pausa = 500, aviso = 100).
    /// Uso: menú Unbreathe > Transicion > Crear ScreenFader Prefab.
    /// No abre ni modifica ninguna escena.
    /// </summary>
    public static class ScreenFaderPrefabBuilder
    {
        private const string CarpetaResources = "Assets/Resources";
        private const string RutaPrefab = "Assets/Resources/ScreenFader.prefab";

        // Máximo permitido por Unity para sortingOrder es 32767.
        private const int OrdenCanvas = 32000;

        [MenuItem("Unbreathe/Transicion/Crear ScreenFader Prefab")]
        public static void CrearPrefab()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaResources))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            {
                bool sobrescribir = EditorUtility.DisplayDialog(
                    "ScreenFader ya existe",
                    "Ya existe Assets/Resources/ScreenFader.prefab.\n\n" +
                    "Si lo sobrescribes se pierden los cambios manuales (duraciones, curva, fundido de volumen).\n\n" +
                    "Cierra el prefab si lo tienes abierto en modo edición antes de continuar.",
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
                Debug.LogError("[ScreenFaderPrefabBuilder] No se pudo guardar el prefab en " + RutaPrefab);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log("<color=cyan>[ScreenFaderPrefabBuilder] Prefab creado en " + RutaPrefab + "</color>");
        }

        private static GameObject ConstruirJerarquia()
        {
            // Raíz: Canvas + escalador + raycaster (para poder bloquear clics) + CanvasGroup + ScreenFader
            GameObject raiz = new GameObject(
                "ScreenFader",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup));

            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OrdenCanvas;

            CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            CanvasGroup grupo = raiz.GetComponent<CanvasGroup>();
            grupo.alpha = 1f;            // arranca en negro; ScreenFader hace el primer fade in
            grupo.interactable = false;
            grupo.blocksRaycasts = true;

            ScreenFader fader = raiz.AddComponent<ScreenFader>();

            // Panel negro que cubre toda la pantalla
            GameObject negro = new GameObject("Negro", typeof(RectTransform), typeof(Image));
            negro.transform.SetParent(raiz.transform, false);

            RectTransform rt = negro.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image imagen = negro.GetComponent<Image>();
            imagen.color = Color.black;
            imagen.raycastTarget = true;

            // Conectar la referencia serializada (campo privado) de ScreenFader
            SerializedObject so = new SerializedObject(fader);
            SerializedProperty propiedad = so.FindProperty("grupo");
            if (propiedad != null)
            {
                propiedad.objectReferenceValue = grupo;
            }
            else
            {
                Debug.LogError("[ScreenFaderPrefabBuilder] ScreenFader no tiene el campo serializado 'grupo'.");
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return raiz;
        }
    }
}
