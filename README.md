# Product Bundles Plugin System

Sistema de plugins para carregar DLLs via reflection e instanciar classes que implementam a interface `IAmAProductBundle`.

## Estrutura do Projeto

- **ProductBundles.Sdk**: Biblioteca SDK contendo:
  - `IAmAProductBundle`: Interface que define um plugin
  - `Property`: Classe para definir propriedades dos plugins
  
- **ProductBundles.Core**: Biblioteca core contendo:
  - `ProductBundlesLoader`: Classe responsável por carregar plugins via reflection
  
- **ProductBundles.PluginLoader**: Aplicação console que demonstra o uso do sistema de plugins

- **ProductBundles.SamplePlugin**: Plugin de exemplo contendo duas implementações de `IAmAProductBundle`

## Como Usar

### 1. Criar um Plugin

Para criar um plugin, você precisa:

1. Criar uma classe que implemente `IAmAProductBundle`
2. Implementar todas as propriedades e métodos necessários
3. Compilar como uma DLL

Exemplo:

```csharp
using ProductBundles.Sdk;

public class MeuPlugin : IAmAProductBundle
{
    public string Id => "meuplugin";
    public string FriendlyName => "Meu Plugin";
    public string Description => "Descrição do meu plugin";
    public string Version => "1.0.0";

    public void Initialize()
    {
        // Código de inicialização
    }

    public void Execute()
    {
        // Funcionalidade principal do plugin
    }

    public void Dispose()
    {
        // Limpeza de recursos
    }
}
```

### 2. Carregar Plugins

```csharp
// Criar instância do carregador de plugins
var pluginLoader = new ProductBundlesLoader("plugins");

// Carregar todos os plugins da pasta
var plugins = pluginLoader.LoadPlugins();

// Inicializar plugins
pluginLoader.InitializePlugins();

// Executar plugins
pluginLoader.ExecutePlugins();

// Limpar recursos
pluginLoader.DisposePlugins();
```

### 3. Buscar Plugins

```csharp
// Buscar por ID
var plugin = pluginLoader.GetPluginById("meuplugin");

// Buscar por nome
var plugins = pluginLoader.GetPluginsByName("Meu Plugin");
```

## Executando o Exemplo

### Compilar e executar:

```bash
# Executar o script de build
./build-plugins.sh

# Executar a aplicação
dotnet run --project ProductBundles.PluginLoader
```

### Ou manualmente:

```bash
# Build dos projetos
dotnet build ProductBundles.Sdk
dotnet build ProductBundles.Core
dotnet build ProductBundles.SamplePlugin
dotnet build ProductBundles.PluginLoader

# Criar pasta plugins e copiar DLLs
mkdir -p plugins
cp ProductBundles.SamplePlugin/bin/Debug/net8.0/ProductBundles.SamplePlugin.dll plugins/
cp ProductBundles.Sdk/bin/Debug/net8.0/ProductBundles.Sdk.dll plugins/
cp ProductBundles.Core/bin/Debug/net8.0/ProductBundles.Core.dll plugins/

# Executar
dotnet run --project ProductBundles.PluginLoader
```

## Funcionalidades do ProductBundlesLoader

- **Carregamento Automático**: Escaneia a pasta "plugins" em busca de DLLs
- **Reflection**: Usa reflection para encontrar classes que implementam `IAmAProductBundle`
- **Instanciação Automática**: Cria instâncias das classes de plugin automaticamente
- **Gerenciamento de Ciclo de Vida**: Suporta Initialize, Execute e Dispose
- **Busca de Plugins**: Permite encontrar plugins por ID ou nome
- **Tratamento de Erros**: Lida com erros de carregamento e execução graciosamente

## Requisitos

- .NET 8.0 ou superior
- Plugins devem referenciar `ProductBundles.Sdk`
- Aplicações que usam plugins devem referenciar `ProductBundles.Core`
- Plugins devem implementar `IAmAProductBundle`
- DLLs devem estar na pasta "plugins"

## Exemplo de Saída

```
=== Product Bundles Plugin Loader ===

Loading plugins from: /path/to/plugins
Found 1 DLL files
Loading assembly: ProductBundles.SamplePlugin.dll
Found 2 plugin types in ProductBundles.SamplePlugin.dll
Successfully instantiated plugin: SampleProductBundle
  - Id: sampleplug
  - Name: Sample Product Bundle
  - Version: 1.0.0
Successfully instantiated plugin: AnotherSamplePlugin
  - Id: anothersample
  - Name: Another Sample Plugin
  - Version: 2.1.0
Successfully loaded 2 plugins

=== Loaded Plugins ===
Plugin: Sample Product Bundle
  ID: sampleplug
  Description: A sample plugin demonstrating the IAmAProductBundle interface
  Version: 1.0.0

Plugin: Another Sample Plugin
  ID: anothersample
  Description: Another sample plugin to demonstrate multiple plugins in one DLL
  Version: 2.1.0

=== Initializing Plugins ===
Initializing plugins...
[Sample Product Bundle] Initializing...
Initialized plugin: Sample Product Bundle
[Another Sample Plugin] Starting initialization sequence...
[Another Sample Plugin] Checking system requirements...
[Another Sample Plugin] Initialization complete!
Initialized plugin: Another Sample Plugin

=== Executing Plugins ===
Executing plugins...
[Sample Product Bundle] Executing main functionality...
[Sample Product Bundle] This is where the plugin would do its work!
[Sample Product Bundle] Execution completed successfully!
Executed plugin: Sample Product Bundle
[Another Sample Plugin] Beginning execution phase...
[Another Sample Plugin] Processing data...
[Another Sample Plugin] Processing step 1/3...
[Another Sample Plugin] Processing step 2/3...
[Another Sample Plugin] Processing step 3/3...
[Another Sample Plugin] All tasks completed successfully!
Executed plugin: Another Sample Plugin
```
