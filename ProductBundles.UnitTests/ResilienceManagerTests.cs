using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Resilience;
using ProductBundles.Sdk;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProductBundles.UnitTests;

[TestClass]
public class ResilienceManagerTests
{
    private ResilienceManager _resilienceManager = null!;
    private ILogger<ResilienceManager> _logger = null!;

    [TestInitialize]
    public void Setup()
    {
        _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ResilienceManager>.Instance;
        // Use a short timeout for tests
        _resilienceManager = new ResilienceManager(_logger, TimeSpan.FromSeconds(2));
    }

    [TestMethod]
    public async Task ExecuteHandleEventAsync_WithNormalPlugin_ReturnsResult()
    {
        // Arrange
        var plugin = new ResilienceMockProductBundle();
        var eventName = "test.event";
        var instance = new ProductBundleInstance("test-id", "test-plugin", "1.0.0");

        // Act
        var result = await _resilienceManager.ExecuteHandleEventAsync(plugin, eventName, instance);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test-id", result.Id);
        Assert.AreEqual(1, plugin.HandleEventCallCount);
    }

    [TestMethod]
    public async Task ExecuteHandleEventAsync_WithHangingPlugin_ReturnsNull()
    {
        // Arrange
        var plugin = new ResilienceMockProductBundle { ShouldHang = true };
        var eventName = "test.event";
        var instance = new ProductBundleInstance("test-id", "test-plugin", "1.0.0");

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _resilienceManager.ExecuteHandleEventAsync(plugin, eventName, instance);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, plugin.HandleEventCallCount);
        Assert.IsTrue(elapsed.TotalSeconds >= 2); // Should timeout after 2 seconds
        Assert.IsTrue(elapsed.TotalSeconds < 4); // But not take too much longer
    }

    [TestMethod]
    public async Task ExecuteHandleEventAsync_WithFailingPlugin_ReturnsNull()
    {
        // Arrange
        var plugin = new ResilienceMockProductBundle { ShouldFail = true };
        var eventName = "test.event";
        var instance = new ProductBundleInstance("test-id", "test-plugin", "1.0.0");

        // Act
        var result = await _resilienceManager.ExecuteHandleEventAsync(plugin, eventName, instance);

        // Assert
        Assert.IsNull(result);
        Assert.AreEqual(1, plugin.HandleEventCallCount);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task ExecuteHandleEventAsync_WithNullPlugin_ThrowsException()
    {
        // Arrange
        IAmAProductBundle plugin = null!;
        var eventName = "test.event";
        var instance = new ProductBundleInstance("test-id", "test-plugin", "1.0.0");

        // Act
        await _resilienceManager.ExecuteHandleEventAsync(plugin, eventName, instance);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public async Task ExecuteHandleEventAsync_WithEmptyEventName_ThrowsException()
    {
        // Arrange
        var plugin = new ResilienceMockProductBundle();
        var eventName = "";
        var instance = new ProductBundleInstance("test-id", "test-plugin", "1.0.0");

        // Act
        await _resilienceManager.ExecuteHandleEventAsync(plugin, eventName, instance);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public async Task ExecuteHandleEventAsync_WithNullInstance_ThrowsException()
    {
        // Arrange
        var plugin = new ResilienceMockProductBundle();
        var eventName = "test.event";
        ProductBundleInstance instance = null!;

        // Act
        await _resilienceManager.ExecuteHandleEventAsync(plugin, eventName, instance);
    }
}

/// <summary>
/// Mock ProductBundle for testing resilience scenarios
/// </summary>
public class ResilienceMockProductBundle : IAmAProductBundle
{
    public string Id => "mock-plugin";
    public string FriendlyName => "Mock Plugin";
    public string Description => "A mock plugin for testing";
    public string Version => "1.0.0";
    public IReadOnlyList<Property> Properties => new List<Property>();
    public IReadOnlyList<RecurringBackgroundJob> RecurringBackgroundJobs => new List<RecurringBackgroundJob>();

    public bool ShouldFail { get; set; } = false;
    public bool ShouldHang { get; set; } = false;
    public int HandleEventCallCount { get; private set; } = 0;

    public void Initialize()
    {
        // Mock implementation - do nothing
    }

    public void Dispose()
    {
        // Mock implementation - do nothing
    }

    public ProductBundleInstance HandleEvent(string eventName, ProductBundleInstance bundleInstance)
    {
        HandleEventCallCount++;

        if (ShouldFail)
        {
            throw new InvalidOperationException("Mock plugin failure");
        }

        if (ShouldHang)
        {
            Thread.Sleep(10000); // Hang for 10 seconds
        }

        // Return a copy of the input instance with some modifications
        var result = new ProductBundleInstance(bundleInstance.Id, bundleInstance.ProductBundleId, bundleInstance.ProductBundleVersion);
        foreach (var prop in bundleInstance.Properties)
        {
            result.Properties[prop.Key] = prop.Value;
        }
        result.Properties["_processed"] = true;
        result.Properties["_processedAt"] = DateTime.UtcNow;

        return result;
    }

    public ProductBundleInstance UpgradeProductBundleInstance(ProductBundleInstance bundleInstance)
    {
        // Return upgraded instance
        var upgraded = new ProductBundleInstance(bundleInstance.Id, Id, Version);
        foreach (var prop in bundleInstance.Properties)
        {
            upgraded.Properties[prop.Key] = prop.Value;
        }
        upgraded.Properties["_upgraded"] = true;
        upgraded.Properties["_upgradeTimestamp"] = DateTime.UtcNow;

        return upgraded;
    }
}
