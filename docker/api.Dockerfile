# --- Kini API + SPA container -------------------------------------------
# Multi-stage:
#   1. node-alpine builds the SPA (and generates its TS types from the spec).
#   2. dotnet/sdk:10 builds the API, with the SPA copied into wwwroot.
#   3. dotnet/aspnet:10-alpine runs the published artifact.

# syntax=docker/dockerfile:1.7

# ===== Stage 1: SPA build =================================================
FROM node:22-alpine AS web-build
WORKDIR /repo

# Install deps first for cacheable layer.
COPY src/Kini.Web/package*.json ./src/Kini.Web/
WORKDIR /repo/src/Kini.Web
RUN npm install --no-audit --no-fund

# Bring in the spec (needed by `npm run gen:api`) and the rest of the web tree.
WORKDIR /repo
COPY spec/ ./spec/
COPY src/Kini.Web/ ./src/Kini.Web/

WORKDIR /repo/src/Kini.Web
RUN npm run gen:api
RUN npm run build
# Vite writes to /repo/src/Kini.Api/wwwroot per vite.config outDir.

# ===== Stage 2: API build =================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /repo

COPY src/Kini.Api/ ./src/Kini.Api/
COPY spec/         ./spec/
COPY --from=web-build /repo/src/Kini.Api/wwwroot ./src/Kini.Api/wwwroot

WORKDIR /repo/src/Kini.Api
RUN dotnet restore
RUN dotnet publish -c Release -o /publish

# ===== Stage 3: Runtime ===================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# ssh-keygen verifies SSH-key sign-in signatures.
# gpg extracts fingerprint + UIDs from uploaded OpenPGP public keys.
RUN apk add --no-cache openssh-client gnupg

WORKDIR /app
COPY --from=api-build /publish ./

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER app

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s \
  CMD wget -qO- http://localhost:8080/healthz >/dev/null 2>&1 || exit 1

ENTRYPOINT ["dotnet", "Kini.Api.dll"]
