# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files and install dependencies
COPY package*.json ./
RUN npm install

# Copy source and build
COPY . .
RUN npm run build -- --configuration production

# Runtime stage
FROM nginx:alpine
# Angular 19 might output to dist/sharky-parser.web/browser
COPY --from=build /app/dist/sharky-parser.web/browser /usr/share/nginx/html
# If the above fails in some versions, it might be just dist/sharky-parser.web
# COPY --from=build /app/dist/sharky-parser.web /usr/share/nginx/html

COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
