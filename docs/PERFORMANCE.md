# Rendimiento (Fase 9)

Metas de la Parte 4: 60 fps en gama media (3 GB), AAB ≤ 50 MB, arranque en frío ≤ 3 s, cero asignaciones en el bucle de juego.

## Lo que está hecho por diseño
- **Lógica de juego sin asignaciones por jugada:** tablero con máscaras de bits por fila; `GameSession` reutiliza `MoveResult`
  y sus listas; deshacer usa 3 instantáneas preasignadas (`Board.CopyTo`); `FairBag` usa un arreglo de pesos fijo.
- **Tweens sin asignaciones por frame:** `Motion` usa un arreglo de structs reutilizado (las lambdas de los callbacks sí asignan al
  crear cada animación, no por frame).
- **Partículas con pooling** (`UiParticles`): imágenes reutilizadas.
- **Un atlas** para todos los sprites procedurales → un draw call por capa; canvas anidados separan casillas (estáticas),
  azulejos, efectos y arrastre (dinámicos).
- **Arranque:** el arte procedural y el audio se calculan en hilos aparte mientras se muestra el fondo; no hay escenas pesadas
  ni assets grandes (fuentes ≈ 770 KB; sin texturas ni audio importados mientras no llegue el arte de la Fase 10).
- **Tamaño:** sin texturas ni audio pregrabado; el AAB base debería quedar muy por debajo de 50 MB (lo dominan el motor y los SDK).
- **16 KB:** el proyecto no incluye librerías nativas propias. Unity 6.3 y las versiones actuales de los SDK publican `.so`
  alineadas a 16 KB; verificar con el informe de Play Console.

## Lo que queda por medir en dispositivo (QA_PENDIENTE.md)
- Profiler en un Android de gama media: fps estables en una partida de 5 min, GC Alloc por frame en la escena de juego.
  Fuentes conocidas de asignaciones pequeñas en UI: `Loc.Number` (cadena por cambio de puntuación), cierres de `Motion.Value`
  al animar la puntuación. Si aparecen picos, cachear cadenas de números 0–9999 y reutilizar delegados.
- Tiempo de arranque en frío (logcat `Displayed`), objetivo ≤ 3 s.
- Tamaño del AAB base en el informe de build.
