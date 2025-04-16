# SG01G02_MVC

## Project Structure
This solution is structured according to the principles of Clean Architecture and Domain-Driven Design (DDD) to ensure separation of concerns, modularity, and scalability. Each layer has a clearly defined responsibility:  
- Domain contains core business entities and logic.
- Application defines interfaces and orchestrates use cases.
- Infrastructure handles data access, external APIs, and integrations.
- Web serves as the entry point with Razor Views and MVC Controllers.
  
The structure below reflects this layered architecture:
  
```css
SG01G02_MVC/
├── SG01G02_MVC.Application/
│   ├── Interfaces/
│   │   └── IProductRepository.cs - Repository pattern abstraction for fetching products (Infrastructure will implement)
│   │   └── IProductService.cs - Application service contract defining product-related use cases (used by Web layer)
│   ├── Services/
│   │   └── ProductService.cs - Implements IProductService, contains use case logic, delegates to IProductRepository
│   └── SG01G02_MVC.Application.csproj
│
├── SG01G02_MVC.Domain/
│   ├── Entities/
│   │   ├── CartItem.cs - Domain model for cart line item
│   │   ├── Order.cs - Domain model for customer order
│   │   └── Product.cs - DDD-core business concept, no framework dependency (EF) or DTO logic, only definition
│   └── SG01G02_MVC.Domain.csproj
│
├── SG01G02_MVC.Infrastructure/
│   ├── Data/
│   │   └── DbContextPlaceholder.cs - Placeholder for EF Core DbContext (will manage DB access in Infrastructure)
│   ├── External/
│   ├── Repositories/
│   │   └── ProductRepository.cs - Implements IProductRepository using EF Core or mock data (depending on environment)
│   └── SG01G02_MVC.Infrastructure.csproj
│
├── SG01G02_MVC.Tests/
│   ├── Services/
│   │   └── ProductServiceTests.cs - Unit test for ProductService using stubbed dependencies (TDD-driven)
│   └── SG01G02_MVC.Infrastructure.Tests/
│
├── SG01G02_MVC.Web/
│   ├── Controllers/
│   │   └── HomeController.cs - Default MVC controller for routing landing page and basic views
│   ├── Models/
│   │   └── ErrorViewModel.cs - ViewModel used for default error page rendering
│   ├── Views/
│   │   ├── Home/
│   │   │   ├── Index.cshtml - Razor view for landing page (MVP placeholder)
│   │   └── Shared/
│   │       ├── _Layout.cshtml - Shared HTML layout with Bootstrap navigation and structure
│   │       ├── _ViewImports.cshtml - Razor namespace imports for views
│   │       └── _ViewStart.cshtml - Razor startup configuration for view rendering
│   ├── wwwroot/
│   ├── appsettings.Development.json - Development environment configuration
│   ├── appsettings.json - Base configuration shared across environments
│   ├── Program.cs - .NET application entry point (configures Web host and services)
│   └── SG01G02_MVC.Web.csproj
│
├── SG01G02_MVC.sln
├── Dockerfile
├── docker-compose.yml
├── .gitignore
└── README.md
```

---

## Build & Run (Dockerized)

The project can be built and run entirely through Docker using either:

### Run the application locally

#### To start the containerized Web app:  
```bash
docker-compose up
```
This will:  
- Run the published .NET MVC app using the image from Docker Hub
- Expose the application at http://localhost:8080
- No local database is started — the CI/CD pipeline will inject the connection string to PostgreSQL hosted externally
  
#### Build the Docker image locally  
```bash
docker build -t mymh/sg01g02mvc:latest .
```
#### Push to Docker Hub  
```bash
docker push mymh/sg01g02mvc:latest
```

# Ci/Cd Pipeline for MVC App
Builds the MVC App into a Docker Image and sends it to the Docker Hub. Watchtower is running in a Docker Container on the Azure VM Appserver that will pull the image and restart the container if it detects a new image. The app is running on port 8080. And it checks every 30 seconds for a new image. 

Can also be forced to pull the image by running the following command in the Azure VM Appserver:
```bash
curl -H "Authorization: Bearer $WATCHTOWER_TOKEN" -X POST http://$APP_IP:8080/v1/update
```

# Blob
TODO: Glöm inte lägga in "builder.Services.AddSingleton<BlobStorageService>();" i Program.cs.