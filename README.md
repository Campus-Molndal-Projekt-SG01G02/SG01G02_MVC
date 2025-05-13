# SG01G02_MVC
[![Build and Test](https://github.com/Campus-Molndal-Projekt-SG01G02/SG01G02_MVC/actions/workflows/build-test-deploy.yml/badge.svg)](https://github.com/Campus-Molndal-Projekt-SG01G02/SG01G02_MVC/actions/workflows/build-test-deploy.yml)


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
│   │   ├── ProductDto.cs                           - Data Transfer Object for product data
│   │   └── ReviewDto.cs                            - DTO for product reviews and ratings
│   ├── Interfaces/
│   │   ├── IAuthService.cs                         - Authentication service contract
│   │   ├── IProductRepository.cs                   - Product data access contract
│   │   ├── IProductService.cs                      - Product business logic contract
│   │   ├── IReviewApiClient.cs                     - TODO:  
│   │   ├── IReviewService.cs                       - Review management contract
│   │   └── IUserRepository.cs                      - User data access contract
│   ├── Services/
│   │   ├── AuthService.cs                          - Authentication implementation
│   │   ├── ProductService.cs                       - Product business logic implementation
│   │   └── ReviewService.cs                        - TODO:  
│   └── SG01G02_MVC.Application.csproj
│
├── SG01G02_MVC.Domain/
│   ├── Entities/
│   │   ├── AppUser.cs                              - User entity with role-based access
│   │   ├── CartItem.cs                             - Shopping cart line item entity
│   │   ├── Order.cs                                - Customer order entity
│   │   └── Product.cs                              - Product entity with business rules
│   └── SG01G02_MVC.Domain.csproj
│
├── SG01G02_MVC.Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs                         - Entity Framework Core context
│   ├── External/
│   │   └── ReviewApiClient.cs                      - TODO:  
│   ├── Migrations/                                 - Entity Framework - Database migrations
│   ├── Repositories/
│   │   ├── CartRepository.cs                       - Shopping cart data access
│   │   ├── EfProductRepository.cs                  - EF Core product repository
│   │   ├── OrderRepository.cs                      - Order data access
│   │   ├── ReviewRepository.cs                     - Review data access
│   │   └── UserRepository.cs                       - User data access implementation
│   ├── Services/
│   │   ├── BlobStorageService.cs                   - Azure Blob Storage integration
│   │   └── DatabaseHealthCheck.cs                  - Database connectivity monitoring
│   └── SG01G02_MVC.Infrastructure.csproj
│
├── SG01G02_MVC.Tests/
│   ├── Controllers/
│   │   ├── AdminControllerTests.cs                 - Admin functionality tests
│   │   ├── LoginControllerTests.cs                 - Authentication flow tests
│   │   └── ReviewControllerTests.cs                - TODO:  
│   ├── Helpers/
│   │   ├── TestBase.cs                             - Common test setup and utilities
│   │   └── TestDbContextFactory.cs                 - Test database context factory
│   ├── Services/
│   │   ├── AuthServiceTests.cs                     - Authentication logic tests
│   │   ├── FakeProductRepository.cs                - In-memory product repository
│   │   ├── ProductServiceTests.cs                  - Product business logic tests
│   │   └── ReviewServiceTests.cs                   - TODO:  
│   └── SG01G02_MVC.Infrastructure.Tests/
│
├── SG01G02_MVC.Web/
│   ├── Controllers/
│   │   ├── AdminController.cs                      - Product management
│   │   ├── CartController.cs                       - Shopping cart operations
│   │   ├── CatalogueController.cs                  - Product browsing
│   │   ├── HomeController.cs                       - Landing page and navigation
│   │   ├── ImageController.cs                      - Image upload management
│   │   ├── LoginController.cs                      - Authentication handling
│   │   ├── ReviewController.cs                     - TODO:  
│   │   └── StaffController.cs                      - Order management
│   ├── Models/
│   │   ├── ErrorViewModel.cs                       - Error page data
│   │   ├── LoginViewModel.cs                       - Login form data
│   │   ├── ProductViewModel.cs                     - Product display data
│   │   └── ReviewSubmissionViewModel.cs            - TODO:  
│   ├── Services/
│   │   ├── IUserSessionService.cs                  - Session management contract
│   │   ├── SeederHelper.cs                         - Initial data seeding
│   │   └── UserSessionService.cs                   - Session implementation
│   ├── Views/
│   │   ├── Admin/
│   │   │   ├── Create.cshtml                       - Product creation form
│   │   │   ├── Delete.cshtml                       - Product deletion confirmation
│   │   │   ├── Edit.cshtml                         - Product editing form
│   │   │   └── Index.cshtml                        - Product management dashboard
│   │   ├── Cart/
│   │   │   └── Index.cshtml                        - Shopping cart view
│   │   ├── Catalogue/
│   │   │   ├── Details.cshtml                      - Product details view
│   │   │   └── Index.cshtml                        - Product listing view
│   │   ├── Home/
│   │   │   └── Index.cshtml                        - Landing page view
│   │   ├── Login/
│   │   │   └── Index.cshtml                        - Login form view
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml                      - Master page template
│   │   │   ├── _ValidationScriptsPartial.cshtml    - Client-side validation
│   │   │   ├── DatabaseUnavailable.cshtml          - Database error page
│   │   │   └── Error.cshtml                        - Error page template
│   │   ├── Staff/
│   │   │   ├── Edit.cshtml                         - Order management view
│   │   │   └── Index.cshtml                        - Staff dashboard
│   │   ├── _ViewImports.cshtml                     - View imports
│   │   └── _ViewStart.cshtml                       - View configuration
│   ├── wwwroot/                                    - Static files (CSS, JS, images)
│   ├── appsettings.Development.json                - Development configuration
│   ├── appsettings.json                            - Base configuration
│   ├── Program.cs                                  - Application startup
│   └── SG01G02_MVC.Web.csproj
│
├── SG01G02_MVC.sln
├── Dockerfile                                      - Container definition
├── docker-compose.yml                              - Multi-container setup
├── .gitignore                                      - Git ignore rules
└── README.md                                       - Project documentation
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

# Production Database (PostgreSQL)
The application uses PostgreSQL in production environments, with a fallback to SQLite only in development. The database configuration follows this priority:

1. Environment Variable: `POSTGRES_CONNECTION_STRING` (injected by CI/CD)
2. Azure Key Vault: `PostgresConnectionString` secret
3. Development Fallback: SQLite (only in Development environment)

### Database Provisioning Flow
1. **Production Deployment**:
   - Connection string is injected via CI/CD pipeline
   - Azure Key Vault integration for secure credential storage
   - Automatic migrations on application startup

2. **Database Migrations**:
   ```bash
   # List available migrations
   dotnet ef migrations list
   
   # Apply migrations to PostgreSQL
   dotnet ef database update
   ```

3. **Health Checks**:
   - Visit `/dbinfo` endpoint to verify database connectivity
   - Health check endpoint at `/health` monitors database status

### Security Notes
- Production credentials are managed through Azure Key Vault
- Connection strings are injected by CI/CD pipeline
- Database access is restricted to the application's network
- Regular security audits and updates are performed

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