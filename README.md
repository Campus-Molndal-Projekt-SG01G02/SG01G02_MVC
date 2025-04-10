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
├── SG01G02_MVC.Domain/
│   └── Entities/
│       ├── Product.cs
│       ├── Order.cs
│       └── CartItem.cs
│
├── SG01G02_MVC.Application/
│   ├── Interfaces/
│   │   └── IProductService.cs
│   └── Services/
│       └── ProductService.cs
│
├── SG01G02_MVC.Infrastructure/
│   ├── Data/
│   │   └── DbContextPlaceholder.cs
│   ├── Repositories/
│   └── External/
│
├── SG01G02_MVC.Web/
│   ├── Controllers/
│   │   └── HomeController.cs
│   ├── Views/
│   │   └── Home/
│   │       ├── Index.cshtml
│   │       └── Privacy.cshtml
│   ├── Models/
│   │   └── ErrorViewModel.cs
│   ├── wwwroot/
│   ├── appsettings.json
│   └── Program.cs
│
├── SG01G02_MVC.sln
├── Dockerfile
├── docker-compose.yml
├── .env           # <-- Not tracked, keeping environment variables
└── README.md
```

---

## Build & Run (Dockerized)

The project can be built and run entirely through Docker using either:

### Local build (for dev/testing)

```bash
docker-compose up
```
This command:  
- Starts the .NET MVC application container (web)
- Starts a PostgreSQL container (db) with a persistent volume
- Injects credentials from a local .env file (not tracked)
Ensure your .env file is present with:  
```ini
POSTGRES_USER=your-user-name
POSTGRES_PASSWORD=not-my-password
```
### Build Docker image manually and push to Docker Hub
```bash
docker build -t mymh/sg01g02mvc:latest .
docker push mymh/sg01g02mvc:latest
```