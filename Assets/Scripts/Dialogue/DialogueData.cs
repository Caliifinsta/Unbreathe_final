using System;
using UnityEngine;

namespace Unbreathe.Dialogue
{
    [Serializable]
    public class DialogueLine
    {
        public string speakerName;
        [TextArea(2, 4)]
        public string message;
        public AudioClip voiceAudio;
    }

    /// <summary>
    /// Estructura de datos para secuencias narrativas en UNBREATHE.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogueData", menuName = "Unbreathe/Dialogue/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Header("Contenido Narrativo")]
        public string sequenceTitle;
        public DialogueLine[] lines;
    }
}
