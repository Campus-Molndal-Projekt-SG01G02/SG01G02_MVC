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
│   ├── DTOs/
│   │   └── ProductDto.cs                           - Data Transfer Object used in Application and Web layers
│   ├── Interfaces/
│   │   ├── IAuthService.cs                         - Auth abstraction for login validation and session-aware auth
│   │   ├── IProductRepository.cs                   - Repository pattern abstraction for fetching products (Infrastructure will implement)
│   │   ├── IProductService.cs                      - Application service contract defining product-related use cases (used by Web layer)
│   │   └── IUserRepository.cs                      - Interface to access and validate user credentials from a data source
│   ├── Services/
│   │   ├── AuthService.cs                          - Role-based login logic, delegates to IUserRepository
│   │   └── ProductService.cs                       - Implements IProductService, delegates to repository
│   └── SG01G02_MVC.Application.csproj
│
├── SG01G02_MVC.Domain/
│   ├── Entities/
│   │   ├── AppUser.cs                              - Represents user identity and role (Admin, Staff, etc.)
│   │   ├── CartItem.cs                             - Domain model for cart line item
│   │   ├── Order.cs                                - Domain model for customer order
│   │   └── Product.cs                              - Core DDD entity, no EF or DTO logic
│   └── SG01G02_MVC.Domain.csproj
│
├── SG01G02_MVC.Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs                         - EF Core DbContext for managing database access
│   ├── External/
│   ├── Migrations/                                 - EF Migrations
│   ├── Repositories/
│   │   ├── EfProductRepository.cs                  - TODO:
│   │   ├── ProductRepository.cs                    - Implements IProductRepository using EF Core
│   │   └── UserRepository.cs                       - Implements IUserRepository for validating users from the database
│   ├── Services/
│   │   └── BlobStorageService.cs                   - Handles image uploads via Azure Blob Storage (stubbed for MVP)
│   └── SG01G02_MVC.Infrastructure.csproj
│
├── SG01G02_MVC.Tests/
│   ├── Controllers/
│   │   ├── AdminControllerTests.cs                 - Unit tests for Admin access and redirection logic
│   │   └── LoginControllerTests.cs                 - Unit tests for login flow using mock services
│   ├── Helpers/
│   │   ├── TestBase.cs                             - TODO: 
│   │   └── TestDbContextFactory.cs                 - TODO: 
│   ├── Services/
│   │   ├── AuthServiceTests.cs                     - TDD tests for role-based login logic
│   │   ├── FakeProductRepository.cs                - In-memory test double for repository logic
│   │   └── ProductServiceTests.cs                  - TDD-driven tests for ProductService
│   └── SG01G02_MVC.Infrastructure.Tests/
│
├── SG01G02_MVC.Web/
│   ├── Controllers/
│   │   ├── AdminController.cs                      - Admin panel for CRUD operations (Index, Create, Edit, Delete)
│   │   ├── CartController.cs                       - Placeholder controller for future shopping cart logic
│   │   ├── CatalogueController.cs                  - Handles product listing and detail views
│   │   ├── HomeController.cs                       - Default MVC controller for routing landing page and basic views
│   │   ├── ImageController.cs                      - Handles image upload/delete (API)
│   │   ├── LoginController.cs                      - Shared login/logout for all roles
│   │   └── StaffController.cs                      - Read-only dashboard for staff to view orders (future)
│   ├── Models/
│   │   ├── ErrorViewModel.cs                       - ViewModel for error page rendering // TODO: not used yet
│   │   ├── LoginViewModel.cs                       - ViewModel for login form input validation
│   │   └── ProductViewModel.cs                     - Presentation model used in views for products
│   ├── Services/
│   │   ├── IUserSessionService.cs                  - Interface for abstracting session access (username, role)
│   │   ├── SeederHelper.cs                         - Seeds default admin user on startup (used in Program.cs)
│   │   └── UserSessionService.cs                   - Wraps access to session data (role, username)
│   ├── Views/
│   │   ├── Admin/
│   │   │   ├── Create.cshtml                       - Form to create a new product
│   │   │   ├── Delete.cshtml                       - Confirmation page for product deletion
│   │   │   ├── Edit.cshtml                         - Form to edit existing product
│   │   │   └── Index.cshtml                        - Admin dashboard showing list of products
│   │   ├── Cart/
│   │   │   └── Index.cshtml                        - Placeholder view for shopping cart
│   │   ├── Catalogue/
│   │   │   ├── Details.cshtml                      - Razor view showing a single product
│   │   │   └── Index.cshtml                        - Razor view listing all products
│   │   ├── Home/
│   │   │   └── Index.cshtml                        - Razor view for landing page (MVP placeholder)
│   │   ├── Login/
│   │   │   └── Index.cshtml                        - Login form view for credential input
│   │   ├── Staff/
│   │   │   ├── Index.cshtml                        - Staff dashboard placeholder
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml                      - Shared HTML layout with Bootstrap navigation and structure
│   │   │   ├── _ValidationSScriptPartial.cshtml    - Script partial for client-side validation (TODO: review use)
│   │   │   ├── DatabaseUnavailable.cshtml          - Fallback view if database connection fails (CI/CD safe)
│   │   │   └── Error.cshtml                        - Default error handling page
│   │   ├── _ViewImports.cshtml                     - Razor namespace imports for views
│   │   └── _ViewStart.cshtml                       - Razor startup configuration for view rendering
│   ├── wwwroot/                                    - Static content root (CSS, JS, images)
│   ├── appsettings.Development.json                - Environmentals for local development
│   ├── appsettings.json                            - Base configuration shared across environments
│   ├── Program.cs                                  - .NET application entry point (configures Web host and services)
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

# Local Development Database (SQLite)
### This section is for developers only.  
Production environments always use PostgreSQL (via Azure Key Vault / CI/CD).  
SQLite is used only as a fallback for local development.  
  
### How to Apply EF Core Migrations Locally (SQLite)
1. Add migration if needed:

```bash
dotnet ef migrations add <MigrationName> --project SG01G02_MVC.Infrastructure --startup-project SG01G02_MVC.Web
```

2. Apply migration to your local SQLite dev database:

```bash
dotnet ef database update --project SG01G02_MVC.Infrastructure --startup-project SG01G02_MVC.Web
```

### Security Warning:
- The SQLite .db file must never be committed to Git or uploaded to any shared storage.
- The database contains seeded login credentials and is for dev use only.
- Check that .gitignore includes: *.db

---

## Log-in rules:

We have three roles (admin, staff, customer) which is handled through the database. Access rules are controlled via session and role handling in Razor. Session is 30 minutes or until log-off and roles are set upon validation on log-in. Using Data annotation and safety measures like hashed passwords to handle secure logins.

---

## Team behind this project, their roles and responsibilities:

### IPL handles planning and project management  
Project Leader: Anton Lindgren  
Project Leader: Olof Bengtsson  
Project Leader: Pierre Nilsson  
  
### MOV sets up infrastructure in Microsoft 365  
MOV/MS 365 Technician: Max Oredson  
MOV/MS 365 Technician: Pontus Kroon  
  
### JIN/External "consultant" API for the Review mechanics and integration  
  
### CLO handles the Azure infrastructures, CI/CD pipeline and software development  
CI/CD: Fredrik Svärd - Terraform (infrastructure), Ansible (configuration), Azure Cloud, Azure Keyvault, Azure Blob storage for images and state, GitHub Secrets. 
Fullstack Developer: Niklas Häll - .NET (backend and Razor/bootstrap frontend), PostgreSQL, Technical documentation for the Project Leading team  