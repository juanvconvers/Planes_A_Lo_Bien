# Prompts para Gemini (Fase 10)

Flujo: 1) genera primero la **hoja de estilo** (sección 0) con Nano Banana Pro; 2) adjúntala como referencia en todos los demás prompts; 3) guarda cada imagen como `ArtDrop/<id>.png` con el id exacto de `docs/ASSET_MANIFEST.json`; 4) si una pieza necesita transparencia y Gemini no entrega alfa real, deja el fondo croma y ejecuta `python3 tools/remove_bg.py` (o deja que el importador quite el croma); 5) en Unity: **Tesela > Importar arte de ArtDrop**.

Cada prompt pide el **doble del tamaño final** para reducir después. Modelos: Nano Banana Pro para piezas protagonistas (azulejos, icono, mosaicos, fondos); Nano Banana 2 para volumen (iconos).

## Bloque de estilo maestro (se repite al inicio de cada prompt)

```
Estilo: azulejo de cerámica esmaltada pintado a mano, trazos de pincel visibles, leves imperfecciones del esmalte, craquelado fino, luz suave desde arriba a la izquierda, vista cenital ortográfica sin perspectiva. Paleta estricta: arena #F2E6D8, índigo #23305C, cobalto #2F5DA8, ocre #D99A2B, oliva #6E8B3D, coral #E0654F, turquesa #2A9D8F, ciruela #7B3F61, oro #E8B64C. Sin texto, sin letras, sin marcas de agua, sin logotipos, sin parecido con juegos, personajes o marcas existentes.
```

## 0. Hoja de estilo (referencia para todo lo demás)

```
[Bloque de estilo maestro] Hoja de referencia de 3×2 azulejos cuadrados sobre fondo arena liso #F2E6D8, separados por juntas gris cálido #8C7B6B: cobalto con estrella de ocho puntas, ocre con flor de seis pétalos, oliva con rombo, coral con dos ondas, turquesa con punto y anillo, ciruela con cruz. Patrones en blanco hueso pintados a pincel. 2048×1365.
```

## 1. `tile_cobalto` — Azulejo esmaltado cobalto con patrón estrella (pieza y tablero) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Azulejo cuadrado de 512x512, esmalte cobalto #2F5DA8 con estrella en blanco hueso, borde biselado, centrado, ocupa el 90% del lienzo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 2. `tile_ocre` — Azulejo esmaltado ocre con patrón flor (pieza y tablero) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Azulejo cuadrado de 512x512, esmalte ocre #D99A2B con flor en blanco hueso, borde biselado, centrado, ocupa el 90% del lienzo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 3. `tile_oliva` — Azulejo esmaltado oliva con patrón rombo (pieza y tablero) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Azulejo cuadrado de 512x512, esmalte oliva #6E8B3D con rombo en blanco hueso, borde biselado, centrado, ocupa el 90% del lienzo. Fondo plano croma #FF00FF (magenta), sin sombras sobre el fondo.
```

## 4. `tile_coral` — Azulejo esmaltado coral con patrón onda (pieza y tablero) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Azulejo cuadrado de 512x512, esmalte coral #E0654F con onda en blanco hueso, borde biselado, centrado, ocupa el 90% del lienzo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 5. `tile_turquesa` — Azulejo esmaltado turquesa con patrón punto (pieza y tablero) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Azulejo cuadrado de 512x512, esmalte turquesa #2A9D8F con punto en blanco hueso, borde biselado, centrado, ocupa el 90% del lienzo. Fondo plano croma #FF00FF (magenta), sin sombras sobre el fondo.
```

## 6. `tile_ciruela` — Azulejo esmaltado ciruela con patrón cruz (pieza y tablero) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Azulejo cuadrado de 512x512, esmalte ciruela #7B3F61 con cruz en blanco hueso, borde biselado, centrado, ocupa el 90% del lienzo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 7. `tile_socket` — Casilla vacía del patio (baldosa de arena hundida) (256×256, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Baldosa cuadrada de arena #F2E6D8 ligeramente hundida, mate, con junta gris cálido alrededor, 512x512, ocupa el 94% del lienzo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 8. `frame_panel` — Marco 9-slice de cerámica para paneles (512×512, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Marco rectangular de cerámica esmaltada arena clara, 1024x1024, borde de 128 px algo irregular hecho a mano, centro liso y uniforme (se estirará como 9-slice: nada de detalles en el centro ni en el medio de los lados), sombra dura y corta abajo a la derecha. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 9. `frame_button_primary` — Marco 9-slice de botón principal (cobalto) (512×512, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Marco rectangular de cerámica esmaltada cobalto #2F5DA8, 1024x1024, borde de 128 px algo irregular hecho a mano, centro liso y uniforme (se estirará como 9-slice: nada de detalles en el centro ni en el medio de los lados), sombra dura y corta abajo a la derecha. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 10. `frame_button_secondary` — Marco 9-slice de botón secundario (arena) (512×512, prioridad 2, Nano Banana Pro)

```
[Bloque de estilo maestro] Marco rectangular de cerámica esmaltada arena #F2E6D8 con borde gris cálido, 1024x1024, borde de 128 px algo irregular hecho a mano, centro liso y uniforme (se estirará como 9-slice: nada de detalles en el centro ni en el medio de los lados), sombra dura y corta abajo a la derecha. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 11. `background_patio` — Fondo vertical del patio de baldosas (1080×1920, prioridad 2, Nano Banana Pro)

```
[Bloque de estilo maestro] Patio de baldosas de terracota y arena visto desde arriba, 2160x3840 vertical, muy sutil y de bajo contraste para que el tablero destaque, grano de terracota al 4%, sin objetos.
```

## 12. `mosaic_plaza` — Mosaico del capítulo 1: la plaza del pueblo con su kiosco (760×456, prioridad 2, Nano Banana Pro)

```
[Bloque de estilo maestro] Mosaico de teselas cerámicas que representa la plaza del pueblo con su kiosco, 1520x912, teselas visibles de tamaño uniforme, colores solo de la paleta. Imagen a sangre completa, sin bordes.
```

## 13. `mosaic_fuente` — Mosaico del capítulo 2: la fuente de la plaza con agua (760×456, prioridad 2, Nano Banana Pro)

```
[Bloque de estilo maestro] Mosaico de teselas cerámicas que representa la fuente de la plaza con agua, 1520x912, teselas visibles de tamaño uniforme, colores solo de la paleta. Imagen a sangre completa, sin bordes.
```

## 14. `mosaic_capilla` — Mosaico del capítulo 3: la fachada de la capilla con su campanario (760×456, prioridad 2, Nano Banana Pro)

```
[Bloque de estilo maestro] Mosaico de teselas cerámicas que representa la fachada de la capilla con su campanario, 1520x912, teselas visibles de tamaño uniforme, colores solo de la paleta. Imagen a sangre completa, sin bordes.
```

## 15. `icon_play` — Icono de interfaz 'play' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'play' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 16. `icon_back` — Icono de interfaz 'back' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'back' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 17. `icon_close` — Icono de interfaz 'close' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'close' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 18. `icon_settings` — Icono de interfaz 'settings' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'settings' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 19. `icon_shop` — Icono de interfaz 'shop' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'shop' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 20. `icon_coin` — Icono de interfaz 'coin' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'coin' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 21. `icon_tile` — Icono de interfaz 'tile' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'tile' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 22. `icon_star` — Icono de interfaz 'star' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'star' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 23. `icon_undo` — Icono de interfaz 'undo' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'undo' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 24. `icon_rotate` — Icono de interfaz 'rotate' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'rotate' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 25. `icon_hammer` — Icono de interfaz 'hammer' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'hammer' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 26. `icon_broom` — Icono de interfaz 'broom' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'broom' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 27. `icon_newtray` — Icono de interfaz 'newtray' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'newtray' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 28. `icon_reserve` — Icono de interfaz 'reserve' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'reserve' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 29. `icon_trophy` — Icono de interfaz 'trophy' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'trophy' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 30. `icon_calendar` — Icono de interfaz 'calendar' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'calendar' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 31. `icon_zen` — Icono de interfaz 'zen' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'zen' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 32. `icon_album` — Icono de interfaz 'album' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'album' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 33. `icon_pass` — Icono de interfaz 'pass' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'pass' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 34. `icon_sound` — Icono de interfaz 'sound' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'sound' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 35. `icon_music` — Icono de interfaz 'music' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'music' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 36. `icon_vibration` — Icono de interfaz 'vibration' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'vibration' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 37. `icon_check` — Icono de interfaz 'check' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'check' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 38. `icon_lock` — Icono de interfaz 'lock' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'lock' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 39. `icon_gift` — Icono de interfaz 'gift' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'gift' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 40. `icon_info` — Icono de interfaz 'info' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'info' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 41. `icon_crown` — Icono de interfaz 'crown' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'crown' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 42. `icon_chest` — Icono de interfaz 'chest' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'chest' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 43. `icon_home` — Icono de interfaz 'home' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'home' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 44. `icon_flame` — Icono de interfaz 'flame' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'flame' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 45. `icon_shield` — Icono de interfaz 'shield' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'shield' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 46. `icon_ad` — Icono de interfaz 'ad' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'ad' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 47. `icon_restore` — Icono de interfaz 'restore' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'restore' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 48. `icon_heart` — Icono de interfaz 'heart' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'heart' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 49. `icon_hand` — Icono de interfaz 'hand' (trazo uniforme, blanco sobre transparente; se tiñe en el juego) (192×192, prioridad 3, Nano Banana 2)

```
[Bloque de estilo maestro] Icono 'hand' de trazo grueso uniforme en blanco puro, estilo pincel cerámico, centrado, 384x384, mismo grosor que el resto del set, sin relleno de fondo. Fondo plano croma #00FF00, sin sombras sobre el fondo.
```

## 50. `app_icon` — Icono de la app para Google Play y el lanzador (512×512, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Icono de app 1024x1024: un solo azulejo cobalto con estrella blanca levemente girado sobre fondo arena, bisel marcado, legible a 48 px. Sin texto.
```

## 51. `feature_graphic` — Gráfico destacado de Google Play (1024×500, prioridad 1, Nano Banana Pro)

```
[Bloque de estilo maestro] Gráfico destacado 2048x1000: patio de azulejos esmaltados en diagonal con piezas encajando y teselas saltando hacia un mosaico, espacio libre a la izquierda para el título (que se añade aparte). Sin texto.
```
