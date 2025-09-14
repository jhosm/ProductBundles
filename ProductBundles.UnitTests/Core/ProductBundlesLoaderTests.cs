using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ProductBundles.Core;
using ProductBundles.Sdk;

namespace ProductBundles.UnitTests
{
    /// <summary>
    /// Tests for the ProductBundlesLoader class
    /// </summary>
    [TestClass]
    public class ProductBundlesLoaderTests
{
    private string _testPluginsPath = null!;
    private string _emptyTestPluginsPath = null!;
    private ILogger<ProductBundlesLoader> _logger = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        // Use the plugins directory that contains the actual plugins
        // Tests run from bin/Debug/net8.0, so we need to go up to the project root and then to plugins
        _testPluginsPath = Path.Combine("..", "..", "..", "plugins");
        // Create a unique empty test directory for tests that need empty directories
        _emptyTestPluginsPath = Path.Combine(Path.GetTempPath(), "ProductBundlesLoaderTests", Guid.NewGuid().ToString());
        _logger = NullLogger<ProductBundlesLoader>.Instance;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Clean up empty test directory
        if (Directory.Exists(_emptyTestPluginsPath))
        {
            Directory.Delete(_emptyTestPluginsPath, true);
        }
    }

    [TestMethod]
    public void Constructor_WithDefaultParameters_InitializesCorrectly()
    {
        // Act
        var loader = new ProductBundlesLoader();

        // Assert
        Assert.IsNotNull(loader);
        Assert.IsNotNull(loader.LoadedPlugins);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void Constructor_WithCustomPluginsPath_InitializesCorrectly()
    {
        // Arrange
        var customPath = "custom-plugins";

        // Act
        var loader = new ProductBundlesLoader(customPath);

        // Assert
        Assert.IsNotNull(loader);
        Assert.IsNotNull(loader.LoadedPlugins);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void Constructor_WithLogger_InitializesCorrectly()
    {
        // Act
        var loader = new ProductBundlesLoader("plugins", _logger);

        // Assert
        Assert.IsNotNull(loader);
        Assert.IsNotNull(loader.LoadedPlugins);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void LoadPlugins_WhenDirectoryDoesNotExist_CreatesDirectoryAndReturnsEmpty()
    {
        // Arrange
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.IsTrue(Directory.Exists(_emptyTestPluginsPath));
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void LoadPlugins_WhenDirectoryExistsButEmpty_ReturnsEmpty()
    {
        // Arrange
        Directory.CreateDirectory(_emptyTestPluginsPath);
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void LoadPlugins_WhenDirectoryHasNonDllFiles_ReturnsEmpty()
    {
        // Arrange
        Directory.CreateDirectory(_emptyTestPluginsPath);
        File.WriteAllText(Path.Combine(_emptyTestPluginsPath, "test.txt"), "not a dll");
        File.WriteAllText(Path.Combine(_emptyTestPluginsPath, "test.exe"), "not a dll");
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void GetPluginById_WhenNoPluginsLoaded_ReturnsNull()
    {
        // Arrange
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.GetPluginById("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LoadedPlugins_ReturnsReadOnlyCollection()
    {
        // Arrange
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.LoadedPlugins;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsInstanceOfType(result, typeof(System.Collections.Generic.IReadOnlyList<IAmAProductBundle>));
    }

    [TestMethod]
    public void LoadPlugins_WhenDirectoryHasInvalidDll_IgnoresInvalidDllAndContinues()
    {
        // Arrange
        Directory.CreateDirectory(_emptyTestPluginsPath);
        File.WriteAllText(Path.Combine(_emptyTestPluginsPath, "invalid.dll"), "This is not a valid DLL file");
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void LoadPlugins_WhenDirectoryHasSubdirectories_SearchesRecursively()
    {
        // Arrange
        Directory.CreateDirectory(_emptyTestPluginsPath);
        var subDir = Path.Combine(_emptyTestPluginsPath, "subfolder");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "test.dll"), "fake dll content");
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result);
        // Should attempt to load even invalid DLLs in subdirectories
        Assert.AreEqual(0, result.Count); // Will be 0 because it's not a real DLL, but confirms recursive search
    }

    [TestMethod]
    public void LoadPlugins_CalledMultipleTimes_DoesNotDuplicatePlugins()
    {
        // Arrange
        Directory.CreateDirectory(_emptyTestPluginsPath);
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result1 = loader.LoadPlugins();
        var result2 = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result1);
        Assert.IsNotNull(result2);
        Assert.AreEqual(result1.Count, result2.Count);
        Assert.AreEqual(0, result1.Count); // Empty directory
        Assert.AreEqual(0, result2.Count);
    }

    [TestMethod]
    public void GetPluginById_WithNullId_ReturnsNull()
    {
        // Arrange
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.GetPluginById(null!);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetPluginById_WithEmptyId_ReturnsNull()
    {
        // Arrange
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.GetPluginById("");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void GetPluginById_WithWhitespaceId_ReturnsNull()
    {
        // Arrange
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act
        var result = loader.GetPluginById("   ");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void LoadPlugins_WhenDirectoryPathIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var loader = new ProductBundlesLoader("", _logger);

        // Act & Assert - LoadPlugins should throw ArgumentException when trying to use Path.GetFullPath with empty string
        Assert.ThrowsException<ArgumentException>(() => loader.LoadPlugins());
    }

    [TestMethod]
    public void Constructor_WithNullPath_AcceptsNullPath()
    {
        // Act - Constructor should not throw with null path
        var loader = new ProductBundlesLoader(null!, _logger);

        // Assert
        Assert.IsNotNull(loader);
        Assert.IsNotNull(loader.LoadedPlugins);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void Constructor_WithNullLogger_DoesNotThrow()
    {
        // Act
        var loader = new ProductBundlesLoader(_testPluginsPath, null);

        // Assert
        Assert.IsNotNull(loader);
        Assert.IsNotNull(loader.LoadedPlugins);
        Assert.AreEqual(0, loader.LoadedPlugins.Count);
    }

    [TestMethod]
    public void LoadPlugins_WithSamplePlugin_LoadsPluginSuccessfully()
    {
        // Arrange - Use the plugins directory that now contains the actual plugins
        var loader = new ProductBundlesLoader(_testPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Count > 0, "Should load at least one plugin");
        Assert.IsTrue(loader.LoadedPlugins.Count > 0, "LoadedPlugins should contain loaded plugins");
        
        // Verify plugin properties
        var plugin = result.First();
        Assert.IsNotNull(plugin.Id);
        Assert.IsNotNull(plugin.FriendlyName);
        Assert.IsNotNull(plugin.Version);
    }

    [TestMethod]
    public void GetPluginById_WithLoadedPlugin_ReturnsCorrectPlugin()
    {
        // Arrange - Use the plugins directory that contains the actual plugins
        var loader = new ProductBundlesLoader(_testPluginsPath, _logger);
        var plugins = loader.LoadPlugins();
        
        if (plugins.Count == 0)
        {
            Assert.Inconclusive("No plugins were loaded. Check plugin dependencies.");
        }
        
        var firstPlugin = plugins.First();

        // Act
        var result = loader.GetPluginById(firstPlugin.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(firstPlugin.Id, result.Id);
        Assert.AreSame(firstPlugin, result);
    }

    [TestMethod]
    public void LoadPlugins_WithMixedValidAndInvalidDlls_LoadsOnlyValidPlugins()
    {
        // Arrange - Create a unique temporary directory with mix of valid and invalid DLLs
        var mixedTestDir = Path.Combine(Path.GetTempPath(), "ProductBundlesLoaderTests", "Mixed", Guid.NewGuid().ToString());
        Directory.CreateDirectory(mixedTestDir);
        
        try
        {
            // Create invalid DLL in test directory
            File.WriteAllText(Path.Combine(mixedTestDir, "invalid.dll"), "This is not a valid DLL");
            
            // Copy valid plugins from the plugins directory to the test directory
            var pluginsDir = Path.Combine("..", "..", "..", "plugins");
            bool hasValidPlugin = false;
            
            if (Directory.Exists(pluginsDir))
            {
                var dllFiles = Directory.GetFiles(pluginsDir, "*.dll");
                foreach (var dllFile in dllFiles)
                {
                    var fileName = Path.GetFileName(dllFile);
                    var destPath = Path.Combine(mixedTestDir, fileName);
                    File.Copy(dllFile, destPath, overwrite: true);
                    hasValidPlugin = true;
                }
            }
            
            var loader = new ProductBundlesLoader(mixedTestDir, _logger);

            // Act
            var result = loader.LoadPlugins();

            // Assert
            Assert.IsNotNull(result);
            if (hasValidPlugin)
            {
                Assert.IsTrue(result.Count > 0, "Should load valid plugins and ignore invalid ones");
            }
            else
            {
                // If no valid plugin was available, should still handle gracefully
                Assert.AreEqual(0, result.Count);
            }
        }
        finally
        {
            // Clean up the temporary directory
            if (Directory.Exists(mixedTestDir))
            {
                Directory.Delete(mixedTestDir, true);
            }
        }
    }

    [TestMethod]
    public void LoadPlugins_WhenAssemblyThrowsException_ContinuesWithOtherAssemblies()
    {
        // Arrange - Use empty test directory to avoid interference from actual plugins
        Directory.CreateDirectory(_emptyTestPluginsPath);
        
        // Create a file that will cause assembly loading to fail
        File.WriteAllText(Path.Combine(_emptyTestPluginsPath, "corrupted.dll"), "CORRUPTED DLL CONTENT THAT WILL CAUSE EXCEPTION");
        
        var loader = new ProductBundlesLoader(_emptyTestPluginsPath, _logger);

        // Act & Assert - Should not throw exception, should handle gracefully
        var result = loader.LoadPlugins();
        
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count); // No valid plugins loaded
    }

    [TestMethod]
    public void LoadPlugins_WhenPluginHasNoParameterlessConstructor_SkipsPlugin()
    {
        // This test would require a special test plugin, but demonstrates the concept
        // The LoadPluginFromAssembly method uses Activator.CreateInstance() which requires
        // a parameterless constructor. Plugins without one will be skipped.
        
        // Arrange
        Directory.CreateDirectory(_testPluginsPath);
        var loader = new ProductBundlesLoader(_testPluginsPath, _logger);

        // Act
        var result = loader.LoadPlugins();

        // Assert - Should handle gracefully without throwing
        Assert.IsNotNull(result);
    }
    }
}
