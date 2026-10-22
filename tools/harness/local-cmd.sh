#!/bin/bash
# send harness commands (stdin) to the game running on this Mac (.harness), wait for "done"
D="$(cd "$(dirname "$0")/../.." && pwd)/.harness"
n=$( [ -f "$D/log.txt" ] && wc -l < "$D/log.txt" || echo 0 )
cat > "$D/cmd.tmp" && mv "$D/cmd.tmp" "$D/cmd.txt"
for i in $(seq 1 ${HWAIT:-200}); do
  out=$(tail -n +$((n + 1)) "$D/log.txt" 2>/dev/null)
  if echo "$out" | grep -q -E "\] done$" || ! pgrep -f "MacOS/Valheim" >/dev/null; then echo "$out"; exit 0; fi
  sleep 3
done
echo "$out"; echo TIMEOUT
