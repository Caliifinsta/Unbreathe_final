using UnityEngine;

namespace Unbreathe.Memories
{
    /// <summary>
    /// Datos que definen un recuerdo coleccionable (Fase 3 y 4).
    /// Contiene su identificación, título, foto estilo Polaroid, líneas de monólogo
    /// para el flashback 2D y la recompensa de cordura al descubrirlo.
    /// </summary>
    [CreateAssetMenu(fileName = "NewMemoryData", menuName = "Unbreathe/Memory Data", order = 1)]
    public class MemoryData : ScriptableObject
    {
        [Header("Identificación")]
        [Tooltip("Identificador único para guardar progreso o búsqueda.")]
        public string memoryId = "memory_01";

        [Tooltip("Título del recuerdo en el álbum y la cinemática.")]
        public string title = "Recuerdo";

        [Header("Material Visual")]
        [Tooltip("Fotografía 2D del recuerdo que se mostrará en el álbum y flashback.")]
        public Sprite photo;

        [Header("Secuencia Narrativa")]
        [Tooltip("Líneas de texto que se muestran con máquina de escribir durante el flashback.")]
        [TextArea(2, 4)]
        public string[] flashbackLines = new string[0];

        [Header("Recompensa")]
        [Tooltip("Cantidad de cordura que Karol recupera la primera vez que lo encuentra.")]
        public float sanityReward = 25f;
    }
}
