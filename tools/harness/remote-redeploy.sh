#!/bin/bash
# stop the game on the Windows test machine, copy fresh DLLs, (optionally) set config keys, start again and wait for the world
W=$(dirname "$0")/remote-ps.sh
: "${WINTERSLED_REMOTE:?set WINTERSLED_REMOTE=user@host of the Windows test machine (e.g. in mise.local.toml)}"
R=$(cd "$(dirname "$0")/../.." && pwd)
# quit through the harness so the game saves the world (live edits included); kill only as a fallback
$W <<'EOS'
if (Get-Process valheim -ErrorAction SilentlyContinue) {
  Set-Content -Path D:\WinterSledTest\harness\cmd.txt -Value 'quit'
  for ($i = 0; $i -lt 60 -and (Get-Process valheim -ErrorAction SilentlyContinue); $i++) { Start-Sleep 1 }
  if (Get-Process valheim -ErrorAction SilentlyContinue) { Stop-Process -Name valheim -Force; 'killed' } else { 'quit cleanly' }
} else { 'not running' }
Start-Sleep 2
EOS
scp -q -o LogLevel=ERROR $R/WinterSled/bin/Release/WinterSled.dll "$WINTERSLED_REMOTE:D:/WinterSledTest/BepInEx/plugins/WinterSled/"
scp -q -o LogLevel=ERROR $R/tools/DevHarness/bin/Release/WinterSledDevHarness.dll "$WINTERSLED_REMOTE:D:/WinterSledTest/BepInEx/plugins/WinterSledDevHarness/"
sets=""
for kv in "$@"; do k=${kv%%=*}; v=${kv#*=}; sets="$sets \$c = \$c -replace '(?m)^$k = .*$', '$k = $v';"; done
$W <<EOS
\$f='D:\WinterSledTest\BepInEx\config\rgeraskin.WinterSled.cfg'
if (Test-Path \$f) { \$c = Get-Content \$f -Raw; $sets Set-Content \$f \$c -NoNewline }
schtasks /Run /TN WinterSledTest | Out-Null
'started'
EOS
for i in $(seq 1 60); do
  out=$($W <<< "if (Test-Path D:\WinterSledTest\harness\log.txt) { Get-Content D:\WinterSledTest\harness\log.txt -Tail 1 }; Get-Content D:\WinterSledTest\BepInEx\LogOutput.log -ErrorAction SilentlyContinue | Select-String 'Building the sled failed' | Select -Last 1" | tr -d '\r')
  echo "$out" | grep -q -E "starting world|failed" && { echo "$out"; break; }
  sleep 4
done
