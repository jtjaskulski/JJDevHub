# Build context is src/Clients so file:../shared/theme resolves.
FROM node:22-alpine AS build
WORKDIR /src

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
ENV CI=true

RUN corepack enable && corepack prepare pnpm@10.33.0 --activate

COPY shared/theme ./shared/theme
COPY web/package.json web/pnpm-lock.yaml ./web/

WORKDIR /src/web
RUN pnpm install --frozen-lockfile

COPY web/ ./
RUN pnpm build

FROM nginx:1.27-alpine AS runtime
COPY web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/web/dist/web/browser /usr/share/nginx/html
EXPOSE 80
