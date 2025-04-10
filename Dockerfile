# --- Build stage ---
    FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
    WORKDIR /app
    
    # Copy solution and restore as distinct layers
    COPY SG01G02_MVC.sln .
    COPY SG01G02_MVC.*/*.csproj ./
    RUN mkdir src && \
        mv SG01G02_MVC.*.csproj src/
    WORKDIR /app/src
    
    # Restore dependencies
    COPY . .
    RUN dotnet restore ../SG01G02_MVC.sln
    
    # Build and publish
    RUN dotnet publish SG01G02_MVC.Web/SG01G02_MVC.Web.csproj -c Release -o /app/publish
    
    # --- Runtime stage ---
    FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
    WORKDIR /app
    COPY --from=build /app/publish .
    
    # Expose port
    EXPOSE 80
    
    # Start application
    ENTRYPOINT ["dotnet", "SG01G02_MVC.Web.dll"]