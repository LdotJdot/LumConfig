# LumConfig

A lightweight, reflection-free JSON configuration manager for .NET that supports AOT (Ahead-of-Time) compilation. Easily manage your application's configuration with simple set/get operations and seamless serialization/deserialization.

## Quick Start

```csharp
// Create
LumConfigManager config = new LumConfigManager();

config.Set("findmax", "xx");
config.Set("HotKey", 46);
config.Set("Now", DateTime.Now);
config.Set("TheHotKeys", new int[] { 46, 33, 21 });
config.Set("HotKeys:Mainkey", 426); // Nested configuration
config.Save("d:\\aa.json");


// Read existed file
LumConfigManager loadedConfig = new LumConfigManager("d:\\aa.json");
Console.WriteLine(loadedConfig.GetInt("HotKeys:Mainkey"));
Console.WriteLine(loadedConfig.Get("Now"));
var hotkeys = loadedConfig.Get("TheHotKeys") as IList;
foreach (var key in hotkeys)
{
    Console.WriteLine(key);
}
```

The config file aa.json
```json
{"findmax":"xx","HotKey":46,"Now":"2025/9/11 10:25:50","TheHotKeys":[46,33,21],"HotKeys":{"Mainkey":426}}
```

## Features

- 🚀 **AOT-Compatible**: Designed to work with AOT compilation environments
- 🔍 **Reflection-Free**: No runtime reflection required for operation
- 📝 **Simple API**: Intuitive methods for setting and retrieving values
- 🎯 **Type Support**: Handles basic JSON types (int, long, double, string, bool) and arrays
- 🔧 **Flexible Structure**: Supports nested configurations through key path notation
- 💾 **Easy Persistence**: Simple save/load functionality for JSON files

## Installation

Add the library to your project via NuGet (LumConfig).

## API Reference

### Core Methods

- `Set(string key, object value)`: Stores a value with the specified key
- `Get(string key)`: Retrieves a value by key (returns object)
- `GetInt(string key)`: Retrieves an integer value by key
- `Save(string path)`: Saves configuration to a JSON file
- `LumConfigManager(string path)`: Constructor that loads configuration from a file

### Supported Value Types

- Basic JSON types: `int`, `long`, `double`, `string`, `bool`
- Arrays of supported types
- Complex objects are automatically stringified
- Nested configurations using colon notation (`Parent:Child`)

## Configuration File Format

The library generates clean JSON files with support for nested structures and arrays.

## Use Cases

- Application settings management
- Game configuration systems
- AOT-compatible environments (e.g., Unity IL2CPP)
- Systems where reflection is limited or prohibited
- Lightweight configuration needs without heavy dependencies

## Requirements

- .NET Standard 2.0+ or .NET Framework 4.6.1+
- No external dependencies

## License

MIT License - feel free to use in your projects!

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
