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
mkdir -p ProductBundles.UnitTests/plugins

# Copy the plugin DLL to ProductBundles.Api, and ProductBundles.UnitTests
echo "Copying plugins to ProductBundles.Api..."
cp plugins/SamplePlugin/ProductBundles.SamplePlugin.dll ProductBundles.Api/plugins/

echo "Copying plugins to ProductBundles.UnitTests..."
cp plugins/SamplePlugin/ProductBundles.SamplePlugin.dll ProductBundles.UnitTests/plugins/

echo "Build completed!"
echo "Plugin DLLs are now in the 'plugins' folder."
