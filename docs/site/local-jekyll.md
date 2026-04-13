---
render_with_liquid: false
---
# Running Jekyll Docs Locally (Docker)

Reusable recipe for previewing `just-the-docs` / GitHub Pages sites locally
using a throwaway `ruby:3.3` container. Works for any repo -- BlazorX, Hyperbee,
anything else that follows the same docs convention.

No Ruby install required. Docker Desktop is the only dependency.

---

## First-time setup (teammate onboarding)

Skip this section if you already have Docker and a bash shell working.

### 1. Install Docker Desktop

**Windows / macOS**: download from https://www.docker.com/products/docker-desktop/
and run the installer. Defaults are fine. On Windows it prompts to enable
WSL 2 -- accept.

**Linux**: install Docker Engine per your distro's instructions. You do
not need Docker Desktop specifically; any `docker` CLI that can pull
public images works.

**Licensing note**: Docker Desktop is free for personal use, education,
non-commercial open-source work, and companies with fewer than 250
employees AND less than $10M in annual revenue. Larger companies need
paid subscriptions. Check the current terms before using it in
a commercial setting.

### 2. Start Docker and verify

Launch Docker Desktop (Windows/macOS) and wait for the whale icon in
the system tray to settle. Then in a terminal:

```bash
docker --version
docker run --rm hello-world
```

The first command prints the Docker version. The second pulls and runs
a tiny test image -- if it prints "Hello from Docker!", you're set.

### 3. Pick a shell

The `docker run` command below works from any shell, but the
`MSYS_NO_PATHCONV=1` prefix is a Git Bash specific workaround for a
path-translation bug on Windows. Your options:

- **Git Bash** (Windows) -- use the commands as written. Get it bundled
  with Git for Windows at https://git-scm.com/
- **WSL 2** (Windows) -- drop the `MSYS_NO_PATHCONV=1` prefix; WSL
  doesn't need it. Paths also change from `C:/...` to `/mnt/c/...`.
- **PowerShell** (Windows) -- drop the prefix; use backticks instead
  of backslashes for line continuation.
- **macOS / Linux terminal** -- drop the prefix; paths are already
  POSIX.

### 4. Pre-pull the image (optional but recommended)

The first `docker run` in the Quick-start section pulls `ruby:3.3`
(~900 MB). You can do this explicitly up front so the first actual
use starts fast:

```bash
docker pull ruby:3.3
```

After this, every container we spin up reuses the cached image.

---

## Prerequisites (for a running environment)

1. **Docker running** -- verified via `docker --version` and `docker info`.
2. A project with a `docs/` folder (or `docs/site/`, or wherever you keep
   Jekyll sources) containing:
   - `_config.yml`
   - `Gemfile`
   - Your markdown content.
3. Git Bash, WSL, or another bash-capable shell (the `MSYS_NO_PATHCONV`
   trick below is specifically for Git Bash on Windows).

---

## Quick-start command

From the project root, pick a port and run:

```bash
MSYS_NO_PATHCONV=1 docker run --rm --name my-jekyll \
  -v "C:/path/to/project/docs/site:/srv/jekyll" \
  -w /srv/jekyll -p 4000:4000 \
  ruby:3.3 \
  sh -c "bundle install --quiet && bundle exec jekyll serve --host 0.0.0.0 --force_polling"
```

Then open `http://localhost:4000/<baseurl>/` in your browser, where
`<baseurl>` is whatever `baseurl` is set to in `_config.yml`
(for example, `/blazorx/`).

**What each flag does**:
- `MSYS_NO_PATHCONV=1` -- stops Git Bash from rewriting the Windows path
  into a POSIX-looking path that Docker then fails to mount.
- `--rm` -- container is deleted when it stops. No cleanup needed.
- `--name my-jekyll` -- optional, lets you `docker stop my-jekyll` later.
- `-v <host-path>:/srv/jekyll` -- bind-mounts your docs folder into the
  container's working directory.
- `-w /srv/jekyll` -- makes that the starting directory.
- `-p 4000:4000` -- publishes port 4000 on your host. Change the left side
  if 4000 is taken.
- `ruby:3.3` -- the Debian/glibc Ruby 3.3 image. Avoids the Alpine/musl
  sass-embedded broken-pipe bug.
- `sh -c "bundle install --quiet && bundle exec jekyll serve ..."` --
  installs gems, then runs Jekyll in watch mode.
- `--host 0.0.0.0` -- needed so the host machine can reach the server
  through the port mapping.
- `--force_polling` -- needed on Windows bind mounts for file-change
  detection to work reliably.

First run downloads `ruby:3.3` (~900 MB). Subsequent runs start in seconds
because the image is cached locally.

---

## What each repo needs

### `docs/<path>/_config.yml`

Minimum viable starting point:

```yaml
title: My Project
description: One-line project description
remote_theme: pmarsceill/just-the-docs
baseurl: "/myproject/"
url: "https://example.github.io"

aux_links:
  "GitHub Repository":
    - "//github.com/my-org/my-project"

search_enabled: true
color_scheme: dark

markdown: kramdown
highlighter: rouge
permalink: pretty

plugins:
  - jekyll-remote-theme
  - jekyll-include-cache
  - jekyll-seo-tag
  - jekyll-relative-links

relative_links:
  enabled: true
  collections: true

# Optional: Mermaid diagram support (just-the-docs >= 0.5)
mermaid:
  version: "10.9.0"

defaults:
  - scope:
      path: ""
    values:
      layout: "default"
  # Disable Liquid rendering inside content pages so code fences
  # containing {{ or }} don't trip the template engine. Adjust the
  # paths to match your directory layout.
  - scope:
      path: "getting-started"
    values:
      render_with_liquid: false
  - scope:
      path: "guides"
    values:
      render_with_liquid: false
  - scope:
      path: "reference"
    values:
      render_with_liquid: false
  - scope:
      path: "index.md"
    values:
      render_with_liquid: false
```

### `docs/<path>/Gemfile`

```ruby
source "https://rubygems.org"

gem "jekyll", "~> 4.3"
gem "just-the-docs"

group :jekyll_plugins do
  gem "jekyll-remote-theme"
  gem "jekyll-include-cache"
  gem "jekyll-seo-tag"
  gem "jekyll-relative-links"
end

gem "webrick", "~> 1.8"
```

### `.gitignore` entries

Add to the repo's root `.gitignore`:

```
# Jekyll / GitHub Pages local build
_site/
.jekyll-cache/
.jekyll-metadata
.sass-cache/
vendor/
.bundle/
docs/site/Gemfile.lock
```

Adjust `docs/site/Gemfile.lock` to match your docs path.

### GitHub Pages workflow (optional, for publishing)

`.github/workflows/deploy-gh-pages.yml`:

```yaml
name: Deploy GitHub Pages

on:
  push:
    branches: ["main"]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/configure-pages@v5
      - uses: actions/jekyll-build-pages@v1
        with:
          source: ./docs/site
          destination: ./_site
      - uses: actions/upload-pages-artifact@v3

  deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    needs: build
    steps:
      - id: deployment
        uses: actions/deploy-pages@v4
```

Set `source:` to wherever your docs live (for example `./docs` or `./docs/site`).

---

## Running multiple projects in parallel

Each project gets its own container name and its own host port. The
`ruby:3.3` image is shared across all of them.

```bash
# BlazorX on port 4000
MSYS_NO_PATHCONV=1 docker run --rm --name bzx-jekyll \
  -v "C:/Development/playground/BlazorX/docs/site:/srv/jekyll" \
  -w /srv/jekyll -p 4000:4000 ruby:3.3 \
  sh -c "bundle install --quiet && bundle exec jekyll serve --host 0.0.0.0 --force_polling"

# Hyperbee.Json on port 4001
MSYS_NO_PATHCONV=1 docker run --rm --name hbjson-jekyll \
  -v "C:/Development/hyperbee.json/docs:/srv/jekyll" \
  -w /srv/jekyll -p 4001:4000 ruby:3.3 \
  sh -c "bundle install --quiet && bundle exec jekyll serve --host 0.0.0.0 --force_polling"
```

Browse them at `http://localhost:4000/<baseurl>/` and
`http://localhost:4001/<baseurl>/` simultaneously.

---

## Reusable helper script

Drop this at a well-known location (for example `C:/Development/tools/jekyll-serve.sh`):

```bash
#!/usr/bin/env bash
# Start a Jekyll dev container for any project.
# Usage: jekyll-serve.sh [docs-dir] [host-port]
set -eu

DOCS_DIR="${1:-$(pwd)/docs/site}"
PORT="${2:-4000}"

# Absolute path for Docker bind mount
DOCS_ABS="$(cd "$DOCS_DIR" && pwd)"

# Pick a reasonable container name from the parent folder
NAME="jekyll-$(basename "$(dirname "$DOCS_ABS")")-$PORT"

echo "Serving $DOCS_ABS on http://localhost:$PORT/"
echo "Container: $NAME"
echo "Stop with: docker stop $NAME"

MSYS_NO_PATHCONV=1 docker run --rm --name "$NAME" \
  -v "${DOCS_ABS}:/srv/jekyll" \
  -w /srv/jekyll -p "${PORT}:4000" \
  ruby:3.3 \
  sh -c "bundle install --quiet && bundle exec jekyll serve --host 0.0.0.0 --force_polling"
```

Then from any repo:

```bash
# Uses ./docs/site, port 4000
~/dev/tools/jekyll-serve.sh

# Custom path
~/dev/tools/jekyll-serve.sh ./docs

# Custom path and port
~/dev/tools/jekyll-serve.sh ./docs 4001
```

---

## Speeding up repeat starts (optional)

On each cold start the container runs `bundle install` and fetches about
40 gems. That's ~20 seconds. Cache the bundle directory to reuse across
runs by adding a volume for `/usr/local/bundle`:

```bash
MSYS_NO_PATHCONV=1 docker run --rm --name my-jekyll \
  -v "C:/path/to/docs:/srv/jekyll" \
  -v "jekyll-bundle-cache:/usr/local/bundle" \
  -w /srv/jekyll -p 4000:4000 ruby:3.3 \
  sh -c "bundle install --quiet && bundle exec jekyll serve --host 0.0.0.0 --force_polling"
```

`jekyll-bundle-cache` is a named Docker volume. It persists across
containers and projects. Remove with `docker volume rm jekyll-bundle-cache`.

---

## Troubleshooting

### `docker: command not found`

Docker is not installed or not on `PATH`. See the First-time setup
section above. On Windows, restart your terminal after installing
Docker Desktop so `PATH` picks up the new entries.

### `Cannot connect to the Docker daemon`

Docker is installed but not running. On Windows/macOS, launch
Docker Desktop and wait for the whale icon to stop animating. On
Linux, `sudo systemctl start docker` (or `service docker start`).

### "Could not locate Gemfile"

The bind mount did not land where the container expected. On Git Bash you
need both the `MSYS_NO_PATHCONV=1` prefix and a Windows-style absolute
path like `C:/path/...` (forward slashes).

### Port already in use

Another process or container has the host port. Check with
`docker ps --filter "publish=4000"` or just pick a different port on the
left side of `-p host:container`.

### Mermaid diagrams don't render

Your `_config.yml` needs a `mermaid:` block with a `version:` key, and
your markdown must use ``` ```mermaid ``` fenced blocks. The theme
JavaScript replaces the code block with rendered SVG on page load --
nothing needs to run server-side.

### SCSS errors ("expected }")

You probably set `render_with_liquid: false` too broadly. Scope it to
content directories only (see the `defaults` block in `_config.yml`
above). The theme's SCSS files genuinely need Liquid for
`{% include %}` directives.

### Links ending in `.md` return 404

Install `jekyll-relative-links` (see `Gemfile` and `_config.yml`
templates above). Without it, Jekyll does not rewrite `[text](foo.md)`
links to the final rendered URL.

### File changes not picked up

Windows bind mounts need `--force_polling` on the `jekyll serve` command.
Without it, Jekyll uses inotify/fsevent which do not fire across the
Docker VM boundary.

### `_config.yml` edits seem to be ignored

Jekyll does not auto-reload `_config.yml` even with `--force_polling`.
Stop and restart the container to pick up changes.

---

## Stopping the container

```bash
docker stop my-jekyll
```

`--rm` in the run command means the container is removed automatically
on stop. No manual cleanup.
