# CLAUDE.md — Tesela

Block puzzle original para Android (Unity 6.3 LTS). Especificación completa: `docs/PROMPT_MAESTRO.md`.
**Si el contexto se reinicia:** lee este archivo y `docs/PROGRESS.md`, y continúa donde ibas.

## Reglas resumidas
- Trabajo continuo por fases (0→9, Fase 10 preparada). No pedir pruebas ni confirmaciones.
- Solo parar ante acciones irreversibles fuera del repo. IDs externos faltantes → `TODO_ID_<NOMBRE>`
  en `Assets/_Tesela/Config/Resources/ExternalIds.asset`.
- Mantener al día `docs/PLAN.md`, `docs/PROGRESS.md`, `docs/DECISIONS.md`, `docs/QA_PENDIENTE.md`.
- Commits pequeños en español. Nunca reescribir historial.
- Lógica pura en C# sin MonoBehaviour (asmdefs `noEngineReferences`); MonoBehaviours delgados.
- **Juego limpio:** `Tesela.Gameplay` nunca referencia `Tesela.Monetization` (test por reflexión y por asmdef).
- **IP:** nada de nombres de juegos/estudios competidores en código, textos ni metadatos. Solo assets CC0/MIT/Apache/OFL,
  registrados en `THIRD_PARTY_NOTICES.md`.
- Fuentes permitidas: Fraunces y Bricolage Grotesque. Paleta en `Theme.asset` / `Tesela.Core.Palette`.

## Módulos (asmdef → carpeta)
| asmdef | Carpeta | Motor |
|---|---|---|
| Tesela.Core | Scripts/Core | puro (`noEngineReferences`) |
| Tesela.Gameplay | Scripts/Gameplay | puro |
| Tesela.Meta | Scripts/Meta | puro |
| Tesela.Monetization | Scripts/Monetization | puro (interfaces, reglas, fakes) |
| Tesela.Services | Scripts/Services | Unity (guardado, audio, háptica, analítica, adaptadores SDK) |
| Tesela.UI | Scripts/UI | Unity (uGUI procedural, vistas, flujo) |
| Tesela.Editor | Scripts/Editor | Editor (build, paquetes, editor de niveles, importador de arte) |
| Tesela.Tests.EditMode / PlayMode | Tests/ | NUnit |

Los SDK opcionales (IAP, LevelPlay, Firebase, Play Games, Notifications, Input System) se activan con
`versionDefines` de los asmdef: el proyecto compila sin ellos y usa implementaciones falsas.

## Verificación sin Unity
`tools/check.sh` compila los módulos puros con .NET 8, corre los tests EditMode con NUnit (cobertura con
coverlet), compila la capa Unity contra ensamblados de referencia + stubs (`tools/dotnet/UnityStubs`), y
regenera niveles/simulaciones. Úsalo tras cada bloque de código.

## Comandos Unity (cuando esté disponible)
- Compilar: `Unity -batchmode -nographics -quit -projectPath . -logFile Logs/build.log`
- Tests: `Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml`
- AAB: `Unity -batchmode -nographics -quit -projectPath . -executeMethod Tesela.EditorTools.BuildAndroid.BuildReleaseAab`
