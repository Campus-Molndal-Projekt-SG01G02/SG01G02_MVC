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
│   └── Services/
│
├── SG01G02_MVC.Infrastructure/
│   ├── Data/
│   ├── Repositories/
│   └── External/
│
├── SG01G02_MVC.Web/
│   ├── Controllers/
│   ├── Views/
│   ├── wwwroot/
│   ├── appsettings.json
│   └── Program.cs
│
└── SG01G02_MVC.sln
```