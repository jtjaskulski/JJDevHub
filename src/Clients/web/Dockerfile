# Build context is src/Clients so file:../shared/theme resolves.
FROM node:26-alpine AS build
WORKDIR /src

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
ENV CI=true

# Node 26 no longer ships corepack, so `corepack enable` exits 127.
RUN npm install -g pnpm@10.33.0

COPY shared/theme ./shared/theme
COPY web/package.json web/pnpm-lock.yaml ./web/

WORKDIR /src/web
RUN pnpm install --frozen-lockfile

COPY web/ ./
RUN pnpm build

FROM nginx:1.31-alpine AS runtime
# Tag still ships libexpat 2.8.4-r0 (CVE-2026-93990), pcre2 10.48-r0 (CVE-2026-103111),
# tiff 4.7.1-r0 (CVE-2026-4775), libx11 1.8.13-r0 (CVE-2026-88806),
# and brotli-libs 1.2.0-r1 (CVE-2026-111497).
# Alpine 3.24 has libexpat 2.8.5-r0, pcre2 10.49-r0, tiff 4.7.2-r0, libx11 1.8.14-r0,
# and brotli-libs 1.2.1-r0.
RUN apk upgrade --no-cache libexpat pcre2 tiff libx11 brotli-libs
COPY web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/web/dist /usr/share/nginx/html
EXPOSE 80
