# StrictId
***Strongly-typed, ergonomic, compatible, fun to use identifiers for your entities***  

*Get on [NuGet](https://www.nuget.org/packages/StrictId)*:  
[![StrictId](https://img.shields.io/nuget/dt/StrictId?style=flat-square&label=StrictId)](https://www.nuget.org/packages/StrictId)
[![EFCore](https://img.shields.io/nuget/dt/StrictId.EFCore?style=flat-square&label=StrictId.EFCore)](https://www.nuget.org/packages/StrictId.EFCore)
[![HotChocolate](https://img.shields.io/nuget/dt/StrictId.HotChocolate?style=flat-square&label=StrictId.HotChocolate)](https://www.nuget.org/packages/StrictId.HotChocolate)

---

## What

```csharp
public class Person {
    public Id<Person> Id { get; init; } // Strongly typed ID, with Ulid as the underlying type
    public Id<Dog> BestFriendId { get; set; } // No confusion about what ID we are looking for here
    public List<Id> Friends { get; set; } // Non-strict/non-generic version also included
}
```

- Strongly-typed IDs for your entities, or anything else
- Ulid as the underlying value, which can easily be converted to and from Guid, string, or byte arrays
- Ergonomic, developer-friendly usage without ceremony, boilerplate, or annoyance
- Built-in JSON conversion support for System.Text.Json
- Full support for Entity Framework Core incl. value converters and value generators, with StrictId.EFCore
- Full support for HotChocolate GraphQL incl. custom scalars for `Id<T>` and `Id`, with StrictId.HotChocolate
- Tiny memory footprint and highly efficient

## How

*Recommended, but optional*  
In your global usings file, add the following to save yourself a few keystrokes:
```csharp
global using StrictId;
```

### Create
```csharp
Id<Person>.NewId(); // Generate a new random ID
new Id<Person>("01HV9AF3QA4T121HCZ873M0BKK"); // Create from ULID string
new Id<Person>("018ED2A7-8EEA-2682-20C5-9F41C7402E73"); // Create from GUID string
new Id<Person>(Ulid.NewUlid()); // Create from ULID
new Id<Person>(Guid.NewGuid()); // Create from GUID
new Id<Person>(Id.NewId()); // Create from non-typed ID

Id<Person> id = Ulid.NewUlid(); // Convert implicitly from Ulid
Id<Person> id = Guid.NewGuid(); // Convert implicitly from Guid
var id = (Id<Person>)"01HV9AF3QA4T121HCZ873M0BKK"; // Cast from string
var id = (Id<Person>)Id.NewId(); // Cast from non-typed ID
```

### Convert

```csharp
var id = new Id<Person>("01HV9AF3QA4T121HCZ873M0BKK");

id.ToString(); // "01HV9AF3QA4T121HCZ873M0BKK"
id.ToUlid(); // Same as Ulid.Parse("01HV9AF3QA4T121HCZ873M0BKK");
id.ToGuid(); // Same as Guid.Parse("018ED2A7-8EEA-2682-20C5-9F41C7402E73");
id.ToByteArray(); // byte[]
id.IdValue // Id("018ED2A7-8EEA-2682-20C5-9F41C7402E73")
```

### With Entity Framework Core

**Install [StrictId.EFCore](https://www.nuget.org/packages/StrictId.EFCore) via NuGet**


In your DbContext:
```csharp
using StrictId.EFCore;

public class MyDatabase (DbContextOptions<MyDatabase> options) : DbContext(options)
{
    protected override void ConfigureConventions (ModelConfigurationBuilder builder)
    {
        // ...
        
        builder.ConfigureStrictId();
    }
}
```

To generate values:
```csharp
using StrictId.EFCore;

// ...

builder.Property(e => e.Id)
    .ValueGeneratedOnAdd()
    .HasStrictIdValueGenerator();
```

### With Hot Chocolate GraphQL

**Install [StrictId.HotChocolate](https://www.nuget.org/packages/StrictId.HotChocolate) via NuGet**

On the request executor builder, configure strict IDs:
```csharp
builder.Services.AddGraphQLServer()
    // ...
    .AddStrictId();
```

Scalars will be created for each strict ID, named `{Type}Id`. For example, `Id<Person>` would become `PersonId` in the GraphQL schema.

## Why

- Using Guid or Ulid as the type for IDs can easily lead to mixing up method arguments and assignments
- Other similar packages are cumbersome, non-compatible, and frankly annoying
- Ulid as the underlying type provides neat benefits over simple Guids, as they are ordered, making databases less fragmented, and look nicer as strings

## Acknowledgements

- [Ulid](https://github.com/Cysharp/Ulid) - Library for ULID in C#, used for much of the underlying functionality
- [StronglyTypedId](https://github.com/andrewlock/StronglyTypedId) - For doing this first, but in a much more convoluted, non-ergonomic, less compatible way

## License

MIT
