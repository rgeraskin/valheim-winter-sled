#!/bin/bash
# send harness commands (stdin) to the Windows test machine, wait for "done", print the new log lines
W=$(dirname "$0")/remote-ps.sh
cmds=$(cat)
n=$($W <<< "if (Test-Path D:\WinterSledTest\harness\log.txt) { (Get-Content D:\WinterSledTest\harness\log.txt).Count } else { 0 }" | tr -d '\r')
b64=$(printf '%s\n' "$cmds" | base64)
$W <<< "[IO.File]::WriteAllBytes('D:\WinterSledTest\harness\cmd.tmp', [Convert]::FromBase64String('$b64')); Move-Item -Force D:\WinterSledTest\harness\cmd.tmp D:\WinterSledTest\harness\cmd.txt" >/dev/null
for i in $(seq 1 ${HWAIT:-200}); do
  out=$($W <<< "Get-Content D:\WinterSledTest\harness\log.txt | Select -Skip $n; if (-not (Get-Process valheim -ErrorAction SilentlyContinue)) { 'GAME_EXITED' }" | tr -d '\r')
  if echo "$out" | grep -q -E "\] done$|GAME_EXITED"; then echo "$out"; exit 0; fi
  sleep 3
done
echo "$out"; echo TIMEOUT
