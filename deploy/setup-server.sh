#!/usr/bin/env bash
# One-time server setup for Ubuntu on Oracle Cloud. Run as the default "ubuntu" user:
#   bash setup-server.sh
set -euo pipefail

echo "== Updating packages"
sudo apt-get update -y
sudo apt-get upgrade -y
sudo apt-get install -y curl unattended-upgrades

echo "== Creating service user and folders"
id portfolio >/dev/null 2>&1 || sudo useradd --system --home /opt/portfolio --shell /usr/sbin/nologin portfolio
sudo mkdir -p /opt/portfolio/app /opt/portfolio/data/uploads /opt/portfolio/data/keys /etc/portfolio
sudo chown -R portfolio:portfolio /opt/portfolio

echo "== Installing cloudflared"
ARCH=$(dpkg --print-architecture)   # arm64 on Ampere, amd64 on the AMD micro VM
curl -fsSL -o /tmp/cloudflared.deb \
  "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-${ARCH}.deb"
sudo dpkg -i /tmp/cloudflared.deb

echo "== Installing the systemd service"
sudo cp ~/portfolio.service /etc/systemd/system/portfolio.service
sudo systemctl daemon-reload
sudo systemctl enable portfolio

if [ ! -f /etc/portfolio/secrets.env ]; then
  echo
  echo "== Admin login for /admin (typed here, never stored in the code)"
  read -rp  "Admin email: " ADMIN_EMAIL
  read -rsp "Admin password (min. 14 characters): " ADMIN_PASSWORD; echo
  printf 'Admin__Email=%s\nAdmin__Password=%s\n' "$ADMIN_EMAIL" "$ADMIN_PASSWORD" | sudo tee /etc/portfolio/secrets.env >/dev/null
  sudo chmod 600 /etc/portfolio/secrets.env
fi

echo
echo "Server is ready. Next: run deploy.ps1 on your PC."
