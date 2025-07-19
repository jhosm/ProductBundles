#!/bin/bash

echo "Building plugins..."

# Create plugins directory if it doesn't exist
mkdir -p plugins

# Build the sample plugin
echo "Building ProductBundles.SamplePlugin..."
dotnet build ProductBundles.SamplePlugin/ProductBundles.SamplePlugin.csproj -o plugins/SamplePlugin

# Create necessary directories
echo "Creating plugin directories..."
mkdir -p ProductBundles.Api/plugins
mkdir -p ProductBundles.PluginLoader/bin/Debug/net8.0/plugins

# Copy the plugin DLL to both ProductBundles.Api and ProductBundles.PluginLoader
echo "Copying plugins to ProductBundles.Api..."
cp plugins/SamplePlugin/ProductBundles.SamplePlugin.dll ProductBundles.Api/plugins/

echo "Copying plugins to ProductBundles.PluginLoader..."
cp plugins/SamplePlugin/ProductBundles.SamplePlugin.dll ProductBundles.PluginLoader/bin/Debug/net8.0/plugins/

# Build the main application
echo "Building ProductBundles.PluginLoader..."
dotnet build ProductBundles.PluginLoader/ProductBundles.PluginLoader.csproj

echo "Build completed!"
echo "Plugin DLLs are now in the 'plugins' folder."
