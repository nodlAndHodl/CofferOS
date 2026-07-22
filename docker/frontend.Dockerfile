# CofferOS frontend (React + Vite, served by nginx) — build context is the repo root.
FROM node:20-alpine AS build
WORKDIR /app

COPY src/frontend/cofferos-ui/package.json src/frontend/cofferos-ui/package-lock.json ./
RUN npm ci

COPY src/frontend/cofferos-ui/ ./
RUN npm run build

FROM nginx:alpine AS runtime
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
