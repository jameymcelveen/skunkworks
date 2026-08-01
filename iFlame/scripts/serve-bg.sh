#!/bin/sh
# Start a background static server; write PID to pidfile.
# Usage: serve-bg.sh <root> <port> <host> <pidfile> <logfile>

set -e
ROOT=$1
PORT=$2
HOST=$3
PIDFILE=$4
LOGFILE=$5

if [ -f "$PIDFILE" ] && kill -0 "$(cat "$PIDFILE")" 2>/dev/null; then
  echo "Server already running (pid $(cat "$PIDFILE")) at http://${HOST}:${PORT}/"
  exit 0
fi

rm -f "$PIDFILE"
cd "$ROOT"
nohup python3 -m http.server "$PORT" --bind "$HOST" >"$LOGFILE" 2>&1 &
echo $! >"$PIDFILE"
sleep 0.5

pid=$(cat "$PIDFILE")
if ! kill -0 "$pid" 2>/dev/null; then
  echo "Server failed to start; see $LOGFILE" >&2
  rm -f "$PIDFILE"
  exit 1
fi

echo "Serving $ROOT at http://${HOST}:${PORT}/ (pid $pid)"
