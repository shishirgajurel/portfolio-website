#!/usr/bin/env bash
# Called by deploy.ps1 on the server: swaps in the new build and restarts the site.
set -euo pipefail

sudo systemctl stop portfolio || true
sudo rm -rf /opt/portfolio/app
sudo mkdir -p /opt/portfolio/app
sudo tar -xzf ~/portfolio-release.tgz -C /opt/portfolio/app

# Uploaded images live outside the app folder so a redeploy never deletes them.
sudo rm -rf /opt/portfolio/app/wwwroot/uploads
sudo ln -s /opt/portfolio/data/uploads /opt/portfolio/app/wwwroot/uploads

sudo chmod +x /opt/portfolio/app/PortfolioSite
sudo chown -R portfolio:portfolio /opt/portfolio
sudo systemctl start portfolio

sleep 3
if curl -fsS -o /dev/null http://127.0.0.1:5000/robots.txt; then
  echo "Site is running."
else
  echo "Site did not start. Last log lines:"
  sudo journalctl -u portfolio -n 40 --no-pager
  exit 1
fi
