#!/bin/sh
set -eu

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
project_dir=$(CDPATH= cd -- "$script_dir/../.." && pwd)
unity_editor_path=${UNITY_EDITOR_PATH:-/Applications/Unity/Hub/Editor/6000.4.7f1/Unity.app}
mono_path="$unity_editor_path/Contents/Resources/Scripting/MonoBleedingEdge/bin/mono"
csc_path="$unity_editor_path/Contents/Resources/Scripting/MonoBleedingEdge/lib/mono/4.5/csc.exe"
validation_dir=$(mktemp -d "${TMPDIR:-/tmp}/neon-clash-validation.XXXXXX")
validation_exe="$validation_dir/NeonClashSimulationValidation.exe"

cleanup() {
  rm -f "$validation_exe"
  rmdir "$validation_dir"
}
trap cleanup EXIT HUP INT TERM

if [ ! -x "$mono_path" ] || [ ! -f "$csc_path" ]; then
  echo "Unity's bundled Mono compiler was not found under: $unity_editor_path" >&2
  echo "Set UNITY_EDITOR_PATH to the installed Unity.app path and retry." >&2
  exit 2
fi

"$mono_path" "$csc_path" -nologo -target:exe -out:"$validation_exe" \
  "$project_dir/Assets/NeonClash/Scripts/Runtime/CombatTypes.cs" \
  "$project_dir/Assets/NeonClash/Scripts/Runtime/FighterSimulation.cs" \
  "$project_dir/Assets/NeonClash/Scripts/Runtime/DeterministicCpu.cs" \
  "$project_dir/Assets/NeonClash/Scripts/Runtime/ComboSequenceTracker.cs" \
  "$project_dir/Assets/NeonClash/Scripts/Runtime/DeterministicMatchSimulation.cs" \
  "$project_dir/Assets/NeonClash/Scripts/Runtime/RollbackNetcode.cs" \
  "$script_dir/Program.cs"

"$mono_path" "$validation_exe"
