# Avisos de terceros

Tesela usa únicamente recursos con licencia comercial clara. Esta lista se muestra también en
**Ajustes > Créditos** (fuente: `Assets/_Tesela/Config/Resources/Credits.json`).

| Recurso | Autor | URL | Licencia | Uso |
|---|---|---|---|---|
| Fraunces (fuente variable) | The Fraunces Project Authors (Undercase Type) | https://github.com/undercasetype/Fraunces | SIL OFL 1.1 | Títulos y números grandes |
| Bricolage Grotesque (fuente variable) | The Bricolage Grotesque Project Authors (Ateliertriay) | https://github.com/ateliertriay/bricolage | SIL OFL 1.1 | Interfaz |

Los textos completos de las licencias están en `Assets/_Tesela/Fonts/OFL-*.txt`.

**Recursos propios (sin licencia de terceros):** azulejos, marcos 9-slice, grano, iconos y partículas
se generan por código (`Tesela.Core.Raster`); efectos de sonido y música se sintetizan por código
(`Tesela.Core.Audio`). No se usan paquetes de código de terceros en tiempo de ejecución: tweens,
háptica y partículas de interfaz son reemplazos mínimos propios (ver `docs/DECISIONS.md`).

Herramientas de desarrollo (no se distribuyen con el juego): NUnit (MIT), coverlet (MIT),
ensamblados de referencia `UnityEngine.Modules` solo para compilar localmente, rembg (MIT) en `tools/remove_bg.py`.
