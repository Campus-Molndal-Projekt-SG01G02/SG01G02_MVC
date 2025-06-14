using SG01G02_MVC.Application.Interfaces;
using SG01G02_MVC.Infrastructure;
using SG01G02_MVC.Infrastructure.Services;
using SG01G02_MVC.Infrastructure.Configuration;
using SG01G02_MVC.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load the application configuration
ConfigurationLoader.LoadAppConfiguration(builder);

// Set the default culture for the application
var cultureInfo = new System.Globalization.CultureInfo("sv-SE");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// Create configurator objects (instead of calling void methods)
var keyVaultConfig = new KeyVaultConfigurator();
var databaseConfig = new DatabaseConfigurator();
var blobStorageConfig = new BlobStorageConfigurator();
var servicesConfig = new ServicesConfigurator();
var appConfig = new AppConfigurator();

// Configure using the objects
keyVaultConfig.Configure(builder);

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Configure database and blob storage services
servicesConfig.Configure(builder, keyVaultConfig, databaseConfig, blobStorageConfig);

var app = builder.Build();

// Configure the HTTP request pipeline 
appConfig.Configure(app, databaseConfig);

app.Run();