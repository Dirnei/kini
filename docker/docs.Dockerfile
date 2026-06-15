# --- Kini docs container -------------------------------------------------
# Serves the OpenAPI specs and the Redoc-based HTML viewers behind nginx.
# Static image; nothing to build at runtime.

FROM nginx:1.31-alpine

COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docs/             /usr/share/nginx/html/docs/
COPY spec/             /usr/share/nginx/html/spec/

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s \
  CMD wget -qO- http://localhost/docs/ >/dev/null 2>&1 || exit 1
