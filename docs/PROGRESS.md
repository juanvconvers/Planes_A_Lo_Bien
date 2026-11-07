# Bitácora

## 2026-11-07
- **F0** Estructura del proyecto Unity, asmdefs por módulo, `ExternalIds.asset` con marcadores `TODO_ID_*`,
  `PackageSetup` (paquetes por nombre + TMP Essentials), `ProjectSetup` (targetSdk 36, minSdk 24, IL2CPP, ARM64, AAB,
  escena Boot), `BuildAndroid` (AAB firmado por variables de entorno), docs y avisos de terceros. Nombre verificado (D-01).
- **F1** Lógica pura en `Tesela.Core` y `Tesela.Gameplay`. 62 tests EditMode en verde con .NET 8 (cobertura Gameplay 98,3 %).
  Simulación de 10.000 partidas por bot: cero fines falsos (`docs/SIM_REPORT.md`).

Tags locales pendientes de recrear al integrar en `main` (D-02): `v0.0`, `v0.1`.
