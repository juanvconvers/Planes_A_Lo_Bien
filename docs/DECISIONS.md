# Decisiones

Formato: **D-NN — título.** Contexto → decisión → consecuencia.

## Fase 0

**D-01 — Verificación del nombre "Tesela".** Desde este entorno no se puede abrir Google Play
(el proxy bloquea play.google.com). Una búsqueda web (7 nov 2026) no encontró ningún juego llamado
"Tesela" en Google Play; lo más cercano fue *Tessella* (editor de teselados en F-Droid, no un juego),
*Teslagrad* (plataformas) y la app de Tesla. → Se mantiene **Tesela** como nombre de trabajo.
**Pendiente del dueño:** buscar "Tesela" en Google Play desde un teléfono, revisar WIPO Global Brand
Database (clases 9 y 41) y la SIC de Colombia. Si choca, alternativas originales en orden de fuerza:
1. **Azulejo Vivo** 2. **Patio de Esmalte** 3. **Junta Fina** 4. **Taracea** 5. **Mosaiquero**.
El nombre visible sale de la clave de localización `app.name`, así que cambiarlo es un cambio de datos.

**D-02 — Git: rama única de sesión en lugar de una rama por fase.** El entorno de ejecución solo permite
publicar en la rama `claude/new-session-3045e6`. → Cada fase se cierra con un commit "Fase N cerrada" y
un tag local `v0.N`; no se crean ramas `fase-N-*` remotas ni se empuja a `main`. Al integrar, el dueño
puede hacer merge de la rama a `main` y recrear los tags (lista en PROGRESS.md).

**D-03 — Unity no está disponible en el entorno.** No hay editor de Unity ni acceso al registro de
paquetes de Unity. → Toda la lógica de juego vive en ensamblados puros (`noEngineReferences`) que se
compilan y testean con .NET 8 + NUnit (`tools/check.sh`). La capa Unity se compila contra los
ensamblados de referencia de UnityEngine (NuGet `UnityEngine.Modules`) y stubs propios de uGUI,
TextMeshPro e Input System. Lo que solo puede verificarse en el editor/dispositivo va a QA_PENDIENTE.md.

**D-04 — Paquetes opcionales por `versionDefines`.** Si un paquete o SDK falta, el proyecto no debe
dejar de compilar (si no compila, no corre ni el instalador de paquetes). → `manifest.json` lleva solo
paquetes base seguros; `Tesela > Setup > Instalar paquetes` (y un `[InitializeOnLoad]` la primera vez)
añade los demás por nombre con `Client.AddAndRemove`, de modo que Unity elige la versión compatible.
El código que usa IAP, LevelPlay, Firebase, Play Games, Notifications o Input System se compila solo si
su define existe (`TESELA_IAP`, `TESELA_LEVELPLAY`, `TESELA_FIREBASE`, `TESELA_PGS`,
`TESELA_NOTIFICATIONS`, `TESELA_INPUTSYSTEM`). Sin ellos se usan implementaciones falsas.

**D-05 — Reemplazos mínimos propios en lugar de LitMotion, UniTask, UIEffect, ParticleEffectForUGUI y
RichHaptic.** No se puede verificar la instalación por git/OpenUPM desde aquí, y un paquete que no
resuelve bloquea la apertura del proyecto. La regla 9 permite reemplazos mínimos. → `Tesela.UI.Motion`
(tweens sin asignaciones con curvas propias), corrutinas, partículas de UI con pooling de `Image`, y
háptica por JNI de `VibrationEffect`. Ningún código de terceros en runtime.

**D-06 — Azulejos procedurales rasterizados en C# en lugar de Shader Graph.** Un `.shadergraph` no se
puede escribir ni revisar con fiabilidad sin el editor, y un shader que falla en una GPU móvil deja el
tablero en rosa. → `Tesela.Core.Raster.TileRasterizer` genera las texturas (rectángulo redondeado,
bisel, brillo de esmalte, craquelado por ruido, patrón por esmalte) en el arranque; la variación por
baldosa (±3 % de tono, giro del patrón en múltiplos de 90°) se aplica por instancia con color de
vértice y rotación. Es lógica pura y está testeada. El interruptor `useTextures` de `Theme.asset`
cambia a texturas de ArtDrop en la Fase 10. Todas las baldosas comparten atlas → un draw call por capa.

**D-07 — Audio sintetizado.** No hay acceso a Kenney/Freesound/OpenGameArt desde el entorno. → Efectos
(clics de cerámica, toques de esmalte, combo pentatónico) y música (guitarra por Karplus-Strong y
marimba por síntesis modal, capas por racha) se sintetizan en `Tesela.Core.Audio`. 100 % propio.

**D-08 — Una escena (Boot) con pantallas construidas por código.** Las escenas y prefabs hechos a mano en
YAML sin editor son frágiles (GUIDs). → `Boot.unity` está vacía; `TeselaApp` se crea con
`RuntimeInitializeOnLoadMethod` y construye la UI de forma procedural (uGUI). "Main" y "Game" son
pantallas del mismo canvas, lo que además hace instantáneas las transiciones.

**D-09 — Texto con TextMeshPro y respaldo.** TMP necesita "TMP Essential Resources". → El instalador de
paquetes las importa; `UiText` usa TMP si `TMP_Settings` existe y, si no, `UnityEngine.UI.Text` con la
fuente dinámica, para que nunca quede texto invisible.

**D-10 — Localización con tablas JSON propias** (alternativa permitida en la Parte 4) en
`Config/Resources/Loc/{es,en,pt}.json`; un test verifica que no falte ninguna clave.

**D-11 — Identificador de la app** `com.jdvelasquez.tesela` (cambiable en `ExternalIds.asset` →
`androidPackageName`; el script de build lo aplica).

## Fases 1–9

**D-12 — Racha.** La tabla ×1, ×1,5, ×2… tope ×4 se implementa como +0,5 por cada jugada consecutiva que limpia
(`streakMultipliers` en `ScoringConfig.json`). La racha sobrevive a 2 jugadas sin limpiar y se rompe en la 3.ª.

**D-13 — Reserva.** El potenciador Reserva gasta una carga para desbloquear la casilla durante la partida; después, guardar o
intercambiar es gratis. Por eso la detección de fin de partida también mira la pieza de la reserva.

**D-14 — Deshacer.** Guarda hasta 3 jugadas. Los demás potenciadores borran el historial de deshacer (no se puede "deshacer un
martillo" y recuperar la carga).

**D-15 — Economía.** El primer `Economy.json` inflaba (34.000 monedas al día 30 para el jugador típico). Se redujeron recompensas
de misiones, tope por partida (40), sets de azulejos (300 azulejos → 100 monedas), XP del pase (450 por escalón) y logros. Rango
sano decidido: **300–4.000 monedas** al día 30 para el jugador típico, que compra temas al poder. Resultado: 3.542
(`docs/ECONOMY_REPORT.md`). El jugador "intenso" (12 partidas/día) acumula más: es intencional, compra todo lo cosmético antes.

**D-16 — Validador de niveles.** El bot del validador persigue los objetivos (bonifica líneas con agrietadas, barro o el esmalte
buscado), como haría una persona. El Monte Carlo usa ruido moderado. Los niveles se ajustan para ≥ 80 % y se exige ≥ 70 % en el
test con 60 partidas.

**D-17 — Consentimiento.** Se pide después de la primera partida (al volver a Inicio o al empezar la segunda), nunca durante el
tutorial. Los anuncios no se inicializan hasta entonces. Con el plugin de Google Mobile Ads se usa UMP (CMP certificada, TCF v2.3);
sin él, el paso informa y continúa (anuncios simulados).

**D-18 — Intersticiales.** La espera inicial de 150 s cuenta desde el inicio de la sesión, y la regla de "2 partidas entre uno y
otro" se aplica desde el primero mostrado. Con las 3 primeras partidas y la primera sesión bloqueadas, el primer intersticial
posible es en la 2.ª sesión tras ≥ 3 partidas totales.

**D-19 — Ofertas.** Solo en momentos de logro (capítulo completo, récord, escalón de pase) o regalo de regreso para jugadores en
riesgo. Nunca tras perder. Si se muestra una oferta, ese tránsito no muestra intersticial.

**D-20 — Guardado en nube de Play Games** queda fuera de esta versión (requiere la API de partidas guardadas y resolución de
conflictos real). El juego guarda localmente con escritura atómica + copia .bak + checksum.

**D-21 — Chequeo de compilación local.** La capa Unity se compila contra UnityEngine 2021.3 y UnityEditor 2021.1 (referencias
NuGet) más stubs de uGUI/TMP/TestRunner. Se evitan APIs exclusivas de Unity 6 salvo en `ProjectSetup.cs` (excluido del chequeo
por usar `NamedBuildTarget`). La instalación de paquetes usa `Client.Add` uno a uno (compatible con todas las versiones).
