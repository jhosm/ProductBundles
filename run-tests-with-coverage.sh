#!/bin/bash

# Script to run unit tests with code coverage and generate reports
echo "🧪 Running unit tests with code coverage..."

# Clean previous test results
rm -rf ProductBundles.UnitTests/TestResults/
mkdir -p ProductBundles.UnitTests/TestResults/

# Run tests with code coverage using coverlet
echo "📊 Collecting code coverage data..."
dotnet test ProductBundles.UnitTests/ProductBundles.UnitTests.csproj \
    --collect:"XPlat Code Coverage" \
    --results-directory:./ProductBundles.UnitTests/TestResults/ \
    --logger:trx \
    --verbosity:normal

# Check if tests passed
if [ $? -eq 0 ]; then
    echo "✅ All tests passed!"
    
    # Generate HTML coverage report using ReportGenerator
    echo "📈 Generating HTML coverage report..."
    
    # Create local tool manifest if it doesn't exist
    if [ ! -f ".config/dotnet-tools.json" ]; then
        dotnet new tool-manifest --force >/dev/null 2>&1
    fi
    
    # Install ReportGenerator locally
    dotnet tool install dotnet-reportgenerator-globaltool --version 5.1.26 2>/dev/null || echo "ReportGenerator already installed locally"
    
    # Find the coverage file
    COVERAGE_FILE=$(find ./ProductBundles.UnitTests/TestResults/ -name "coverage.cobertura.xml" | head -1)
    
    if [ -f "$COVERAGE_FILE" ]; then
        dotnet tool run reportgenerator -- \
            "-reports:$COVERAGE_FILE" \
            "-targetdir:./ProductBundles.UnitTests/TestResults/CoverageReport" \
            "-reporttypes:Html" \
            "-title:ProductBundles Code Coverage Report"
            
        echo "📊 Coverage report generated at: ./ProductBundles.UnitTests/TestResults/CoverageReport/index.html"
        echo "🌐 Open the report in your browser to view detailed coverage information"
    else
        echo "⚠️  Coverage file not found. Using alternative method..."
        
        # Alternative: Run with coverlet.msbuild for detailed coverage
        dotnet test ProductBundles.UnitTests/ProductBundles.UnitTests.csproj \
            /p:CollectCoverage=true \
            /p:CoverletOutputFormat=opencover \
            /p:CoverletOutput='./TestResults/coverage.opencover.xml' \
            /p:Include='[ProductBundles.*]*' \
            /p:Exclude='[*.UnitTests]*'
            
        if [ -f "./ProductBundles.UnitTests/TestResults/coverage.opencover.xml" ]; then
            dotnet tool run reportgenerator -- \
                "-reports:./ProductBundles.UnitTests/TestResults/coverage.opencover.xml" \
                "-targetdir:./ProductBundles.UnitTests/TestResults/CoverageReport" \
                "-reporttypes:Html" \
                "-title:ProductBundles Code Coverage Report"
                
            echo "📊 Coverage report generated at: ./ProductBundles.UnitTests/TestResults/CoverageReport/index.html"
        fi
    fi
    
    # Display coverage summary if available
    if [ -f "./ProductBundles.UnitTests/TestResults/CoverageReport/index.html" ]; then
        echo ""
        echo "📋 Coverage Summary:"
        echo "==================="
        # Try to extract coverage summary from the generated report
        grep -A 10 "coverage-summary" ./ProductBundles.UnitTests/TestResults/CoverageReport/index.html 2>/dev/null || echo "View detailed report in browser"
        echo ""
        echo "🔗 Open coverage report:"
        echo "   file://$(pwd)/ProductBundles.UnitTests/TestResults/CoverageReport/index.html"
    fi
    
else
    echo "❌ Tests failed! Check the output above for details."
    exit 1
fi
