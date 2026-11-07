# Juego limpio

Reglas públicas del generador de piezas de Tesela. Se muestran resumidas en **Ajustes > Juego limpio** (claves `fair.rule1..7`).

1. **Semilla visible.** Cada partida usa un generador determinista PCG32 con semilla. El código de 8 caracteres aparece en
   Resultados y en Pausa: la misma semilla produce exactamente la misma secuencia de piezas en cualquier teléfono.
2. **Aislamiento por arquitectura.** El generador (`Tesela.Gameplay.FairBag`) solo recibe el catálogo público de piezas, el
   tablero, la puntuación de la partida y su RNG. `Tesela.Gameplay` no referencia `Tesela.Monetization` (ni Meta ni Services);
   lo verifican tres tests: por reflexión sobre el ensamblado, leyendo el `.asmdef` y revisando que la API pública de
   `FairBag` no acepte otros tipos (`FairPlayArchitectureTests`). No existe ningún camino para pasarle datos de anuncios, gasto,
   país, dispositivo o valor del jugador.
3. **Siempre una pieza jugable.** Cada bandeja nueva trae al menos una pieza que cabe (verificado en 10.000 partidas por bot,
   `docs/SIM_REPORT.md`).
4. **Variedad.** Nunca más de dos piezas de la misma familia seguidas (la garantía 3 tiene prioridad en el caso extremo de que
   la única pieza que cabe lo exija; en la simulación no ocurrió nunca).
5. **Dificultad igual para todos.** Solo depende de la puntuación de la partida, según la tabla de `Pieces.json`
   (`difficulty`): el peso de las piezas grandes (5+ celdas) va de ×0,8 (0 puntos) a ×1,6 (10.000+ puntos).
6. **Fin de partida honesto.** Solo termina cuando ninguna pieza de la bandeja (ni de la reserva desbloqueada, ni sus giros en
   el evento de giro libre) cabe en ninguna posición. Comprobación exhaustiva con máscaras de bits y, en los tests, con una
   verificación independiente celda a celda.
7. **Reto diario compartido.** La semilla sale de la fecha UTC (`Seeds.Daily`): todo el mundo juega la misma secuencia.

**Modo relajado** (opción transparente en Ajustes): las tres piezas de cada bandeja caben en el tablero actual y, colocadas en
orden con una estrategia razonable, caben todas.
