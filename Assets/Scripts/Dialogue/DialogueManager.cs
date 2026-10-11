using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unbreathe.Dialogue
{
    /// <summary>
    /// Administrador de diálogos y narrativa diegética para UNBREATHE.
    /// Respeta la regla de diseño: interfaz no intrusiva y audio diegético.
    /// Almacena referencias en Awake() y no realiza búsquedas en Update().
    /// </summary>
    [DisallowMultipleComponent]
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("Audio Diegético Narrativo")]
        [SerializeField] private AudioSource narrativeAudioSource;

        private readonly Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
        private DialogueLine currentLine;

        public bool IsDialogueActive { get; private set; }

        public event Action<DialogueLine> OnLineDisplayed;
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (narrativeAudioSource == null)
            {
                narrativeAudioSource = GetComponent<AudioSource>();
            }
        }

        public void StartDialogue(DialogueData data)
        {
            if (data == null || data.lines == null || data.lines.Length == 0) return;

            dialogueQueue.Clear();
            foreach (var line in data.lines)
            {
                dialogueQueue.Enqueue(line);
            }

            IsDialogueActive = true;
            OnDialogueStarted?.Invoke();
            AdvanceDialogue();
        }

        /// <summary>
        /// Avanza la línea de diálogo actual (activado con la tecla Espacio).
        /// </summary>
        public void AdvanceDialogue()
        {
            if (!IsDialogueActive) return;

            if (dialogueQueue.Count > 0)
            {
                currentLine = dialogueQueue.Dequeue();
                PlayVoiceAudio(currentLine.voiceAudio);
                OnLineDisplayed?.Invoke(currentLine);
            }
            else
            {
                EndDialogue();
            }
        }

        public void EndDialogue()
        {
            IsDialogueActive = false;
            currentLine = null;

            if (narrativeAudioSource != null && narrativeAudioSource.isPlaying)
            {
                narrativeAudioSource.Stop();
            }

            OnDialogueEnded?.Invoke();
        }

        private void PlayVoiceAudio(AudioClip clip)
        {
            if (narrativeAudioSource != null && clip != null)
            {
                narrativeAudioSource.Stop();
                narrativeAudioSource.clip = clip;
                narrativeAudioSource.Play();
            }
        }
    }
}
