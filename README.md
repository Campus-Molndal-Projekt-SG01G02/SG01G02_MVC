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
│   │   └── IProductService.cs
│   ├── Services/
│   └── SG01G02_MVC.Application.csproj
│
├── SG01G02_MVC.Domain/
│   ├── Entities/
│   │   ├── CartItem.cs
│   │   ├── Order.cs
│   │   └── Product.cs - DDD-core business concept, no framework dependency (EF) or DTO logic, only definition
│   └── SG01G02_MVC.Domain.csproj
│
├── SG01G02_MVC.Infrastructure/
│   ├── Data/
│   │   └── DbContextPlaceholder.cs
│   ├── External/
│   ├── Repositories/
│   └── SG01G02_MVC.Infrastructure.csproj
│
├── SG01G02_MVC.Web/
│   ├── Controllers/
│   │   └── HomeController.cs
│   ├── Models/
│   │   └── ErrorViewModel.cs
│   ├── Views/
│   │   ├── Home/
│   │   │   ├── Index.cshtml
│   │   │   └── Privacy.cshtml
│   │   └── Shared/
│   │       ├── _Layout.cshtml
│   │       ├── _ViewImports.cshtml
│   │       └── _ViewStart.cshtml
│   ├── wwwroot/
│   ├── appsettings.Development.json
│   ├── appsettings.json
│   ├── Program.cs
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