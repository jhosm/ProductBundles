using ProductBundles.Api.Models;
using ProductBundles.Core;
using ProductBundles.Sdk;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

app.Run();
