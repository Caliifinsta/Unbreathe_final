using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unbreathe.UI
{
    /// <summary>
    /// Aplica el estilo común de UNBREATHE (UnbreatheUITheme) al menú principal cuando carga
    /// la escena "MainMenu". No modifica MainMenu.unity ni MainMenuManager.cs: busca los objetos
    /// por nombre y ajusta tamaños, colores, contornos y la casilla de "Pantalla Completa".
    /// Los cambios se ven solo al dar Play. Si un objeto no se encuentra (por ejemplo, si se le
    /// cambió el nombre en la escena) se avisa en la Console y el resto del estilo se aplica igual.
    /// </summary>
    public static class MainMenuStyler
    {
        private const string EscenaMenuPrincipal = "MainMenu";
        private const string NombreCanvas = "MainMenu_Canvas";

        // Mensaje de resumen en la Console. Poner en false cuando todo funcione.
        private const bool MostrarResumen = true;

        private static readonly List<string> noEncontrados = new List<string>();
        private static int elementosEstilizados;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inicializar()
        {
            // Corre una sola vez al arrancar; las escenas siguientes llegan por sceneLoaded.
            // El -= evita doble suscripción si el Editor tiene el Domain Reload desactivado.
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;

            Aplicar(SceneManager.GetActiveScene());
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo != LoadSceneMode.Single) return;
            Aplicar(escena);
        }

        private static void Aplicar(Scene escena)
        {
            if (escena.name != EscenaMenuPrincipal) return;

            GameObject canvasGo = GameObject.Find(NombreCanvas);
            if (canvasGo == null)
            {
                Debug.LogWarning("[MainMenuStyler] No se encontró '" + NombreCanvas + "'. No se aplicó el estilo.");
                return;
            }

            Transform[] todos = canvasGo.GetComponentsInChildren<Transform>(true);
            noEncontrados.Clear();
            elementosEstilizados = 0;

            // Cada paso va protegido: si uno falla se avisa en la Console y se sigue con el resto.

            // Textos sueltos
            Intentar("Txt_Title", () => EstilizarTexto(todos, "Txt_Title", UnbreatheUITheme.TamanoTitulo, UnbreatheUITheme.ColorAcento));
            Intentar("Txt_VolLabel", () => EstilizarTexto(todos, "Txt_VolLabel", UnbreatheUITheme.TamanoTexto, UnbreatheUITheme.ColorTexto));
            Intentar("Txt_VolPercent", () => EstilizarTexto(todos, "Txt_VolPercent", UnbreatheUITheme.TamanoTexto, UnbreatheUITheme.ColorAcento));
            Intentar("Txt_ToggleLabel", () => EstilizarTexto(todos, "Txt_ToggleLabel", UnbreatheUITheme.TamanoTexto, UnbreatheUITheme.ColorTexto));
            Intentar("Txt_Hint", () => EstilizarTexto(todos, "Txt_Hint", UnbreatheUITheme.TamanoTexto, UnbreatheUITheme.ColorTexto));

            // Botones: banners del menú (con borde opcional) y VOLVER (siempre con borde)
            Intentar("Btn_Play", () => EstilizarBoton(todos, "Btn_Play", UnbreatheUITheme.BordeEnBanners));
            Intentar("Btn_Options", () => EstilizarBoton(todos, "Btn_Options", UnbreatheUITheme.BordeEnBanners));
            Intentar("Btn_Quit", () => EstilizarBoton(todos, "Btn_Quit", UnbreatheUITheme.BordeEnBanners));
            Intentar("Btn_CloseOptions", () => EstilizarBoton(todos, "Btn_CloseOptions", true));

            // Panel de opciones, barra de volumen y casilla de pantalla completa
            Intentar("Panel_Opciones", () => EstilizarPanelOpciones(todos));
            Intentar("Slider_Volume", () => EstilizarBarraVolumen(todos));
            Intentar("Toggle_Fullscreen", () => CrearCasillaPantallaCompleta(todos));

            if (noEncontrados.Count > 0)
            {
                Debug.LogWarning("[MainMenuStyler] No se encontraron estos objetos (¿cambiaron de nombre?): " +
                                 string.Join(", ", noEncontrados));
            }

            if (MostrarResumen)
            {
                Debug.Log("<color=cyan>[MainMenuStyler] Estilo aplicado a " + elementosEstilizados + " elementos del menú principal.</color>");
            }
        }

        private static void Intentar(string nombre, System.Action accion)
        {
            try
            {
                accion();
            }
            catch (System.Exception error)
            {
                Debug.LogWarning("[MainMenuStyler] No se pudo estilizar '" + nombre + "': " + error.Message);
            }
        }

        // ------------------------------------------------------------------
        // Textos
        // ------------------------------------------------------------------

        private static void EstilizarTexto(Transform[] todos, string nombre, float tamano, Color color)
        {
            Transform t = Buscar(todos, nombre);
            if (t == null) return;

            TextMeshProUGUI texto = t.GetComponent<TextMeshProUGUI>();
            if (texto == null)
            {
                noEncontrados.Add(nombre + " (sin TextMeshProUGUI)");
                return;
            }

            AplicarEstiloTexto(texto, tamano, color);
        }

        private static void AplicarEstiloTexto(TextMeshProUGUI texto, float tamano, Color color)
        {
            texto.enableAutoSizing = false;
            texto.fontSize = tamano;
            texto.color = color;
            texto.textWrappingMode = TextWrappingModes.NoWrap; // una sola línea, aunque el texto crezca

            // El contorno se aplica con un componente aparte: los textos de paneles inactivos
            // (como Panel_Opciones) aún no inicializaron su material y fallarían si se tocan ahora.
            UITextOutline contorno = texto.GetComponent<UITextOutline>();
            if (contorno == null)
            {
                contorno = texto.gameObject.AddComponent<UITextOutline>();
            }
            contorno.Configurar(UnbreatheUITheme.GrosorContornoTexto, UnbreatheUITheme.ColorContornoTexto);

            elementosEstilizados++;
        }

        // ------------------------------------------------------------------
        // Botones
        // ------------------------------------------------------------------

        private static void EstilizarBoton(Transform[] todos, string nombre, bool conBorde)
        {
            Transform t = Buscar(todos, nombre);
            if (t == null) return;

            Image imagen = t.GetComponent<Image>();
            if (imagen != null)
            {
                // El color real lo da el ColorBlock del Button (tinte), igual para todos los botones.
                imagen.color = Color.white;

                if (conBorde)
                {
                    AgregarBorde(imagen.gameObject, UnbreatheUITheme.ColorAcento, UnbreatheUITheme.GrosorBordeBoton);
                }
            }

            Button boton = t.GetComponent<Button>();
            if (boton != null)
            {
                ColorBlock colores = boton.colors;
                colores.normalColor = UnbreatheUITheme.ColorBoton;
                colores.highlightedColor = UnbreatheUITheme.ColorBotonHover;
                colores.pressedColor = UnbreatheUITheme.ColorBotonPresionado;
                colores.selectedColor = UnbreatheUITheme.ColorBoton; // que no se quede "encendido" tras el clic
                colores.disabledColor = UnbreatheUITheme.ColorBotonDeshabilitado;
                colores.colorMultiplier = 1f;
                colores.fadeDuration = 0.1f;
                boton.colors = colores;
            }

            TextMeshProUGUI texto = t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto != null)
            {
                AplicarEstiloTexto(texto, UnbreatheUITheme.TamanoBoton, UnbreatheUITheme.ColorTexto);
            }

            elementosEstilizados++;
        }

        private static void AgregarBorde(GameObject objetivo, Color color, float grosor)
        {
            Outline borde = objetivo.GetComponent<Outline>();
            if (borde == null)
            {
                borde = objetivo.AddComponent<Outline>();
            }

            borde.effectColor = color;
            borde.effectDistance = new Vector2(grosor, -grosor);
        }

        // ------------------------------------------------------------------
        // Panel de opciones, barra de volumen y casilla
        // ------------------------------------------------------------------

        private static void EstilizarPanelOpciones(Transform[] todos)
        {
            Transform panel = Buscar(todos, "Panel_Opciones");
            if (panel == null) return;

            Image fondo = panel.GetComponent<Image>();
            if (fondo != null)
            {
                fondo.color = UnbreatheUITheme.ColorPanel;
                elementosEstilizados++;
            }

            // Capa que oscurece lo que queda detrás (JUGAR, OPCIONES, SALIR) mientras el panel está abierto.
            // Es solo visual: no bloquea clics.
            GameObject atenuador = new GameObject("Atenuador_Opciones", typeof(RectTransform), typeof(Image));
            atenuador.transform.SetParent(panel.parent, false);

            RectTransform rt = atenuador.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image imagenAtenuador = atenuador.GetComponent<Image>();
            imagenAtenuador.color = UnbreatheUITheme.ColorAtenuador;
            imagenAtenuador.raycastTarget = false;

            // Justo detrás del panel: encima de los banners, debajo del panel.
            atenuador.transform.SetSiblingIndex(panel.GetSiblingIndex());
            atenuador.SetActive(panel.gameObject.activeSelf);

            UIActiveMirror espejo = panel.GetComponent<UIActiveMirror>();
            if (espejo == null)
            {
                espejo = panel.gameObject.AddComponent<UIActiveMirror>();
            }
            espejo.SetObjetivo(atenuador);

            elementosEstilizados++;
        }

        private static void EstilizarBarraVolumen(Transform[] todos)
        {
            Transform barra = Buscar(todos, "Slider_Volume");
            if (barra == null) return;

            // El Image del propio Slider es la pista (la parte vacía de la barra).
            Image pista = barra.GetComponent<Image>();
            if (pista != null)
            {
                pista.color = UnbreatheUITheme.ColorPista;
                elementosEstilizados++;
            }
        }

        private static void CrearCasillaPantallaCompleta(Transform[] todos)
        {
            Transform t = Buscar(todos, "Toggle_Fullscreen");
            if (t == null) return;

            Toggle toggle = t.GetComponent<Toggle>();
            if (toggle == null)
            {
                noEncontrados.Add("Toggle_Fullscreen (sin Toggle)");
                return;
            }

            // Si algún día le agregan casilla propia, se respeta su diseño.
            if (toggle.targetGraphic != null || toggle.graphic != null) return;

            // Casilla: a la izquierda de la fila, en el margen del panel, para no mover el texto.
            GameObject casilla = new GameObject("Casilla", typeof(RectTransform), typeof(Image));
            casilla.transform.SetParent(t, false);

            RectTransform rtCasilla = casilla.GetComponent<RectTransform>();
            rtCasilla.anchorMin = new Vector2(0f, 0.5f);
            rtCasilla.anchorMax = new Vector2(0f, 0.5f);
            rtCasilla.pivot = new Vector2(0f, 0.5f);
            rtCasilla.anchoredPosition = new Vector2(-56f, 0f);
            rtCasilla.sizeDelta = new Vector2(40f, 40f);

            Image imagenCasilla = casilla.GetComponent<Image>();
            imagenCasilla.color = UnbreatheUITheme.ColorBoton;
            AgregarBorde(casilla, UnbreatheUITheme.ColorAcento, UnbreatheUITheme.GrosorBordeBoton);

            // Marca (cuadro cian) que aparece cuando está activado
            GameObject marca = new GameObject("Marca", typeof(RectTransform), typeof(Image));
            marca.transform.SetParent(casilla.transform, false);

            RectTransform rtMarca = marca.GetComponent<RectTransform>();
            rtMarca.anchorMin = Vector2.zero;
            rtMarca.anchorMax = Vector2.one;
            rtMarca.offsetMin = new Vector2(8f, 8f);
            rtMarca.offsetMax = new Vector2(-8f, -8f);

            Image imagenMarca = marca.GetComponent<Image>();
            imagenMarca.color = UnbreatheUITheme.ColorAcento;
            imagenMarca.raycastTarget = false;

            toggle.targetGraphic = imagenCasilla;
            toggle.graphic = imagenMarca;
            imagenMarca.canvasRenderer.SetAlpha(toggle.isOn ? 1f : 0f);

            elementosEstilizados++;
        }

        // ------------------------------------------------------------------
        // Búsqueda por nombre (incluye objetos inactivos)
        // ------------------------------------------------------------------

        private static Transform Buscar(Transform[] todos, string nombre)
        {
            for (int i = 0; i < todos.Length; i++)
            {
                if (todos[i].name == nombre)
                {
                    return todos[i];
                }
            }

            noEncontrados.Add(nombre);
            return null;
        }
    }
}
