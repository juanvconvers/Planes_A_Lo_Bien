# Prompt maestro — Tesela (block puzzle para Android, Unity)

> Transcripción de las Partes 1 a 6 del documento "Prompt maestro — Juego block puzzle para
> Android (Unity)" (Juan David Velasquez, 7 nov 2026). Es la especificación de referencia del
> proyecto. Si el contexto se reinicia: lee `CLAUDE.md` y `docs/PROGRESS.md` y continúa.

## Parte 1 — Rol y reglas de trabajo

**Rol.** Equipo completo de un estudio móvil (director de juego, programador senior Unity/C#,
UI/UX, economía, audio y QA). Construir **Tesela**, un block puzzle original para Android en
Unity 6.3 LTS, inspirado solo en las reglas del género y mejorado con lo que piden los
jugadores. Trabajo continuo por fases hasta cerrar la Fase 9 y dejar preparada la Fase 10.

### Reglas de trabajo continuo
1. No detenerse a pedir pruebas, capturas ni confirmaciones. Al cerrar una fase, empezar la siguiente.
2. Solo detenerse ante una acción irreversible fuera del repo. Si falta un ID externo, escribir
   `TODO_ID_<NOMBRE>` en `Assets/_Tesela/Config/ExternalIds.asset` y seguir.
3. Mantener `CLAUDE.md`, `docs/PLAN.md`, `docs/PROGRESS.md`, `docs/DECISIONS.md`, `docs/QA_PENDIENTE.md`.
4. Git: una rama por fase (`fase-N-nombre`), commits pequeños en español, merge a `main`, tag `v0.N`. Nunca reescribir historial.
5. Verificar sin parar: compilar en batch si Unity está disponible y correr tests EditMode. Si Unity no está, anotarlo en QA_PENDIENTE.md.
6. Tests EditMode para la lógica pura (tablero, generador, puntuación, economía).
7. Código: namespaces `Tesela.*`, un asmdef por módulo (Core, Gameplay, UI, Meta, Monetization,
   Services, Editor, Tests). Lógica pura sin MonoBehaviour; MonoBehaviours delgados. Datos en
   ScriptableObjects y JSON. Comentarios solo para explicar un "por qué". Sin código muerto ni logs en release.
8. Contenido actualizable sin recompilar: niveles, eventos, ofertas y parámetros de anuncios en JSON, Remote Config y Addressables.
9. Si una librería no instala, usar la alternativa de la Parte 4 o un reemplazo mínimo propio.

### Propiedad intelectual (obligatorio)
- Solo reglas de género: tablero cuadriculado, colocar poliominós, limpiar filas y columnas. No copiar expresión de ningún juego.
- Prohibido usar nombres de juegos/estudios competidores en textos, ficha, palabras clave, metadatos o código.
- No usar el aspecto de gemas brillantes sobre azul marino ni el "look" de piezas de 4 cuadros con colores canónicos.
- Solo assets con licencia comercial clara (CC0, MIT, Apache 2.0, OFL), registrados en `THIRD_PARTY_NOTICES.md` y en Ajustes > Créditos.
- Fase 0: verificar el nombre "Tesela" en Google Play; el dueño revisa WIPO y SIC Colombia. Si choca, proponer 5 nombres en DECISIONS.md.

### Juego limpio y ética (obligatorio)
- El generador de piezas nunca recibe datos de anuncios, gasto, país, dispositivo ni valor del jugador.
  Por arquitectura: `Tesela.Gameplay` no referencia `Tesela.Monetization`; un test lo verifica por reflexión.
- Dificultad depende solo del tablero y del avance de la partida. Reglas en `docs/FAIR_PLAY.md` y pantalla "Juego limpio".
- Sin patrones oscuros (cierres escondidos/retrasados, cuentas regresivas falsas, "gratis" falso, precios ambiguos, frases de culpa, anuncios disfrazados).
- Sin cajas de botín pagadas con dinero real; cofres solo con moneda ganada jugando y con probabilidades visibles.
- Público 13+. Recordatorio opcional de sesión larga.
- Cumplir políticas de Google Play y de uso de Anthropic.

## Parte 2 — Concepto, arte y diseño de juego

**Concepto.** Block puzzle sobre un patio de baldosas: cada pieza es un grupo de azulejos de
cerámica esmaltada pintados a mano. Al limpiar líneas, los azulejos se desprenden y restauran los
mosaicos de un pueblo ficticio (plaza, fuente, capilla, mercado, estación). Lema: *"El block
puzzle justo: sin trampas, sin anuncios en tu cara, y cada línea restaura algo bello."*

**Dirección de arte.**
- Paleta (`Theme.asset`): arena `#F2E6D8`, juntas `#8C7B6B`, texto índigo `#23305C`, oro `#E8B64C`.
  Esmaltes: cobalto `#2F5DA8`, ocre `#D99A2B`, oliva `#6E8B3D`, coral `#E0654F`, turquesa `#2A9D8F`, ciruela `#7B3F61`.
- Doble código: patrón por esmalte (estrella, flor, rombo, onda, punto, cruz).
- Azulejos procedurales hasta la Fase 10: rectángulo redondeado con bisel, brillo de esmalte, craquelado sutil, variación por baldosa de tono (±3%) y giro del patrón.
- Tipografía (OFL): Fraunces (títulos y números grandes), Bricolage Grotesque (interfaz). Prohibidas Inter, Roboto, Poppins, Montserrat, Arial.
- Evitar señales de plantilla/IA: degradados morado-azul, glassmorphism, emoji, sombras difusas, mismo radio en todo, botones píldora idénticos, simetría perfecta, textos de relleno.
- En su lugar: marcos 9-slice con borde de cerámica algo irregular, sombras duras y cortas con luz arriba-izquierda, grano de terracota 3–5 %, asimetría intencional, microcopy de taller ("¡Junta perfecta!", "Mosaico completo"). Idiomas: es, en, pt.
- Movimiento: curvas propias 120–320 ms; squash & stretch leve; azulejos limpiados saltan en arco al contador del mosaico. "Reducir movimiento" apaga lo no esencial.
- Sonido: clics de cerámica, toques de esmalte, combo con escala pentatónica ascendente, música acústica (guitarra y marimba) con capas que crecen con la racha.
- Iconos: un solo set, mismo grosor de trazo, reemplazable en la Fase 10.

**Núcleo de juego.**
- Tablero 8×8 y bandeja de 3 piezas; al colocar las tres llegan tres nuevas. Sin rotación en Clásico.
- Catálogo: piezas de 1 a 9 celdas (líneas 2–5, cuadrados 2×2 y 3×3, esquinas, trominós, tetrominós y pentominós elegidos). Pesos en JSON.
- Puntuación (`ScoringConfig.json`): 1 punto por celda; líneas simultáneas 1→10, 2→30, 3→60, 4→100 o más. Racha: ×1, ×1,5, ×2, tope ×4; se rompe tras 3 jugadas sin limpiar. Tablero vacío +300.
- Fin de partida solo cuando ninguna pieza de la bandeja cabe (verificación exhaustiva + tests).
- Generador justo `FairBag`: RNG determinista con semilla (PCG), semilla visible; bolsa por pesos sin más de 2 piezas iguales seguidas; cada bandeja trae al menos una pieza que cabe; pesos de piezas grandes suben con la puntuación según tabla pública igual para todos. "Modo relajado": las tres piezas siempre caben.
- Control táctil: pieza elevada sobre el dedo (0–3 celdas), crece a tamaño real; sombra con imán; resaltado de líneas; vibración leve; si se suelta fuera, vuelve con rebote. Modo zurdo.
- Guardado completo tras cada jugada (JSON con checksum). "Continuar partida".

**Modos.** 1) Clásico. 2) Restauración: capítulos por lugar; objetivos (recolectar color, baldosas agrietadas de dos limpiezas, piezas bajo barro, puntuación en N jugadas); niveles JSON con tablero inicial y semilla; validador (greedy + Monte Carlo) rechaza < 70 % de resolución; 3 estrellas; álbum; lanzamiento 60 niveles en 3 capítulos. 3) Reto diario: semilla por fecha UTC, clasificación, racha con "Protector de racha". 4) Zen: si nada cabe, barrido limpia la fila más llena; sin récord. 5) Eventos semanales (Fase 8): giro libre, 9×9 con regiones 3×3, bombas con cuenta, piezas doradas.

**Potenciadores.** Deshacer, Reserva, Girar (90°), Martillo, Escoba (fila o columna) y Nueva bandeja. Cargas gratis diarias, se ganan jugando, también se compran.

**Meta y retención.** Nivel de jugador por XP; 3 misiones diarias, semanales y cofre de racha de 7 días; colecciones; logros con barra de progreso (Play Games); notificaciones locales con permiso (máx. 1/día); primera partida en < 10 s; tutorial de 3 pasos.

## Parte 3 — Monetización híbrida

Meta ≈ 45/55 compras/anuncios. Todo valor en Remote Config, con A/B sin recompilar.

**Anuncios (LevelPlay + AdMob).**
- Con premio (opcionales): Revivir 1 vez por partida (barre las 2 filas más llenas y da bandeja nueva), duplicar monedas, potenciador gratis (tope 5/día), doble cofre del reto diario. Tope total inicial 20/día.
- Intersticiales: solo en la transición resultados → menú o nueva partida. Nunca en la primera sesión ni en las 3 primeras partidas, nunca al iniciar nivel ni a mitad de partida, nunca en los 120 s siguientes a un anuncio con premio. Espera inicial 150 s y 2 partidas. Cero intersticiales 24 h tras cualquier compra.
- Banner adaptativo solo en menús; oculto en Zen y para "Sin anuncios"/Club.
- Sin anuncio de apertura.
- Consentimiento: CMP certificada con IAB TCF v2.3 antes de cargar anuncios, sin bloquear el tutorial. Opción "Privacidad".
- A prueba de fallos: llamadas envueltas, límite 5 s; si no hay video, alternativa con monedas.

**Compras (Unity IAP / Play Billing).** Sin anuncios (no consumible, 3,99, +3 protectores);
Pack inicial (48 h, 1,99, 500 monedas + 5 potenciadores + tema "Cobalto antiguo"); Hucha de
azulejos (2,99); Pase de temporada (28 días, 4,99); Pase Plus (9,99, +10 niveles, sin anuncios
en la temporada); Club Tesela (suscripción 4,99: sin anuncios, 100 monedas/día, 1 potenciador
diario de cada tipo, temas, ×1,5 XP del pase); Monedas (0,99–99,99, 6 niveles, bono 0–50 %);
Packs de potenciadores (1,99–9,99); Cosméticos (1,99–3,99 o monedas). IDs en `Config/Products.json`.

**Ofertas por segmento.** Segmentos en dispositivo: nuevo (0–3 días), no pagador comprometido,
primer pagador, recurrente, gran comprador (≥ 50 USD), en riesgo. Reglas en Remote Config.
Máx. una ventana por sesión, nunca antes de la 2.ª partida, solo en momentos de logro.

**Economía.** Monedas: puntuación/50 con tope, misiones, logros, pase y anuncios; gastos: Girar 80,
Deshacer 100, Martillo 120, Nueva bandeja 150, Escoba 200, Revivir 300, Protector 250, temas
2.000–5.000. Azulejos: solo jugando, no se venden. `Economy.json` + simulador.

**Analítica.** Firebase Analytics, Crashlytics, Remote Config, A/B. Eventos: `run_start`, `run_end`,
`level_*`, `booster_use`, `ad_offer`/`ad_show`/`ad_reward`, `iap_view`/`iap_purchase`,
`offer_show`/`offer_accept`/`offer_dismiss`, `tutorial_step`, `pass_level`, `streak_update`.
Metas: D1 ≥ 40 %, D7 ≥ 15 %, D30 ≥ 6 %; sesiones sin cierres ≥ 99,5 %.

**Cumplimiento.** Intersticiales en pausas naturales y cerrables en ≤ 15 s; suscripción con
precio, periodo y cancelación visibles; Restaurar compras; política de privacidad, Seguridad de
los datos y borrado de datos desde Ajustes.

## Parte 4 — Stack técnico

Unity 6.3 LTS, URP 2D, IL2CPP, ARM64, AAB. `targetSdk 36`, `minSdk 24`. Librerías nativas con
páginas de 16 KB. Interfaz uGUI + TextMeshPro; tweens LitMotion (o PrimeTween); partículas
ParticleEffectForUGUI; UIEffect; UniTask; RichHaptic (o JNI propio de VibrationEffect); Input
System; Addressables + Remote Config; Unity Localization (o tablas JSON propias); Play Games;
Unity IAP; LevelPlay + AdMob; Firebase; Mobile Notifications; fuentes OFL; iconos CC0; audio CC0
(o sintetizado); azulejos con shader procedural propio. No usar la integración antigua de Unity Ads.

Rendimiento: 60 fps en gama media 3 GB, AAB ≤ 50 MB, arranque en frío ≤ 3 s, cero asignaciones en
el bucle de juego, canvas estático/dinámico separados, atlas.

## Parte 5 — Fases
- **F0 Cimientos.** Proyecto, paquetes, asmdefs, ServiceLocator, escena Boot, ExternalIds, docs, verificación de nombre. Listo: compila en batch y un test vacío pasa.
- **F1 Núcleo lógico puro.** Board, PieceCatalog, colocación, limpieza, puntuación, rachas, fin, FairBag PCG, deshacer, serialización con checksum. Test de arquitectura. Simulación de 10.000 partidas (bot aleatorio y greedy) → `docs/SIM_REPORT.md`. Listo: cobertura Gameplay ≥ 90 % y simulación pasa.
- **F2 Clásico jugable.** Escena de juego, arrastre con elevación, sombra, resaltado, animaciones, HUD, vibración, audio. Resultados con semilla; reanudar. Listo: test PlayMode de 300 jugadas y guardado/reanudación.
- **F3 Identidad visual.** Theme, fuentes, 9-slice, grano, partículas, transiciones, microcopy es/en/pt. Accesibilidad. Pantallas "Juego limpio" y Créditos. Listo: test de claves de traducción y contraste WCAG AA.
- **F4 Menús y flujo.** Boot → consentimiento (simulado) → Inicio. Navegación con Atrás, tutorial, ajustes persistentes. Listo: test PlayMode que recorre pantallas.
- **F5 Modos.** Restauración (formato, objetivos, editor, validador, 60 niveles, estrellas, álbum), Reto diario, Zen, marco de eventos. Listo: 60 niveles ≥ 70 % y test PlayMode por modo.
- **F6 Meta.** XP, misiones, racha con protector, cofres con probabilidades, colecciones, logros, notificaciones. `Economy.json` + simulador 30 días. Listo: sin inflación.
- **F7 Monetización.** `IAdService`, `IStoreService` con fakes; IAP, tienda, restaurar, productos; LevelPlay, reglas de intersticiales, banner, CMP, ofertas. Listo: tests de cada regla y entrega.
- **F8 Servicios y live-ops.** Firebase, Remote Config con defaults locales, A/B; Play Games; In-App Review/Updates; Addressables remotos. Listo: arranca y se juega sin red ni IDs.
- **F9 Pulido y QA.** Perfilado, 16 KB, bot de humo 200 jugadas por modo, borradores de privacidad/ficha, firma de release, QA_PENDIENTE final. Listo: AAB de release y tests en verde.

## Parte 6 — Fase 10: arte final con Gemini
1. `docs/ASSET_MANIFEST.json` (id, uso, ruta, tamaño, alfa, pivote, bordes 9-slice, prioridad; icono 512×512 y gráfico 1024×500).
2. `docs/GEMINI_PROMPTS.md` con bloque de estilo maestro + un prompt por id, al doble de tamaño.
3. Transparencia: fondo croma `#00FF00` (o `#FF00FF`), `tools/remove_bg.py` (rembg) e importador que quite el croma.
4. Coherencia: primero hoja de estilo con los 6 azulejos, adjunta como referencia.
5. Modelos: Nano Banana Pro (protagonistas) y Nano Banana 2 (volumen).
6. Menú `Tesela > Importar arte de ArtDrop`: valida, configura importación, reemplaza en Theme y atlas, mantiene lo procedural si falta, genera `docs/ART_SWAP_REPORT.md`.
7. Interruptor procedural/textura en `Theme.asset`. Cerrar con tag `v1.0-rc`.

**Definición de terminado.** F0–F9 con tags, tests en verde y AAB; F10 lista; sin terceros sin
licencia; juego limpio verificado por test; funciona sin red ni IDs; QA_PENDIENTE listo; mensaje
final con resumen, IDs faltantes y 3 riesgos.
