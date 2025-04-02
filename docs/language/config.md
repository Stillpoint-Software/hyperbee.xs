---
layout: default
title: Config
parent: Language
nav_order: 5
---

# Config

The `config` construct is used to retrieve a configuration value from the `Host` environment. 

This construct requires the `Hyperbee.XS.Extensions` package.

## Usage

```
config<string>("config_key");
```

> Note: `config` is only allowed when using the `Compile( serviceProvider )` that takes an IServicesProvider as an argument.
> ```csharp
> var host = Host
>     .CreateDefaultBuilder()
>     .ConfigureAppConfiguration( ( _, config ) =>
>     {
>         config.AddInMemoryCollection( new Dictionary<string, string>
>         {
>             {"config_key", "value"}
>         } );
>     } )
>     .Build();
> 
> host.Services;  // IServiceProvider
> ```