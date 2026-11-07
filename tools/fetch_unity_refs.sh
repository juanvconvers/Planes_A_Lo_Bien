#!/usr/bin/env bash
# Descarga ensamblados de REFERENCIA de UnityEngine/UnityEditor (NuGet) solo para compilar localmente la capa Unity
# sin tener el editor instalado. No se versionan ni se distribuyen con el juego.
set -euo pipefail
DEST="$(cd "$(dirname "$0")" && pwd)/dotnet/.refs"
mkdir -p "$DEST"
fetch() { # id version
  local id="$1" ver="$2" out="$DEST/$1"
  [ -d "$out" ] && return 0
  curl -sSL -o "$DEST/$id.nupkg" "https://api.nuget.org/v3-flatcontainer/$id/$ver/$id.$ver.nupkg"
  mkdir -p "$out" && unzip -q -o "$DEST/$id.nupkg" -d "$out" && rm "$DEST/$id.nupkg"
}
fetch unityengine.modules 2021.3.33
fetch unity3d.sdk 2021.1.14.1
echo "Referencias en $DEST"
