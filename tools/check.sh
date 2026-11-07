#!/usr/bin/env bash
# Verificación completa sin Unity: lógica pura + tests EditMode (con cobertura) + compilación de la capa Unity
# contra ensamblados de referencia y stubs + simulaciones. Uso: tools/check.sh [--quick]
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT/tools/dotnet"
"$ROOT/tools/fetch_unity_refs.sh" >/dev/null
echo "▶ Tests EditMode (.NET 8 + NUnit)"
rm -rf TestResults
dotnet test Tesela.Tests -nologo -v q --collect:"XPlat Code Coverage" --results-directory TestResults
COV=$(find TestResults -name coverage.cobertura.xml | head -1)
python3 - "$COV" <<'PY'
import sys, xml.etree.ElementTree as ET
root = ET.parse(sys.argv[1]).getroot()
for p in root.find('packages'):
    rate = float(p.get('line-rate')) * 100
    print(f"  cobertura {p.get('name')}: {rate:.1f} %")
    if p.get('name') == 'Tesela.Gameplay' and rate < 90:
        sys.exit("Cobertura de Gameplay por debajo del 90 %")
PY
echo "▶ Compilación de la capa Unity (Services, UI, Editor, PlayMode)"
dotnet build Tesela.UnityCheck --no-incremental -nologo -v q
if [ "${1:-}" != "--quick" ]; then
  echo "▶ Simulación, niveles y economía"
  dotnet run -c Release --project Tesela.Tools -- sim 10000 >/dev/null
  dotnet run -c Release --project Tesela.Tools -- levels
  dotnet run -c Release --project Tesela.Tools -- economy
fi
echo "✔ Todo en verde"
