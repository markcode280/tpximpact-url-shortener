# Build stage
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm test && npm run build

# Production stage – serve with Nginx
FROM nginx:stable-alpine AS runtime

# Copy build output
COPY --from=build /app/dist /usr/share/nginx/html

# Nginx config: proxy /shorten, /urls, and /{alias} to the API
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
