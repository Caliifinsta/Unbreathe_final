using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbreathe.Input
{
    /// <summary>
    /// Manejador de entrada (Input) para el jugador.
    /// Responsabilidad única: capturar los controles físicos del jugador y emitir datos limpios.
    /// Controles de AGENTS.md:
    /// - Movimiento: WASD
    /// - Interacción: Tecla 'E' (recoger objetos, examinar entorno)
    /// - Narrativa: Tecla 'Espacio' (avanzar o activar sistema de diálogos)
    /// Compatible nativamente tanto con New Input System como con Legacy Input Manager.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerInputHandler : MonoBehaviour
    {
        public Vector2 MoveInput { get; private set; }
        public bool InputEnabled { get; set; } = true;

        public event Action OnInteractPressed;
        public event Action OnDialogueAdvancePressed;

        private void Update()
        {
            if (!InputEnabled)
            {
                MoveInput = Vector2.zero;
                return;
            }

            ReadMovement();
            ReadActions();
        }

        private void ReadMovement()
        {
            float horizontal = 0f;
            float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed) vertical -= 1f;
                if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed) horizontal += 1f;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKey(KeyCode.W)) vertical += 1f;
            if (UnityEngine.Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (UnityEngine.Input.GetKey(KeyCode.D)) horizontal += 1f;
#else
            // Fallback genérico cuando ambos están activos o detección híbrida
            try
            {
                if (UnityEngine.Input.GetKey(KeyCode.W)) vertical += 1f;
                if (UnityEngine.Input.GetKey(KeyCode.S)) vertical -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.A)) horizontal -= 1f;
                if (UnityEngine.Input.GetKey(KeyCode.D)) horizontal += 1f;
            }
            catch
            {
                // Si Legacy Input está desactivado por completo en ProjectSettings
            }
#endif

            Vector2 rawInput = new Vector2(horizontal, vertical);
            MoveInput = rawInput.sqrMagnitude > 1f ? rawInput.normalized : rawInput;
        }

        private void ReadActions()
        {
            bool interactThisFrame = false;
            bool dialogueThisFrame = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactThisFrame = true;
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    dialogueThisFrame = true;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                interactThisFrame = true;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                dialogueThisFrame = true;
            }
#else
            try
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.E)) interactThisFrame = true;
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space)) dialogueThisFrame = true;
            }
            catch
            {
                // Captura controlada en caso de backend único
            }
#endif

            if (interactThisFrame)
            {
                OnInteractPressed?.Invoke();
            }

            if (dialogueThisFrame)
            {
                OnDialogueAdvancePressed?.Invoke();
            }
        }
    }
}
