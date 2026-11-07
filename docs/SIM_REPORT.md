# Informe de simulación (Fase 1)

| Bot | Partidas | Excepciones | Fines falsos | Bandejas sin pieza jugable | >2 iguales seguidas | Puntuación media | Mediana | P90 | Máx | Jugadas medias | Líneas medias | Racha máx | Topadas |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Aleatorio · Clásico | 10000 | 0 | 0 | 0 | 0 | 70 | 55 | 128 | 603 | 13.9 | 1.4 | 11 | 0 |
| Codicioso · Clásico | 10000 | 0 | 0 | 0 | 0 | 1736 | 1402 | 3527 | 12257 | 114.7 | 49.5 | 45 | 0 |
| Aleatorio · Relajado | 1000 | 0 | 0 | 0 | 0 | 121 | 83 | 253 | 689 | 19.9 | 3.4 | 9 | 0 |
| Aleatorio · Evento 9×9 | 1000 | 0 | 0 | 0 | 0 | 90 | 71 | 160 | 502 | 18.1 | 1.7 | 6 | 0 |

Generado con `dotnet run --project tools/dotnet/Tesela.Tools -- sim 10000` en 8.3 s.

**Qué se verifica en cada partida**
- *Fines falsos*: el juego terminó pero una comprobación independiente celda a celda (sin máscaras de bits) encuentra una jugada, o el bot no encontró jugada sin que el juego hubiera terminado.
- *Bandejas sin pieza jugable*: alguna bandeja nueva llegó sin ninguna pieza que quepa (el generador lo prohíbe).
- *>2 iguales seguidas*: tres piezas consecutivas de la misma familia (el generador lo prohíbe salvo que la única pieza que cabe lo exija).
- *Topadas*: partidas cortadas al llegar a 3.000 jugadas (el bot codicioso puede jugar casi indefinidamente).

**Resultado: PASA** (cero excepciones, cero fines falsos, cero bandejas injugables).
