#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
test_root="$(mktemp -d)"
trap 'rm -rf "$test_root"' EXIT

fake_bin="$test_root/bin"
install_log="$test_root/sudo.log"
mkdir -p "$fake_bin"

cat >"$fake_bin/dotnet" <<'EOF'
#!/usr/bin/env bash
if [[ "${1:-}" == "--list-sdks" ]]; then
  echo "8.0.100 [/usr/share/dotnet/sdk]"
fi
EOF

cat >"$fake_bin/npm" <<'EOF'
#!/usr/bin/env bash
exit 0
EOF

cat >"$fake_bin/sudo" <<'EOF'
#!/usr/bin/env bash
printf '%s\n' "$*" >>"${DOTNET_INSTALL_LOG:?}"
EOF

chmod +x "$fake_bin/dotnet" "$fake_bin/npm" "$fake_bin/sudo"
export DOTNET_INSTALL_LOG="$install_log"

PATH="$fake_bin:$PATH" bash "$repo_root/.cursor/install.sh"

if ! grep -q 'apt-get install .*dotnet-sdk-10.0' "$install_log"; then
  echo "Expected the installer to install .NET 10 when only .NET 8 is available." >&2
  exit 1
fi
