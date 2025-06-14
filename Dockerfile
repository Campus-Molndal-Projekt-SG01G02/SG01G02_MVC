# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy everything into the container
COPY . .

# Restore dependencies
RUN dotnet restore SG01G02_MVC.sln

# Fix project references
RUN dotnet build SG01G02_MVC.sln -c Release --no-restore

# Build and publish only the Web layer
RUN dotnet publish SG01G02_MVC.Web/SG01G02_MVC.Web.csproj -c Release -o /app/publish

# --- Runtime stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Installera curl för healthcheck
RUN apt-get update && apt-get install -y curl && apt-get clean

# Set environment variable from build-arg
ARG KEY_VAULT_NAME
ENV KEY_VAULT_NAME=${KEY_VAULT_NAME}
ARG POSTGRES_CONNECTION_STRING
ENV POSTGRES_CONNECTION_STRING=${POSTGRES_CONNECTION_STRING}
ARG BLOB_CONNECTION_STRING
ENV BLOB_CONNECTION_STRING=${BLOB_CONNECTION_STRING}

# For local development in Docker
ENV USE_POSTGRES_IN_DEV=false

# Expose port
EXPOSE 80
EXPOSE 8080

# Start application
ENTRYPOINT ["dotnet", "SG01G02_MVC.Web.dll"]