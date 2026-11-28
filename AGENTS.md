# AGENTS.md - Directrices de Desarrollo para "UNBREATHE"

## 1. CONTEXTO DEL PROYECTO
* **Nombre:** UNBREATHE: Sumergido en el Silencio.
* **Motor:** Unity 6 LTS / C# / Físicas y Entorno 3D / Universal Render Pipeline (URP).
* **Género:** Exploración narrativa y tensión psicológica.
* **Estilo y Atmósfera:** Tensión e inmersión profunda. Fuerte dependencia de audio diegético, sistemas de partículas y diseño de interfaces (UI mockups) no intrusivas.

---

## 2. REGLAS ESTRICTAS DE PROGRAMACIÓN
* **Arquitectura (CRÍTICO):** Separar estrictamente la lógica visual (componente GFX), el controlador de físicas y la entrada del jugador (Input).
* **Rendimiento:** Prohibido usar `Find()`, `FindGameObjectWithTag()` o `GetComponent()` dentro de los métodos `Update()` o `FixedUpdate()`. Almacenar referencias en caché en `Awake()`.
* **Namespaces:** Para evitar ambigüedades de compilación entre `UnityEngine.Physics` y `Unbreathe.Physics`, calificar siempre de forma explícita las llamadas de motor como `UnityEngine.Physics.OverlapSphere`.
* **Cámaras y Billboards:** La cámara principal de cada escena debe llevar el tag `MainCamera`. Todo componente visual dependiente (`SpriteBillboard`, `CameraFollow`) debe implementar una resolución segura con `FindFirstObjectByType<Camera>()` como respaldo en `Awake()`.
* **Controles y Mecánicas Oficiales:** 
  * **Movimiento:** WASD (orientado relativo a la rotación de la cámara).
  * **Interacción:** Tecla **'E'** (recoger objetos, examinar entorno, abrir puertas).
  * **Narrativa:** Tecla **'Espacio'** (avanzar texto o cerrar sistema de diálogos).

---

## 3. SISTEMAS FUNDAMENTALES COMPLETADOS

### 3.1. Sistema de Interacción (`Unbreathe.Interaction`)
* **Contrato `IInteractable`:** Métodos estándar `GetPromptText()`, `CanInteract()`, `Interact(GameObject interactor)`.
* **Detección Física:** Los objetos interactuables requieren un `Collider` en modo **Trigger** y un `Rigidbody` cinemático (`isKinematic = true`) para garantizar la detección confiable con `CharacterController`.
* **Doble Comprobación:** `PlayerInteraction` cachea objetivos mediante `OnTriggerEnter/Exit` y ejecuta una comprobación de proximidad en el instante de pulsar 'E' para evitar fallos por capas o jerarquías.
* **UI No Intrusiva (`InteractionPromptUI`):** Muestra un cartel minimalista diegético (`[E] Acción`) únicamente cuando el personaje está en rango.

### 3.2. Sistema Narrativo y Diálogos (`Unbreathe.Dialogue`)
* **Datos Desacoplados (`DialogueData`):** ScriptableObjects que contienen título y líneas narrativas (`speakerName`, `message`, `voiceAudio`).
* **Pausa de Entrada:** Durante la lectura de documentos, computadoras o celulares, el movimiento del jugador se silencia automáticamente (`MoveInput = Vector2.zero`) hasta finalizar la secuencia.
* **Avance con Espacio:** `PlayerDialogueActivator` y `DialogueManager` procesan el avance línea a línea mediante la tecla 'Espacio'.

### 3.3. Transición de Escenas (`SceneTransitionDoor`)
* Permite transportar al jugador entre escenas (ej. `Primer Plano` ➔ `Segundo plano`) al pulsar 'E'.
* Todas las escenas del proyecto (`MainMenu`, `Primer Plano`, `Segundo plano`, `Tercer Plano`) deben estar registradas en `EditorBuildSettings`.

### 3.4. Audio Diegético y Pasos (`Unbreathe.Audio`)
* **Pasos (`PlayerFootsteps`):** Reproduce audio a intervalos rítmicos basados en la velocidad de movimiento y contacto con el suelo (`IsGrounded`). Incorpora micro-variaciones aleatorias de tono y un sintetizador procedural de impacto sordo de respaldo si no hay clips asignados.
* **Música Ambiental (`AmbientMusicPlayer`):** Reproducción en bucle con entrada gradual (*fade-in*) y volumen sutil calibrado (0.2 - 0.3) para preservar la inteligibilidad de diálogos y efectos.

### 3.5. Menú Principal y Persistencia de Audio (`Unbreathe.UI.MainMenuManager`)
* **Control de Volumen Maestro:** Se regula mediante `AudioListener.volume` y se persiste en `PlayerPrefs` con la clave `"MasterVolume"`.
* **Interfaz de Opciones:** El slider de volumen incluye estructura estándar de Unity (`Fill Area`, `Fill`, `Handle`) y lectura de porcentaje en tiempo real.
* **Flujo de Inicio:** Fade-in de botones sobre `Menu.jpeg` ➔ Prólogo interactivo con `InicioPlay.jpeg` ➔ Carga asíncrona de `Primer Plano`.

### 3.6. Sistema de Cordura y Apariciones Psicológicas (`Unbreathe.Sanity`)
* **Gestor de Cordura (`SanityManager`):** Rango de 0 a 100 (inicia en 100). Incluye decremento progresivo en el tiempo (`decayRate`), pausa durante diálogos y notas activas, y método público `RecoverSanity(float amount)` para eventos o recuerdos.
* **Barra de Cordura HUD (`SanityBarUI`):** Canvas uGUI `Screen Space - Overlay` con `Canvas Scaler` a 1920x1080. Ubicado en la esquina superior izquierda con slider de relleno cian a rojo crítico y etiqueta en tiempo real `"Cordura XX%"`.
* **Niebla Dinámica (`SanityFogController`):** Modula `RenderSettings.fogDensity` de `0.005` (lucidez) a `0.08` (angustia crítica) en modo `ExponentialSquared` con tonalidad gris frío oscuro conforme decrece la cordura.
* **Maniquíes Observadores (`WatcherApparition`):** Figuras de tensión psicológica sin lógica de ataque ni daño. Rotan suavemente sobre el eje Y hacia el `Player` mientras ejecutan sus animaciones de *Idle* / *Crouch*. Implementan escalado suave al aparecer y desvanecerse.
* **Generador Gradual (`ApparitionSpawner`):** Monitorea la cordura; al caer por debajo de 70 activa la congregación de figuras en un anillo de 4 a 9 metros (con prioridad en el cono frontal visible) apoyadas en el suelo mediante `UnityEngine.Physics.Raycast`. A mayor pérdida de cordura se acumulan más figuras (hasta 8-10 en nivel crítico); al recuperar cordura se retiran progresivamente.

### 3.7. Cinemática de Inicio (`Unbreathe.Cutscenes.IntroCutsceneManager`)
* **Canvas Cinematográfico (`IntroCutsceneCanvas`):** Canvas uGUI `Screen Space - Overlay` con sort order prioritario (100) y `Canvas Scaler` a 1920x1080. Contiene `FadePanel` (pantalla completa negra para fade-in de 2 segundos), `LetterboxGroup` (franjas negras superior e inferior de 105px) y `SubtitlePanel` con TextMeshPro.
* **Flujo Narrativo:** Al iniciar `Primer Plano`, bloquea `PlayerController3D`, pausa el decremento de cordura y oculta el HUD de cordura. Karol ejecuta su animación inicial de levantarse (`Personaje@Getting Up`) mientras corre el monólogo interior con efecto máquina de escribir.
* **Controles Cinemáticos:** Tecla 'Espacio' o Clic Izquierdo completa la frase actual o avanza a la siguiente; tecla 'Esc' salta la cinemática de inmediato. Al finalizar, desvanece suavemente el letterbox (1s), reactiva el movimiento del jugador y reanuda el sistema de cordura y su HUD.

### 3.8. Indicador de Objetivos y Libreta Diegética (Fase 2)
* **Indicador de Objetivos (`ObjectiveIndicator`):** Integrado en `SanityHUD`. Proyecta el objetivo 3D en pantalla (`WorldToScreenPoint`) con clamping en bordes (~55px) y orientación inversa si está fuera del cono visual. Muestra icono de diamante cian y distancia en metros (`XX m`). Se oculta automáticamente al estar a menos de 2.5m del objetivo o durante la cinemática de inicio. Método público: `SetObjective(Transform target, string text)`.
* **Libreta en la Banca (`NotebookInteractable`):** Ubicada en `Magic Book` sobre la banca. Incorpora una `Point Light` cálida ámbar con pulso senoidal diegético y detección por proximidad (`Vector3.Distance <= 3.2m`). Al entrar en rango muestra el aviso flotante inferior `[E] Examinar libreta`. Al presionar 'E', desactiva temporalmente `PlayerController3D`, silencia la cordura y despliega el panel modal.
* **Modal de Lectura (`NotebookPanelUI`):** Panel semitransparente oscuro de 2 páginas con TextMeshPro. Página 1 narra el prólogo íntimo de Karol; Página 2 instruye la búsqueda de recuerdos (0/3). Avance con [Espacio]/[E] y cierre con [Esc] o botón 'X'. Al cerrar, devuelve el control del jugador, reactiva el decremento de cordura, restaura +15 puntos de cordura de alivio y actualiza el objetivo a `"Objetivo: Explora la calle y busca tus recuerdos (0/3)"`.

### 3.9. Álbum de Recuerdos y Flashbacks 2D (Fase 3 y 4)
* **Datos de Recuerdos (`MemoryData`):** ScriptableObject con `memoryId`, `title`, `photo` (Sprite), `flashbackLines` y `sanityReward` (25 puntos por defecto). Ubicados en `Assets/Data/Memories` (`Memory_01_Telefono`, `Memory_02_Foto`, `Memory_03_Nota`).
* **Puntos de Recolección (`MemoryPickup`):** 3 objetos en la calle (`MEMORY_PICKUPS`) con luz puntual senoidal suave y detección por distancia (`<= 3.0m`). Despliegan `[E] Recoger recuerdo`, registran el hallazgo en el álbum, se desactivan y lanzan el flashback.
* **Álbum Polaroid (`MemoryAlbumManager` / `MemoryAlbumModal`):** Panel de 3 ranuras Polaroid en `SanityHUD`, accesible con `Tab` o `I` (pausa movimiento y cordura). Recuerdos bloqueados se muestran con "?" y marco oscuro; al desbloquearlos muestran su foto y título, y permiten reproducir nuevamente su memoria al hacer clic.
* **Cinemática Flashback 2D (`MemoryFlashbackUI` / `FlashbackCanvas`):** Canvas de prioridad 110 con efecto Ken Burns (zoom suave 1.0 a 1.08) y máquina de escribir con avance por `Espacio`/Clic y salto por `Esc`. Al terminar por primera vez, recompensa +25 de cordura y actualiza el contador. Al completar los 3 recuerdos (3/3), actualiza `ObjectiveIndicator` hacia `Puerta_Transicion_SegundoPlano` con el texto `"Objetivo: Cruza la puerta hacia El Departamento"`.

---

## 4. ESTÁNDARES DE ESCENA E ILUMINACIÓN (UNITY 6 URP)
* **Datos de Iluminación:** Prohibido vincular archivos `LightingData.asset` obsoletos de versiones previas. Toda escena limpia debe tener `m_LightingDataAsset: {fileID: 0}` y `Realtime Lightmaps` desactivado a menos que se regenere con el Bake backend de Unity 6.
* **Presupuesto de Sombras Urbanas:** En escenas abiertas con alta densidad de farolas puntuales (`Punctual Lights`), la luz direccional no debe saturar el atlas de sombras con modos pesados que superen la capacidad de URP.