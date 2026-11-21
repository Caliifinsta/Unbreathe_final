using UnityEngine;

namespace Unbreathe.UI
{
    /// <summary>
    /// Estilo visual común de UNBREATHE: azul marino oscuro + acento cian + texto blanco.
    /// Nace del menú principal existente para que todas las interfaces se sientan del mismo juego.
    /// Los tamaños están pensados para un lienzo de 1920x1080 (el que usan los Canvas del juego).
    /// Para ajustar el aspecto de todas las interfaces basta con cambiar los valores de este archivo.
    /// </summary>
    public static class UnbreatheUITheme
    {
        // ---------------- Colores ----------------
        /// <summary>Fondo de paneles (#0F121C al 95%).</summary>
        public static readonly Color ColorPanel = new Color(0.059f, 0.071f, 0.110f, 0.95f);

        /// <summary>Fondo de botones (#141A29 al 95%).</summary>
        public static readonly Color ColorBoton = new Color(0.078f, 0.102f, 0.161f, 0.95f);

        /// <summary>Botón con el cursor encima (cian oscuro; el texto blanco conserva buen contraste).</summary>
        public static readonly Color ColorBotonHover = new Color(0.000f, 0.404f, 0.498f, 0.98f);

        /// <summary>Botón presionado.</summary>
        public static readonly Color ColorBotonPresionado = new Color(0.000f, 0.290f, 0.380f, 1.00f);

        /// <summary>Botón deshabilitado.</summary>
        public static readonly Color ColorBotonDeshabilitado = new Color(0.200f, 0.200f, 0.200f, 0.50f);

        /// <summary>Acento cian (#00D9FF): títulos, bordes, valores y marcas.</summary>
        public static readonly Color ColorAcento = new Color(0.000f, 0.851f, 1.000f, 1.000f);

        /// <summary>Texto principal.</summary>
        public static readonly Color ColorTexto = Color.white;

        /// <summary>Pista de las barras (sliders): más clara que el panel para que se note.</summary>
        public static readonly Color ColorPista = new Color(0.180f, 0.220f, 0.310f, 1.000f);

        /// <summary>Capa que oscurece lo que queda detrás de un panel.</summary>
        public static readonly Color ColorAtenuador = new Color(0f, 0f, 0f, 0.65f);

        // ---------------- Contornos ----------------
        /// <summary>Contorno oscuro del texto para que destaque sobre la escena 3D.</summary>
        public static readonly Color32 ColorContornoTexto = new Color32(5, 8, 15, 255);

        public const float GrosorContornoTexto = 0.18f;

        /// <summary>Borde cian de botones y casillas.</summary>
        public const float GrosorBordeBoton = 2f;

        /// <summary>Si es true, los banners (JUGAR, OPCIONES, SALIR) llevan un borde cian fino.</summary>
        public const bool BordeEnBanners = true;

        // ---------------- Tamaños de texto (1920x1080) ----------------
        public const float TamanoTitulo = 48f;
        public const float TamanoBoton = 36f;
        public const float TamanoTexto = 30f;
        public const float TamanoMinimo = 26f;
    }
}
