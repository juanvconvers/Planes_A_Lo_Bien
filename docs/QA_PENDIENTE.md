# QA pendiente (probar en el editor y en dispositivo)

Unity no estaba disponible en el entorno de desarrollo (D-03). La lógica pura está compilada y testeada con .NET 8;
todo lo de esta lista requiere el editor o un teléfono.

## Al abrir el proyecto por primera vez (Unity 6.3 LTS)
- [ ] El proyecto abre sin errores de compilación; `Tesela > Setup > Instalar paquetes` se ejecuta solo y termina.
- [ ] Se importan las TMP Essential Resources.
- [ ] `Tesela > Setup > Configurar proyecto Android` deja targetSdk 36, minSdk 24, IL2CPP, ARM64, AAB y crea `Scenes/Boot.unity`.
- [ ] Test Runner → EditMode: todos los tests en verde (los mismos 62+ que pasan en .NET).
