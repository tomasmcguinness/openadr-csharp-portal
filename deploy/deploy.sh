#!/usr/bin/env bash
# Publishes the VTN locally (including building the dashboard — this is the "build on my
# machine, never on the server" step) and ships it to a Lightsail instance over scp, then
# restarts the systemd service. Run from anywhere; paths below are relative to this script.
#
# Usage:
#   REMOTE_HOST=1.2.3.4 REMOTE_USER=ubuntu ./deploy.sh
#
# First-time setup on the instance (once, by hand — see README "Lightsail deployment"):
#   sudo useradd --system --home /opt/openadr-vtn --shell /usr/sbin/nologin openadr-vtn
#   sudo mkdir -p /opt/openadr-vtn /var/lib/openadr-vtn
#   sudo chown openadr-vtn:openadr-vtn /opt/openadr-vtn /var/lib/openadr-vtn

set -euo pipefail

REMOTE_HOST="${REMOTE_HOST:?Set REMOTE_HOST to the Lightsail instance's public IP or hostname}"
REMOTE_USER="${REMOTE_USER:-ubuntu}"
REMOTE_DIR="${REMOTE_DIR:-/opt/openadr-vtn}"
SERVICE_NAME="${SERVICE_NAME:-openadr-vtn}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
SERVER_PROJECT="$REPO_ROOT/src/OpenAdr.Vtn.Server/OpenAdr.Vtn.Server.csproj"
PUBLISH_DIR="$REPO_ROOT/deploy/.publish"

echo "==> Publishing self-contained linux-x64 build (this also builds the dashboard)..."
rm -rf "$PUBLISH_DIR"
dotnet publish "$SERVER_PROJECT" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -o "$PUBLISH_DIR"

echo "==> Copying the systemd unit..."
scp "$SCRIPT_DIR/openadr-vtn.service" "$REMOTE_USER@$REMOTE_HOST:/tmp/openadr-vtn.service"

echo "==> Copying published files to $REMOTE_USER@$REMOTE_HOST:$REMOTE_DIR..."
# rsync would be nicer, but scp -r needs nothing beyond what Lightsail ships with by default.
ssh "$REMOTE_USER@$REMOTE_HOST" "sudo mkdir -p '$REMOTE_DIR' && sudo chown \$(whoami) '$REMOTE_DIR'"
scp -r "$PUBLISH_DIR"/* "$REMOTE_USER@$REMOTE_HOST:$REMOTE_DIR/"

echo "==> Installing the service and restarting..."
ssh "$REMOTE_USER@$REMOTE_HOST" "
  set -e
  sudo mv /tmp/openadr-vtn.service /etc/systemd/system/$SERVICE_NAME.service
  sudo chown -R openadr-vtn:openadr-vtn '$REMOTE_DIR'
  sudo chmod +x '$REMOTE_DIR/OpenAdr.Vtn.Server'
  sudo systemctl daemon-reload
  sudo systemctl enable '$SERVICE_NAME'
  sudo systemctl restart '$SERVICE_NAME'
  sudo systemctl --no-pager status '$SERVICE_NAME'
"

echo "==> Done. Tail logs with: ssh $REMOTE_USER@$REMOTE_HOST sudo journalctl -u $SERVICE_NAME -f"
