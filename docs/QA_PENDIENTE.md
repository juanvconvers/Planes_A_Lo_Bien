# QA pendiente — checklist para probar en el editor y en un teléfono real

Unity no estaba disponible en el entorno de desarrollo (D-03). Lo verificado automáticamente aquí:
- 113 tests EditMode en verde con .NET 8 (cobertura: Gameplay 98 %, Meta 99 %, Monetization 99 %, Core 94 %).
- 10.000 partidas por bot sin fines falsos (`SIM_REPORT.md`), 60 niveles validados (`LEVELS_REPORT.md`), economía sana (`ECONOMY_REPORT.md`).
- Toda la capa Unity (Services, UI, Editor, tests PlayMode) compila contra ensamblados de referencia de Unity + stubs (`tools/check.sh`).

Lo que sigue **no se ha podido ejecutar** y debes comprobarlo tú.

## 1. Abrir el proyecto (Unity 6.3 LTS)
- [ ] Abre sin errores de compilación. Se instalan solos los paquetes (URP, Input System, IAP, Notifications, Addressables, LevelPlay) y las TMP Essential Resources. Si alguno falla, `Tesela > Setup > Instalar paquetes`.
- [ ] Tras instalar, sigue compilando (se activan `TESELA_IAP`, `TESELA_LEVELPLAY`, `TESELA_NOTIFICATIONS`, `TESELA_INPUTSYSTEM`). **Riesgo:** los adaptadores de SDK se escribieron sin poder compilarlos; si un nombre de API cambió, corrige el adaptador (`Scripts/Services/Monetization/*`, `Analytics/FirebaseServices.cs`, `Platform/PlatformAdapters.cs`) o quita temporalmente el paquete.
- [ ] `Tesela > Setup > Configurar proyecto Android`: targetSdk 36, minSdk 24, IL2CPP, ARM64, AAB, retrato, escena Boot en Build Settings, URP 2D asignado.
- [ ] Test Runner → **EditMode**: todo en verde. **PlayMode**: todo en verde (300 jugadas por UI, guardar/reanudar, 200 jugadas por modo, todas las pantallas, sin red ni IDs).

## 2. Primera partida (teléfono, instalación limpia)
- [ ] La primera partida es jugable en < 10 s desde que se abre la app; el tutorial de 3 pasos aparece y avanza solo.
- [ ] Arrastrar: la pieza se eleva sobre el dedo, crece a tamaño real, la sombra de destino tiene imán y las líneas que se completarían se resaltan en oro.
- [ ] Soltar fuera devuelve la pieza con rebote. Vibración leve al encajar (si está activada).
- [ ] Limpiar líneas: azulejos saltan en arco al contador, suena el combo pentatónico y aparece el microcopy ("¡Doble junta!", etc.).
- [ ] Tras la primera partida: Resultados con semilla → al ir a Inicio aparece el paso de consentimiento (y el formulario UMP si está el plugin).

## 3. Modos y meta
- [ ] Restauración: niveles 1–20 del capítulo 1 jugables; agrietadas necesitan dos limpiezas; barro se libera; estrellas y álbum.
- [ ] Reto diario: mismo código de semilla en dos teléfonos el mismo día UTC.
- [ ] Zen: nunca termina; el barrido limpia la fila más llena; música más calmada; sin banner.
- [ ] Evento activo (giro libre: tocar una pieza la gira gratis). Cambiar `events.active` en Remote Config a `patio_grande`, `bombas` o `azulejos_dorados` y probar cada uno.
- [ ] Misiones diarias se cobran una vez; racha con protector; cofre muestra probabilidades antes de abrir; pase reclama gratis/premium; logros con barra.
- [ ] Cerrar la app a mitad de partida (incluido "forzar detención") → "Continuar partida" restaura todo.

## 4. Monetización (con IDs reales en `ExternalIds.asset`)
- [ ] LevelPlay inicializa solo después del consentimiento. Videos con premio: revivir (1 por partida), duplicar monedas, potenciador gratis (5/día), tope 20/día.
- [ ] Intersticiales: nunca en la primera sesión ni en las 3 primeras partidas; solo al salir de Resultados; ≥ 150 s y 2 partidas entre ellos; nunca 120 s después de un video con premio; nunca 24 h tras una compra; nunca a quien compró Sin anuncios / Club / Pase Plus.
- [ ] Banner adaptativo solo en Inicio; nunca sobre el tablero ni en Zen.
- [ ] Sin video disponible: se dice claramente y se ofrece la alternativa con monedas.
- [ ] Compras de prueba (cuentas de prueba de Play): cada producto entrega su contenido; Restaurar compras; suscripción Club muestra precio, periodo y cómo cancelar; "Gestionar suscripciones" abre Play.
- [ ] Oferta por segmento: aparece solo en momentos de logro, máximo una por sesión, nunca tras perder.

## 5. Servicios
- [ ] Firebase: con `google-services.json` en `Assets/`, eventos `run_start`, `run_end`, `booster_use`, `ad_*`, `iap_*`, `offer_*`, `tutorial_step`, `pass_level`, `streak_update`, `level_*` llegan a DebugView.
- [ ] Remote Config: cambiar `ads` u `offers` en la consola modifica el comportamiento sin recompilar.
- [ ] Play Games: inicio de sesión, envío de puntuación (Clásico y Diario), logros (`achievementIds` en ExternalIds).
- [ ] In-App Review solo tras un récord o capítulo completo y tras ≥ 8 partidas, una sola vez. In-App Updates flexible.
- [ ] Notificaciones: el permiso se pide solo al activarlo en Ajustes; como máximo una al día a las 19:00.
- [ ] Modo avión: el juego arranca y se juega completo.

## 6. Accesibilidad y textos
- [ ] Español, inglés y portugués: ningún texto cortado en un teléfono pequeño (720×1280) ni en tableta.
- [ ] Tamaño de texto 115 % y 130 %; modo zurdo espeja HUD y potenciadores; reducir movimiento quita animaciones no esenciales; patrones visibles en los 6 esmaltes.
- [ ] Música y efectos por separado; todo se silencia al pasar a segundo plano.
- [ ] Botón Atrás de Android: cierra diálogos, abre pausa en partida y sale desde Inicio.

## 7. Rendimiento y build (ver PERFORMANCE.md)
- [ ] 60 fps estables en gama media; GC Alloc ≈ 0 por frame en partida (Profiler).
- [ ] Arranque en frío ≤ 3 s; AAB base ≤ 50 MB; informe de 16 KB sin avisos.
- [ ] `Tesela > Build > AAB de release` con las variables de `RELEASE_SIGNING.md` genera `Builds/Tesela.aab`.

## 8. Legal y tienda
- [ ] Revisar y publicar `PRIVACY_POLICY_DRAFT.md`; poner la URL en `ExternalIds.privacyPolicyUrl`.
- [ ] Completar Seguridad de los datos con `DATA_SAFETY_DRAFT.md`.
- [ ] Ficha con `STORE_LISTING.md` y capturas de jugabilidad real.
- [ ] Nombre: búsqueda en Google Play desde el teléfono, WIPO y SIC Colombia (D-01).
