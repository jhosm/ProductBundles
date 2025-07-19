using ProductBundles.Api.Models;
using ProductBundles.Api.Services;
using ProductBundles.Core;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using Hangfire;
using Hangfire.MemoryStorage;

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

// Register ProductBundleInstance storage services
builder.Services.AddProductBundleInstanceServices(
    Path.Combine(Directory.GetCurrentDirectory(), "storage"),
    jsonOptions => {
        jsonOptions.WriteIndented = true;
        jsonOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseMemoryStorage()); // Use in-memory storage for development

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.Queues = new[] { "productbundles", "recurring", "default" };
});

// Register background service for managing ProductBundle recurring jobs
builder.Services.AddScoped<ProductBundleBackgroundService>();
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
        await recurringJobManager.InitializeRecurringJobsAsync();
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

// GET all ProductBundleInstances
app.MapGet("/ProductBundleInstances", async (IProductBundleInstanceStorage storage) =>
{
    var instances = await storage.GetAllAsync();
    var dtos = instances.Select(instance => new ProductBundleInstanceDto
    {
        Id = instance.Id,
        ProductBundleId = instance.ProductBundleId,
        ProductBundleVersion = instance.ProductBundleVersion,
        Properties = instance.Properties
    }).ToList();
    
    return Results.Ok(dtos);
})
.WithName("GetProductBundleInstances")
.WithOpenApi();

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

// GET ProductBundleInstances by ProductBundleId
app.MapGet("/ProductBundleInstances/ByProductBundle/{productBundleId}", async (string productBundleId, IProductBundleInstanceStorage storage) =>
{
    var instances = await storage.GetByProductBundleIdAsync(productBundleId);
    var dtos = instances.Select(instance => new ProductBundleInstanceDto
    {
        Id = instance.Id,
        ProductBundleId = instance.ProductBundleId,
        ProductBundleVersion = instance.ProductBundleVersion,
        Properties = instance.Properties
    }).ToList();
    
    return Results.Ok(dtos);
})
.WithName("GetProductBundleInstancesByProductBundleId")
.WithOpenApi();

// Hangfire Recurring Jobs Management Endpoints

// GET all registered recurring jobs
app.MapGet("/RecurringJobs", (HangfireRecurringJobManager manager) =>
{
    var jobs = manager.GetRegisteredJobsInfo();
    return Results.Ok(jobs);
})
.WithName("GetRecurringJobs")
.WithOpenApi();

// POST refresh recurring jobs for a specific ProductBundle
app.MapPost("/RecurringJobs/Refresh/{productBundleId}", async (string productBundleId, HangfireRecurringJobManager manager) =>
{
    try
    {
        await manager.RefreshPluginRecurringJobsAsync(productBundleId);
        return Results.Ok($"Successfully refreshed recurring jobs for ProductBundle '{productBundleId}'");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to refresh recurring jobs for ProductBundle '{productBundleId}': {ex.Message}");
    }
})
.WithName("RefreshRecurringJobs")
.WithOpenApi();

// POST refresh all recurring jobs from all ProductBundles
app.MapPost("/RecurringJobs/RefreshAll", async (HangfireRecurringJobManager manager) =>
{
    try
    {
        await manager.InitializeRecurringJobsAsync();
        return Results.Ok("Successfully refreshed all recurring jobs from ProductBundles");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to refresh recurring jobs: {ex.Message}");
    }
})
.WithName("RefreshAllRecurringJobs")
.WithOpenApi();

// DELETE recurring jobs for a specific ProductBundle
app.MapDelete("/RecurringJobs/{productBundleId}", async (string productBundleId, HangfireRecurringJobManager manager) =>
{
    try
    {
        await manager.RemovePluginRecurringJobsAsync(productBundleId);
        return Results.Ok($"Successfully removed recurring jobs for ProductBundle '{productBundleId}'");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to remove recurring jobs for ProductBundle '{productBundleId}': {ex.Message}");
    }
})
.WithName("RemoveRecurringJobs")
.WithOpenApi();

// POST manually trigger a specific recurring job
app.MapPost("/RecurringJobs/Trigger/{productBundleId}/{jobName}", (string productBundleId, string jobName, ProductBundleBackgroundService backgroundService) =>
{
    try
    {
        var jobId = BackgroundJob.Enqueue<ProductBundleBackgroundService>(
            service => service.ExecuteRecurringJobAsync(
                productBundleId, 
                jobName, 
                new Dictionary<string, object?> { { "triggeredManually", true }, { "triggerTime", DateTime.UtcNow } }
            )
        );
        
        return Results.Ok(new { JobId = jobId, Message = $"Manually triggered job '{jobName}' for ProductBundle '{productBundleId}'" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to trigger job '{jobName}' for ProductBundle '{productBundleId}': {ex.Message}");
    }
})
.WithName("TriggerRecurringJob")
.WithOpenApi();

app.Run();
