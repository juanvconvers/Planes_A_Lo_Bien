# Tesela

Block puzzle original para Android (Unity 6.3 LTS): azulejos de cerámica esmaltada, un patio de 8×8 y mosaicos que se
restauran con cada línea. *El block puzzle justo: sin trampas, sin anuncios en tu cara.*

- Especificación: [`docs/PROMPT_MAESTRO.md`](docs/PROMPT_MAESTRO.md) · Estado: [`docs/PROGRESS.md`](docs/PROGRESS.md) · Decisiones: [`docs/DECISIONS.md`](docs/DECISIONS.md)
- Qué probar en el teléfono: [`docs/QA_PENDIENTE.md`](docs/QA_PENDIENTE.md) · Juego limpio: [`docs/FAIR_PLAY.md`](docs/FAIR_PLAY.md)
- Arte final con Gemini: [`docs/GEMINI_PROMPTS.md`](docs/GEMINI_PROMPTS.md) → `ArtDrop/` → **Tesela > Importar arte de ArtDrop**

## Abrir
1. Unity Hub → Unity 6.3 LTS con Android Build Support → abrir esta carpeta. Los paquetes se instalan solos la primera vez.
2. `Tesela > Setup > Configurar proyecto Android`.
3. Play en cualquier escena: la app se monta sola (`TeselaApp`).

## Verificar sin Unity
```bash
tools/check.sh          # tests EditMode (.NET 8), cobertura, compilación de la capa Unity, simulaciones
```
