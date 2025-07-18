#!/bin/bash

echo "Building plugins..."

# Create plugins directory if it doesn't exist
mkdir -p plugins

# Build the sample plugin
echo "Building ProductBundles.SamplePlugin..."
dotnet build ProductBundles.SamplePlugin/ProductBundles.SamplePlugin.csproj -o plugins/SamplePlugin

# Copy the plugin DLL to the plugins folder
cp plugins/SamplePlugin/ProductBundles.SamplePlugin.dll ProductBundles.PluginLoader/bin/Debug/net8.0/plugins/
cp plugins/SamplePlugin/ProductBundles.Core.dll ProductBundles.PluginLoader/bin/Debug/net8.0/plugins/
cp plugins/SamplePlugin/ProductBundles.Sdk.dll ProductBundles.PluginLoader/bin/Debug/net8.0/plugins/

# Build the main application
echo "Building ProductBundles.PluginLoader..."
dotnet build ProductBundles.PluginLoader/ProductBundles.PluginLoader.csproj

echo "Build completed!"
echo "Plugin DLLs are now in the 'plugins' folder."
