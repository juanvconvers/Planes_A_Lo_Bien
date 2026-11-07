# Plan (checklist de fases)

- [x] **F0 Cimientos** — estructura, asmdefs, ServiceLocator, ExternalIds, instalador de paquetes, setup Android, build AAB, docs, verificación de nombre.
- [x] **F1 Núcleo lógico** — tablero, catálogo, puntuación, rachas, fin, FairBag, deshacer, guardado con checksum, test de arquitectura, simulación 10.000 partidas, cobertura ≥ 90 %.
- [x] **F2 Clásico jugable** — pantalla de juego, arrastre elevado, sombra con imán, resaltado, animaciones, HUD, háptica, audio, resultados con semilla, reanudar; test PlayMode 300 jugadas *(pendiente de ejecutar en Unity)*.
- [x] **F3 Identidad visual** — tokens, fuentes OFL, 9-slice, grano, partículas, transiciones, microcopy es/en/pt, accesibilidad, Juego limpio, Créditos; tests de claves y contraste WCAG AA.
- [x] **F4 Menús y flujo** — Boot → partida-tutorial → consentimiento → Inicio; Atrás de Android; ajustes persistentes; test PlayMode de pantallas *(pendiente de ejecutar)*.
- [x] **F5 Modos** — Restauración (formato, objetivos, editor, validador, 60 niveles ≥ 70 %), Reto diario, Zen, eventos remotos.
- [x] **F6 Meta** — XP, misiones, racha con protector, cofres con probabilidades, colecciones, logros, notificaciones; simulador de economía 30 días.
- [x] **F7 Monetización** — interfaces + fakes, IAP, tienda, restaurar, todos los productos, reglas de anuncios testeadas, banner, CMP, ofertas por segmento.
- [x] **F8 Servicios** — Firebase (Analytics, Crashlytics, Remote Config), Play Games, In-App Review/Updates, notificaciones; todo con valores por defecto locales.
- [~] **F9 Pulido y QA** — borradores legales y de ficha, firma de release, QA_PENDIENTE, rendimiento por diseño. **Pendiente (requiere Unity):** perfilado en dispositivo y AAB de release.
- [x] **F10 (preparada)** — ASSET_MANIFEST, GEMINI_PROMPTS, remove_bg.py, importador ArtDrop con informe e interruptor en Theme.
