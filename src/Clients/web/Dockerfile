# Stage 1: Build
FROM node:20-alpine AS build
WORKDIR /app

# Kopiujemy pliki zależności, aby wykorzystać cache Dockera
COPY package*.json ./
RUN npm install

# Kopiujemy resztę kodu i budujemy projekt
COPY . .
RUN npm run build -- --configuration production

# Stage 2: Serve
FROM nginx:alpine
# Kopiujemy wynik builda do domyślnego folderu Nginxa
COPY --from=build /app/dist/web/browser /usr/share/nginx/html

# Kopiujemy Twoją customową konfigurację Nginxa (jeśli chcesz specyficzny routing)
# COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
