using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unbreathe.UI;

namespace Unbreathe.EditorTools
{
    /// <summary>
    /// Genera Assets/Resources/PauseMenu.prefab con el Canvas, el panel y los 4 botones
    /// (Continuar, Reiniciar, Menú principal, Salir) ya conectados a MenuPausa.
    /// Uso: menú Unbreathe > Pausa > Crear PauseMenu Prefab.
    /// No abre ni modifica ninguna escena.
    /// </summary>
    public static class PauseMenuPrefabBuilder
    {
        private const string CarpetaResources = "Assets/Resources";
        private const string RutaPrefab = "Assets/Resources/PauseMenu.prefab";

        private static readonly Color ColorFondo = new Color(0f, 0f, 0f, 0.78f);
        private static readonly Color ColorTexto = new Color(0.92f, 0.92f, 0.92f, 1f);
        private static readonly Color ColorBoton = new Color(0.10f, 0.10f, 0.12f, 0.95f);
        private static readonly Color ColorBotonResaltado = new Color(0.24f, 0.24f, 0.30f, 1f);
        private static readonly Color ColorBotonPresionado = new Color(0.38f, 0.38f, 0.45f, 1f);

        [MenuItem("Unbreathe/Pausa/Crear PauseMenu Prefab")]
        public static void CrearPrefab()
        {
            if (!AssetDatabase.IsValidFolder(CarpetaResources))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
            {
                bool sobrescribir = EditorUtility.DisplayDialog(
                    "PauseMenu ya existe",
                    "Ya existe Assets/Resources/PauseMenu.prefab.\n\n" +
                    "Si lo sobrescribes se pierden los cambios manuales (por ejemplo, el sonido de clic que hayas asignado).",
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
                Debug.LogError("[PauseMenuPrefabBuilder] No se pudo guardar el prefab en " + RutaPrefab);
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            Debug.Log("<color=cyan>[PauseMenuPrefabBuilder] Prefab creado en " + RutaPrefab + "</color>");
        }

        // ------------------------------------------------------------------
        // Construcción de la jerarquía
        // ------------------------------------------------------------------

        private static GameObject ConstruirJerarquia()
        {
            // Raíz: Canvas + escalador + raycaster + AudioSource + MenuPausa
            GameObject raiz = new GameObject(
                "PauseMenu",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(AudioSource));

            Canvas canvas = raiz.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // por encima de los Canvas de diálogo e interacción

            CanvasScaler escalador = raiz.GetComponent<CanvasScaler>();
            escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escalador.referenceResolution = new Vector2(1920f, 1080f);
            escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            escalador.matchWidthOrHeight = 0.5f;

            AudioSource sfx = raiz.GetComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.loop = false;
            sfx.spatialBlend = 0f;
            sfx.ignoreListenerPause = true;

            MenuPausa menu = raiz.AddComponent<MenuPausa>();

            // Panel que oscurece toda la pantalla
            GameObject panel = CrearUI("Panel_Pausa", raiz.transform);
            Estirar(panel.GetComponent<RectTransform>());
            Image fondo = panel.AddComponent<Image>();
            fondo.color = ColorFondo;

            // Contenedor central con layout vertical
            GameObject contenedor = CrearUI("Contenedor", panel.transform);
            RectTransform rtContenedor = contenedor.GetComponent<RectTransform>();
            rtContenedor.anchorMin = new Vector2(0.5f, 0.5f);
            rtContenedor.anchorMax = new Vector2(0.5f, 0.5f);
            rtContenedor.pivot = new Vector2(0.5f, 0.5f);
            rtContenedor.anchoredPosition = Vector2.zero;
            rtContenedor.sizeDelta = new Vector2(560f, 620f);

            VerticalLayoutGroup layout = contenedor.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(40, 40, 30, 30);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CrearTexto("Titulo", contenedor.transform, "PAUSA", 64f, 110f, true);

            Button botonContinuar = CrearBoton("Boton_Continuar", contenedor.transform, "CONTINUAR");
            Button botonReiniciar = CrearBoton("Boton_Reiniciar", contenedor.transform, "REINICIAR");
            Button botonMenuPrincipal = CrearBoton("Boton_MenuPrincipal", contenedor.transform, "MENÚ PRINCIPAL");
            Button botonSalir = CrearBoton("Boton_Salir", contenedor.transform, "SALIR");

            // Conectar las referencias serializadas (campos privados) de MenuPausa
            SerializedObject so = new SerializedObject(menu);
            Asignar(so, "panelPausa", panel);
            Asignar(so, "botonContinuar", botonContinuar);
            Asignar(so, "botonReiniciar", botonReiniciar);
            Asignar(so, "botonMenuPrincipal", botonMenuPrincipal);
            Asignar(so, "botonSalir", botonSalir);
            Asignar(so, "sfxAudioSource", sfx);
            so.ApplyModifiedPropertiesWithoutUndo();

            // El panel arranca oculto; MenuPausa lo muestra al pausar.
            panel.SetActive(false);

            return raiz;
        }

        // ------------------------------------------------------------------
        // Utilidades
        // ------------------------------------------------------------------

        private static GameObject CrearUI(string nombre, Transform padre)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return go;
        }

        private static void Estirar(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string contenido, float tamano, float alto, bool conLayout)
        {
            GameObject go = CrearUI(nombre, padre);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = contenido;
            tmp.fontSize = tamano;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ColorTexto;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            if (conLayout)
            {
                LayoutElement elemento = go.AddComponent<LayoutElement>();
                elemento.minHeight = alto;
                elemento.preferredHeight = alto;
            }

            return tmp;
        }

        private static Button CrearBoton(string nombre, Transform padre, string etiqueta)
        {
            GameObject go = CrearUI(nombre, padre);

            Image imagen = go.AddComponent<Image>();
            imagen.color = Color.white; // el color real lo da el ColorBlock del Button

            Button boton = go.AddComponent<Button>();
            boton.targetGraphic = imagen;

            ColorBlock colores = boton.colors;
            colores.normalColor = ColorBoton;
            colores.highlightedColor = ColorBotonResaltado;
            colores.pressedColor = ColorBotonPresionado;
            colores.selectedColor = ColorBoton;
            colores.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.1f;
            boton.colors = colores;

            LayoutElement elemento = go.AddComponent<LayoutElement>();
            elemento.minHeight = 80f;
            elemento.preferredHeight = 80f;

            TextMeshProUGUI texto = CrearTexto("Texto", go.transform, etiqueta, 34f, 0f, false);
            Estirar(texto.rectTransform);

            return boton;
        }

        private static void Asignar(SerializedObject so, string campo, Object valor)
        {
            SerializedProperty propiedad = so.FindProperty(campo);
            if (propiedad == null)
            {
                Debug.LogError("[PauseMenuPrefabBuilder] MenuPausa no tiene el campo serializado '" + campo + "'.");
                return;
            }

            propiedad.objectReferenceValue = valor;
        }
    }
}
