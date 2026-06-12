# Deploy: NajaEchoPortal on a Digital Ocean VM

Single Ubuntu droplet, nginx terminates TLS, backend runs in Docker, frontend is static files in `/var/www/najaecho/dist`, database is DO managed Postgres reached over SSL. CI/CD via `.github/workflows/deploy.yml`.

## One-time VM bootstrap

Run as root (or with sudo). Replace `<domain>` and `<owner>` with real values.

### 1. Install packages

```bash
apt update
apt install -y nginx docker.io docker-compose-plugin certbot python3-certbot-nginx curl
systemctl enable --now docker nginx
```

### 2. Create the deploy user

```bash
adduser --disabled-password --gecos "" deploy
usermod -aG docker deploy
mkdir -p /home/deploy/.ssh
# Paste the public half of the CI deploy key here:
echo "ssh-ed25519 AAAA... ci@najaecho" >> /home/deploy/.ssh/authorized_keys
chown -R deploy:deploy /home/deploy/.ssh
chmod 700 /home/deploy/.ssh
chmod 600 /home/deploy/.ssh/authorized_keys
```

Grant the deploy user passwordless sudo *only* for the deploy script:

```bash
cat >/etc/sudoers.d/najaecho-deploy <<'EOF'
deploy ALL=(root) NOPASSWD: /opt/najaecho/deploy.sh
EOF
chmod 440 /etc/sudoers.d/najaecho-deploy
```

### 3. Layout

```bash
mkdir -p /opt/najaecho /var/www/najaecho /etc/najaecho
```

Copy `docker-compose.prod.yml` from this repo to `/opt/najaecho/docker-compose.yml`, edit `<owner>` to your GitHub org / user. Copy `deploy.sh` to `/opt/najaecho/deploy.sh`:

```bash
chmod 750 /opt/najaecho/deploy.sh
chown root:root /opt/najaecho/{docker-compose.yml,deploy.sh}
```

### 4. Secrets file

```bash
install -m 0600 -o root -g root /dev/null /etc/najaecho/najaecho.env
$EDITOR /etc/najaecho/najaecho.env
```

Required keys (note .NET's `Section__Key` env-var convention maps to `Section:Key` in `IConfiguration`):

```env
Discord__ClientId=...
Discord__ClientSecret=...
ConnectionStrings__Default=Host=<cluster>.b.db.ondigitalocean.com;Port=25060;Database=najaecho;Username=...;Password=...;SslMode=Require;Trust Server Certificate=true
Frontend__Origin=https://<domain>
```

### 5. GHCR pull credentials

The image is private by default. Log in once as root so the systemd-launched docker daemon can pull on behalf of the deploy user:

```bash
docker login ghcr.io -u <github-user> -p <PAT-with-read:packages>
# credentials cached at /root/.docker/config.json
```

(Alternative: make the GHCR package public and skip this step.)

### 6. Nginx + TLS

Place `nginx/najaecho.conf` from this repo at `/etc/nginx/sites-available/najaecho` after substituting `<domain>` in all three spots.

```bash
ln -s /etc/nginx/sites-available/najaecho /etc/nginx/sites-enabled/najaecho
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl reload nginx
certbot --nginx -d <domain> --agree-tos -m you@example.com --redirect
```

Certbot rewrites the site file to wire up the issued cert. Renewal is automatic via the `certbot.timer` systemd unit installed with the package.

### 7. Discord application

In the Discord developer portal, OAuth2 → Redirects, add:

```
https://<domain>/api/auth/discord/callback
```

### 8. GitHub repository secrets

Settings → Secrets and variables → Actions:

| Secret           | Value                                                       |
| ---------------- | ----------------------------------------------------------- |
| `DEPLOY_HOST`    | VM IP or hostname                                           |
| `DEPLOY_USER`    | `deploy`                                                    |
| `DEPLOY_SSH_KEY` | Private half of the key whose public part is in step 2      |

`GITHUB_TOKEN` is provided automatically and is used to push to GHCR.

## Deploys

Push to `main`. The `deploy` workflow:

1. Builds and pushes `ghcr.io/<owner>/najaecho-api:<sha>` (and `:latest`).
2. Builds the SPA into `frontend-dist.tar.gz`.
3. Builds an EF migration bundle (`efbundle`).
4. `scp`'s both artifacts to `/tmp/najaecho-deploy/` on the VM.
5. `ssh`'s in and runs `sudo /opt/najaecho/deploy.sh <sha>`, which:
   - Sources `/etc/najaecho/najaecho.env`, runs `efbundle` against DO Postgres.
   - Pulls the new image and restarts the `api` container.
   - Atomically swaps `/var/www/najaecho/dist`.
   - Polls `/api/health` until it returns 200.

Rollback: re-run the workflow at an earlier commit, or `sudo IMAGE_TAG=<old-sha> docker compose -f /opt/najaecho/docker-compose.yml up -d api`. The previous SPA bundle is preserved at `/var/www/najaecho/dist.prev`.

## Verification

```bash
curl https://<domain>/api/health        # {"status":"ok"}
docker logs najaecho-api --tail 50      # Serilog JSON, no errors
```

Then browser-test: landing page loads, sign-in bounces through Discord and lands on `/dashboard`, sign-out returns to landing.
