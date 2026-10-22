#!/bin/bash
# run a PowerShell script (from stdin) on the Windows test machine; output as UTF-8
: "${WINTERSLED_REMOTE:?set WINTERSLED_REMOTE=user@host of the Windows test machine (e.g. in mise.local.toml)}"
script="[Console]::OutputEncoding=[Text.Encoding]::UTF8; \$ProgressPreference='SilentlyContinue'; $(cat)"
enc=$(printf '%s' "$script" | iconv -f UTF-8 -t UTF-16LE | base64)
ssh -o ConnectTimeout=10 -o BatchMode=yes -o LogLevel=ERROR "$WINTERSLED_REMOTE" "powershell -NoProfile -NonInteractive -EncodedCommand $enc"
