# Firma del AAB de release

La keystore **nunca** se sube al repositorio (`.gitignore` excluye `*.keystore`, `*.jks`, `keystore.properties`).

## Crear la keystore (una sola vez, en tu equipo)
```bash
keytool -genkeypair -v -keystore ~/claves/tesela-upload.jks -alias tesela -keyalg RSA -keysize 4096 -validity 10000
```
Guarda la keystore y las contraseñas en un gestor de contraseñas y en una copia de seguridad fuera del equipo.
Activa **Play App Signing** en Play Console: esta es tu *clave de subida*; Google guarda la clave de firma de la app.

## Compilar el AAB firmado
```bash
export TESELA_KEYSTORE_PATH=~/claves/tesela-upload.jks
export TESELA_KEYSTORE_PASS='…'
export TESELA_KEY_ALIAS=tesela
export TESELA_KEY_PASS='…'
Unity -batchmode -nographics -quit -projectPath . -executeMethod Tesela.EditorTools.BuildAndroid.BuildReleaseAab -logFile Logs/aab.log
```
Resultado: `Builds/Tesela.aab`. Sin las variables, el build usa la clave de depuración y Play lo rechazará (se avisa en el log).

## Antes de subir
- Sube `Application.version` / `bundleVersionCode` en Player Settings.
- Comprueba en el informe del build que el AAB base pese ≤ 50 MB.
- Ejecuta el informe de "páginas de 16 KB" de Play Console (o `zipalign -c -P 16 -v 4` sobre el APK universal) y revisa que todas las `.so` de los SDK estén alineadas.
