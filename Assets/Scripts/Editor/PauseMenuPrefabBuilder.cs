using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unbreathe.UI;

namespace Unbreathe.EditorTools
{
    /// <summary>
    /// Genera Assets/Resources/PauseMenu.prefab con el estilo común de UNBREATHE (UnbreatheUITheme):
    /// una tarjeta azul marino con borde cian detrás del título y los 4 botones
    /// (Continuar, Reiniciar, Menú principal, Salir) ya conectados a MenuPausa, más el recordatorio
    /// "ESC para continuar". La tarjeta hace que el menú se lea igual sobre cualquier escena.
    /// Uso: menú Unbreathe > Pausa > Crear PauseMenu Prefab.
    /// No abre ni modifica ninguna escena.
    /// </summary>
    public static class PauseMenuPrefabBuilder
    {
        private const string CarpetaResources = "Assets/Resources";
        private const string RutaPrefab = "Assets/Resources/PauseMenu.prefab";

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
                    "Si lo sobrescribes se pierden los cambios manuales (por ejemplo, el sonido de clic que hayas asignado).\n\n" +
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

            // Panel de pantalla completa: solo oscurece la escena
            GameObject panel = CrearUI("Panel_Pausa", raiz.transform);
            Estirar(panel.GetComponent<RectTransform>());
            Image atenuador = panel.AddComponent<Image>();
            atenuador.color = UnbreatheUITheme.ColorAtenuador;

            // Tarjeta central: fondo azul marino con borde cian; se ajusta sola al contenido
            GameObject tarjeta = CrearUI("Tarjeta", panel.transform);
            RectTransform rtTarjeta = tarjeta.GetComponent<RectTransform>();
            rtTarjeta.anchorMin = new Vector2(0.5f, 0.5f);
            rtTarjeta.anchorMax = new Vector2(0.5f, 0.5f);
            rtTarjeta.pivot = new Vector2(0.5f, 0.5f);
            rtTarjeta.anchoredPosition = Vector2.zero;
            rtTarjeta.sizeDelta = new Vector2(620f, 0f);

            Image fondoTarjeta = tarjeta.AddComponent<Image>();
            fondoTarjeta.color = UnbreatheUITheme.ColorPanel;
            AgregarBorde(tarjeta);

            VerticalLayoutGroup layout = tarjeta.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.padding = new RectOffset(50, 50, 40, 40);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter ajustador = tarjeta.AddComponent<ContentSizeFitter>();
            ajustador.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            ajustador.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            CrearTexto("Titulo", tarjeta.transform, "PAUSA", UnbreatheUITheme.TamanoTituloPantalla, 110f, true, UnbreatheUITheme.ColorAcento);

            Button botonContinuar = CrearBoton("Boton_Continuar", tarjeta.transform, "CONTINUAR");
            Button botonReiniciar = CrearBoton("Boton_Reiniciar", tarjeta.transform, "REINICIAR");
            Button botonMenuPrincipal = CrearBoton("Boton_MenuPrincipal", tarjeta.transform, "MENÚ PRINCIPAL");
            Button botonSalir = CrearBoton("Boton_Salir", tarjeta.transform, "SALIR");

            CrearTexto("Recordatorio", tarjeta.transform, "ESC para continuar", UnbreatheUITheme.TamanoMinimo, 50f, true, UnbreatheUITheme.ColorTextoSecundario);

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

        private static void AgregarBorde(GameObject objetivo)
        {
            Outline borde = objetivo.AddComponent<Outline>();
            borde.effectColor = UnbreatheUITheme.ColorAcento;
            borde.effectDistance = new Vector2(UnbreatheUITheme.GrosorBordeBoton, -UnbreatheUITheme.GrosorBordeBoton);
        }

        private static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string contenido, float tamano, float alto, bool conLayout, Color color)
        {
            GameObject go = CrearUI(nombre, padre);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = contenido;
            tmp.fontSize = tamano;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
            {
                tmp.font = TMP_Settings.defaultFontAsset;
            }

            // Contorno oscuro del texto (se aplica en Play)
            UITextOutline contorno = go.AddComponent<UITextOutline>();
            contorno.Configurar(UnbreatheUITheme.GrosorContornoTexto, UnbreatheUITheme.ColorContornoTexto);

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
            colores.normalColor = UnbreatheUITheme.ColorBoton;
            colores.highlightedColor = UnbreatheUITheme.ColorBotonHover;
            colores.pressedColor = UnbreatheUITheme.ColorBotonPresionado;
            colores.selectedColor = UnbreatheUITheme.ColorBoton;
            colores.disabledColor = UnbreatheUITheme.ColorBotonDeshabilitado;
            colores.colorMultiplier = 1f;
            colores.fadeDuration = 0.1f;
            boton.colors = colores;

            AgregarBorde(go);

            LayoutElement elemento = go.AddComponent<LayoutElement>();
            elemento.minHeight = 80f;
            elemento.preferredHeight = 80f;

            TextMeshProUGUI texto = CrearTexto("Texto", go.transform, etiqueta, UnbreatheUITheme.TamanoBoton, 0f, false, UnbreatheUITheme.ColorTexto);
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
