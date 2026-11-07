# Bitácora

## 2026-11-07
- **F0** Estructura Unity, asmdefs, `ExternalIds.asset` con `TODO_ID_*`, instalador de paquetes, setup Android, build AAB, docs, nombre (D-01).
- **F1** Lógica pura (`Core`, `Gameplay`). Simulación 10.000 partidas sin fines falsos. Cobertura Gameplay 98 %.
- **F6/F7 (lógica)** Meta, economía (ajustada con el simulador, D-15) y monetización pura con tests.
- **F2–F4** Arte y audio procedurales, servicios Unity, UI completa por código, localización es/en/pt, tests de claves y contraste.
- **F5** Validador + generador; 60 niveles validados; editor de niveles; simulador de economía en el editor.
- **F8** Adaptadores Firebase / Play Games / Review / Updates / Notificaciones (protegidos por defines) y valores locales.
- **F9** Docs de privacidad, Seguridad de los datos, ficha (es/en/pt), firma, rendimiento, QA final.
- **F10** Manifiesto de arte, prompts de Gemini, `remove_bg.py`, importador ArtDrop.
- Verificación: `tools/check.sh` → 113 tests EditMode en verde + capa Unity compilando contra referencias.

**Siguiente paso (requiere al dueño):** abrir en Unity 6.3, seguir `QA_PENDIENTE.md`, cargar IDs en `ExternalIds.asset`,
generar el AAB firmado y luego el arte con Gemini (`GEMINI_PROMPTS.md`) → `ArtDrop/` → Fase 10.

Tags locales a recrear al integrar en `main` (D-02): `v0.1` (F0+F1), `v0.4` (F2–F4), `v0.5` (F5), `v0.9` (F6–F9), `v1.0-rc` tras la Fase 10.
