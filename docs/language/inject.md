---
layout: default
title: Inject
parent: Language
nav_order: 9
---

# Inject (dependency injection)

The `inject` construct is used to retrieve a service from the service provider by type with an optional key.

This construct requires the `Hyperbee.XS.Extensions` package.


## Usage

With a type:

```
var service = inject<IService>();
```

Or with a key:

```
var service = inject<IService>("key");
```

> Note: `inject` is only allowed when using the `Compile( serviceProvider )` that takes an IServicesProvider as an argument.
> ```csharp
> var host = Host
>     .CreateDefaultBuilder()
>     .ConfigureServices( ( _, services ) =>
>     {
>         services.AddKeyedSingleton<IService>( "key", ( _, _ ) => new Service( " And Universe!" ) );
>     } )
>     .Build();
> 
> host.Services;  // IServiceProvider
> ```