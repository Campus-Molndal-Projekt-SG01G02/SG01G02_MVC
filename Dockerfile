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

# Set environment variable from build-arg
ARG POSTGRES_CONNECTION_STRING
ARG KEY_VAULT_NAME
ENV POSTGRES_CONNECTION_STRING=$POSTGRES_CONNECTION_STRING
ENV KEY_VAULT_NAME=$KEY_VAULT_NAME

# Expose port
EXPOSE 80
EXPOSE 8080

# Start application
ENTRYPOINT ["dotnet", "SG01G02_MVC.Web.dll"]