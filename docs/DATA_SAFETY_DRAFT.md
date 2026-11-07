# Respuestas para "Seguridad de los datos" de Google Play — BORRADOR

Revisar con las versiones finales de los SDK (LevelPlay, AdMob, Firebase, Play Games) y sus guías de Data safety.

| Pregunta | Respuesta propuesta |
|---|---|
| ¿Recopila o comparte datos de usuario? | Sí |
| ¿Datos cifrados en tránsito? | Sí (todos los SDK usan HTTPS) |
| ¿El usuario puede pedir que se borren? | Sí: Ajustes > Borrar mis datos + correo/URL de borrado |
| Ubicación aproximada | Recopilada y compartida (anuncios, analítica) · no obligatoria para usar la app |
| Información personal (nombre, correo) | No |
| Información financiera | Historial de compras: recopilado por Google Play (no por la app) |
| Actividad en la app (interacciones, otras acciones) | Recopilada: analítica, personalización de ofertas en el dispositivo |
| Información y rendimiento de la app (fallos, diagnósticos) | Recopilada: Crashlytics |
| Identificadores de dispositivo (ID de publicidad, ID de instancia) | Recopilados y compartidos: publicidad y analítica |
| Finalidades | Funcionalidad de la app, analítica, publicidad o marketing, prevención de fraude (Play) |
| Público objetivo | 13+ (no dirigido a niños) |
