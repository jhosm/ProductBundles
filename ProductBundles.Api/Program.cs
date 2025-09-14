using ProductBundles.Api.Models;
using ProductBundles.Api.Services;
using ProductBundles.Api.HealthChecks;
using ProductBundles.Core;
using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register ProductBundlesLoader as a singleton service
builder.Services.AddSingleton<ProductBundlesLoader>(serviceProvider =>
{
    var logger = serviceProvider.GetService<ILogger<ProductBundlesLoader>>();
    return new ProductBundlesLoader("plugins", logger);
});

// Register ProductBundle services with better composability
builder.Services
    .AddProductBundleJsonSerialization(jsonOptions => {
        jsonOptions.WriteIndented = true;
        jsonOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure storage (logging will be handled internally when DI container is available)
builder.Services.AddProductBundleStorageFromConfiguration(builder.Configuration);

// Register plugin resilience services with 30 second timeout
builder.Services.AddPluginResilience(TimeSpan.FromSeconds(30));

// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()); // Use in-memory storage for development

// Add health checks with storage connectivity and background job processing
builder.Services.AddHealthChecks()
    .AddProductBundleStorageHealthChecks(builder.Configuration)
    .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready" });

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "productbundles", "recurring", "default" };
});

// Register background job processing services
builder.Services.AddScoped<IBackgroundJobProcessor, ProductBundleBackgroundService>();
builder.Services.AddScoped<HangfireBackgroundJobWrapper>();
builder.Services.AddScoped<HangfireRecurringJobManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Hangfire middleware
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Initialize ProductBundle recurring jobs on startup
using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<HangfireRecurringJobManager>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        recurringJobManager.InitializeRecurringJobs();
        logger.LogInformation("Successfully initialized ProductBundle recurring jobs");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to initialize ProductBundle recurring jobs");
    }
}

// ProductBundles endpoint
app.MapGet("/ProductBundles", (ProductBundlesLoader loader) =>
{
    // Load all plugins if not already loaded
    var plugins = loader.LoadedPlugins.Any() ? loader.LoadedPlugins : loader.LoadPlugins();
    
    // Convert to DTOs
    var productBundleDtos = plugins.Select(plugin => new ProductBundleDto
    {
        Id = plugin.Id,
        FriendlyName = plugin.FriendlyName,
        Description = plugin.Description,
        Version = plugin.Version,
        Properties = plugin.Properties.Select(prop => new PropertyDto
        {
            Name = prop.Name,
            Description = prop.Description,
            DefaultValue = prop.DefaultValue,
            IsRequired = false // Default value since Property class doesn't have IsRequired
        }).ToList()
    }).ToList();
    
    return Results.Ok(productBundleDtos);
})
.WithName("GetProductBundles")
.WithOpenApi();

// Optional endpoint to get a specific ProductBundle by ID
app.MapGet("/ProductBundles/{id}", (string id, ProductBundlesLoader loader) =>
{
    var plugin = loader.GetPluginById(id);
    
    if (plugin == null)
    {
        return Results.NotFound($"ProductBundle with ID '{id}' not found");
    }
    
    var productBundleDto = new ProductBundleDto
    {
        Id = plugin.Id,
        FriendlyName = plugin.FriendlyName,
        Description = plugin.Description,
        Version = plugin.Version,
        Properties = plugin.Properties.Select(prop => new PropertyDto
        {
            Name = prop.Name,
            Description = prop.Description,
            DefaultValue = prop.DefaultValue,
            IsRequired = false // Default value since Property class doesn't have IsRequired
        }).ToList()
    };
    
    return Results.Ok(productBundleDto);
})
.WithName("GetProductBundleById")
.WithOpenApi();

// ProductBundleInstance CRUD endpoints

// GET specific ProductBundleInstance by ID
app.MapGet("/ProductBundleInstances/{id}", async (string id, IProductBundleInstanceStorage storage) =>
{
    var instance = await storage.GetAsync(id);
    
    if (instance == null)
    {
        return Results.NotFound($"ProductBundleInstance with ID '{id}' not found");
    }
    
    var dto = new ProductBundleInstanceDto
    {
        Id = instance.Id,
        ProductBundleId = instance.ProductBundleId,
        ProductBundleVersion = instance.ProductBundleVersion,
        Properties = instance.Properties
    };
    
    return Results.Ok(dto);
})
.WithName("GetProductBundleInstanceById")
.WithOpenApi();

// POST create new ProductBundleInstance
app.MapPost("/ProductBundleInstances", async (CreateProductBundleInstanceDto createDto, IProductBundleInstanceStorage storage) =>
{
    // Generate new ID for the instance
    var instanceId = Guid.NewGuid().ToString();
    
    var instance = new ProductBundleInstance(
        instanceId,
        createDto.ProductBundleId,
        createDto.ProductBundleVersion,
        createDto.Properties
    );
    
    var created = await storage.CreateAsync(instance);
    
    if (!created)
    {
        return Results.Conflict($"ProductBundleInstance with ID '{instanceId}' already exists");
    }
    
    var responseDto = new ProductBundleInstanceDto
    {
        Id = instance.Id,
        ProductBundleId = instance.ProductBundleId,
        ProductBundleVersion = instance.ProductBundleVersion,
        Properties = instance.Properties
    };
    
    return Results.Created($"/ProductBundleInstances/{instanceId}", responseDto);
})
.WithName("CreateProductBundleInstance")
.WithOpenApi();

// PUT update existing ProductBundleInstance
app.MapPut("/ProductBundleInstances/{id}", async (string id, UpdateProductBundleInstanceDto updateDto, IProductBundleInstanceStorage storage) =>
{
    // Check if the instance exists
    var existingInstance = await storage.GetAsync(id);
    if (existingInstance == null)
    {
        return Results.NotFound($"ProductBundleInstance with ID '{id}' not found");
    }
    
    // Update the instance (keeping the same ID and ProductBundleId)
    var updatedInstance = new ProductBundleInstance(
        id,
        existingInstance.ProductBundleId, // Keep original ProductBundleId
        updateDto.ProductBundleVersion,
        updateDto.Properties
    );
    
    var updated = await storage.UpdateAsync(updatedInstance);
    
    if (!updated)
    {
        return Results.Problem($"Failed to update ProductBundleInstance with ID '{id}'");
    }
    
    var responseDto = new ProductBundleInstanceDto
    {
        Id = updatedInstance.Id,
        ProductBundleId = updatedInstance.ProductBundleId,
        ProductBundleVersion = updatedInstance.ProductBundleVersion,
        Properties = updatedInstance.Properties
    };
    
    return Results.Ok(responseDto);
})
.WithName("UpdateProductBundleInstance")
.WithOpenApi();

// DELETE ProductBundleInstance
app.MapDelete("/ProductBundleInstances/{id}", async (string id, IProductBundleInstanceStorage storage) =>
{
    var deleted = await storage.DeleteAsync(id);
    
    if (!deleted)
    {
        return Results.NotFound($"ProductBundleInstance with ID '{id}' not found");
    }
    
    return Results.NoContent();
})
.WithName("DeleteProductBundleInstance")
.WithOpenApi();

// Basic health check endpoint
app.MapHealthChecks("/health");

// Detailed health check endpoint with JSON response - only in Development
if (app.Environment.IsDevelopment())
{
    app.MapHealthChecks("/health/detailed", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var response = new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    exception = x.Value.Exception?.Message,
                    duration = x.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            };
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
        }
    });
}

app.Run();
