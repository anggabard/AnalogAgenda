# Build stage
FROM node:20-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY . .

# Build the Angular application (docker config: apiUrl '' so /api is same-origin via nginx proxy)
RUN npm run build:docker

# Production stage with nginx
FROM nginx:alpine AS production

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built Angular app from build stage
COPY --from=build /app/dist/analogagenda.client/browser /usr/share/nginx/html

# Expose port 8080 (Azure Container Apps default)
EXPOSE 8080

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
