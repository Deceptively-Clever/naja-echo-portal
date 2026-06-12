#!/usr/bin/env bash
# /opt/najaecho/deploy.sh — invoked over SSH by GitHub Actions.
# Usage: deploy.sh <image-tag>

set -euo pipefail

TAG="${1:?image tag required}"
DEPLOY_DIR=/tmp/najaecho-deploy
ENV_FILE=/etc/najaecho/najaecho.env
COMPOSE_DIR=/opt/najaecho
WWW_ROOT=/var/www/najaecho

[[ -f "$ENV_FILE" ]] || { echo "missing $ENV_FILE" >&2; exit 1; }
[[ -d "$DEPLOY_DIR" ]] || { echo "missing $DEPLOY_DIR (scp step)" >&2; exit 1; }

echo "==> Running EF migration bundle"
chmod +x "$DEPLOY_DIR/efbundle"
set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a
"$DEPLOY_DIR/efbundle" --connection "$ConnectionStrings__Default"

echo "==> Pulling and starting API image $TAG"
cd "$COMPOSE_DIR"
IMAGE_TAG="$TAG" docker compose pull api
IMAGE_TAG="$TAG" docker compose up -d api

echo "==> Swapping frontend bundle"
NEW="$WWW_ROOT/dist.new"
OLD="$WWW_ROOT/dist"
PREV="$WWW_ROOT/dist.prev"

rm -rf "$NEW"
mkdir -p "$NEW"
tar -xzf "$DEPLOY_DIR/frontend-dist.tar.gz" -C "$NEW"

rm -rf "$PREV"
if [[ -d "$OLD" ]]; then mv "$OLD" "$PREV"; fi
mv "$NEW" "$OLD"

echo "==> Health check"
for i in {1..10}; do
    if curl -fsS http://127.0.0.1:5180/api/health >/dev/null; then
        echo "==> Healthy."
        rm -rf "$DEPLOY_DIR"
        exit 0
    fi
    sleep 2
done

echo "Health check failed; check 'docker logs najaecho-api'." >&2
exit 1
