# StrictId
***Strongly-typed, ergonomic, compatible, fun to use identifiers for your entities***  

*Get on [NuGet](https://www.nuget.org/packages/StrictId)*:  
[![StrictId](https://img.shields.io/nuget/v/StrictId?label=StrictId)](https://www.nuget.org/packages/StrictId)
[![EFCore](https://img.shields.io/nuget/v/StrictId.EFCore?label=StrictId.EFCore)](https://www.nuget.org/packages/StrictId.EFCore)
[![HotChocolate](https://img.shields.io/nuget/v/StrictId.HotChocolate?label=StrictId.HotChocolate)](https://www.nuget.org/packages/StrictId.HotChocolate)

---

## What

```csharp
public class Person {
    public Id<Person> Id { get; init; } // Strongly typed ID, lexicographically sortable, and round-trip convertible to Guid, Ulid, and string
    public Id<Dog> BestFriendId { get; set; } // No confusion about what ID we are looking for here
    public List<Id> Friends { get; set; } // Non-strict/non-generic version also included
}
```

- **Strongly-typed IDs for your entities, or anything else**
- [Ulid](https://github.com/ulid/spec) as the underlying value, which can easily be converted to and from Guid, string, or byte arrays
- Ergonomic, developer-friendly usage without ceremony, boilerplate, or annoyance
- Cure to [primitive obsession](https://refactoring.guru/smells/primitive-obsession) by being a DDD-friendly value object
- Built-in JSON conversion support for System.Text.Json
- Plug-and-play support for Entity Framework Core incl. value converters and value generators, with [StrictId.EFCore](https://www.nuget.org/packages/StrictId.EFCore)
- Plug-and-play support for HotChocolate GraphQL incl. custom scalars for `Id<T>` and `Id`, with [StrictId.HotChocolate](https://www.nuget.org/packages/StrictId.HotChocolate)
- Easy to create your own integrations and converters thanks to lack of magic
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
Id<Person> id = Id.NewId(); // Convert implicitly from non-typed Id
var id = (Id<Person>)"01HV9AF3QA4T121HCZ873M0BKK"; // Cast from string

Id<Person> id = Id<Person>.Parse("018ED2A7-8EEA-2682-20C5-9F41C7402E73"); // Parse from Guid or Ulid
bool success = Id<Person>.TryParse("01HV9AF3QA4T121HCZ873M0BKK", out Id<Person> id); // Safely parse from Guid or Ulid
```

Usage of the non-typed `Id` is identical.

### Convert

```csharp
var id = new Id<Person>("01HV9AF3QA4T121HCZ873M0BKK");

id.ToString(); // "01HV9AF3QA4T121HCZ873M0BKK"
id.ToUlid(); // Same as Ulid.Parse("01HV9AF3QA4T121HCZ873M0BKK");
id.ToGuid(); // Same as Guid.Parse("018ED2A7-8EEA-2682-20C5-9F41C7402E73");
id.ToByteArray(); // byte[]
id.ToId() // Id("018ED2A7-8EEA-2682-20C5-9F41C7402E73")
```

### Benefit
StrictId will prevent you from accidentally doing bad things, and lets you do nice things instead:

```csharp
var personId = Id<Person>.NewId();
var dogId = Id<Dog>.NewId();

if (personId == dogId) Console.Write("Uh oh"); // Compiler error

public void Feed(Id<Dog> id) { 
    GetDog(id).FeedLeftovers();
}

Feed(personId); // Compiler error

// But:
public class Diet {
    public void Feed(Id<Dog> id) { 
        GetDog(id).FeedLeftovers();
    }
    
    public void Feed(Id<Person> id) { 
        GetPerson(id).FeedMichelinStarMeal();
    }
}

Feed(personId); // We eat well tonight. Better method overloads!
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

#### Notes

Id values are stored as fixed-length Ulid strings in the database (e.g. "01HV9AF3QA4T121HCZ873M0BKK"). An alternative value converter for storing them as Guid is also included (`StrictId.EFCore.ValueConverters.IdToGuidConverter`). Keep in mind that storing the IDs as Guid makes the database representation visually different from the normal string representation, which can be inconvenient.
If you would prefer to store IDs as byte arrays, you can create your own value generator and converter based on the ones included. Keep in mind, though, that the small improvement you gain in database performance and storage by using byte arrays is most likely not worth the loss of readability and clarity. 

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

- Using primitives such as Guid or Ulid as the type for IDs can easily lead to mixing up method arguments and assignments
- Using value objects makes your code easier to read and more DDD-friendly (see [primitive obsession](https://refactoring.guru/smells/primitive-obsession))
- Other similar packages are cumbersome, non-compatible, and full of magic™, while StrictId's Id is just a simple generic type, no source generation or other hocus-pocus needed
- Ulid as the underlying type provides [neat benefits](https://github.com/ulid/spec) over simple Guids, as they are ordered, making databases less fragmented, and look nicer as strings

## Acknowledgements

- [Ulid](https://github.com/Cysharp/Ulid) - Library for ULID in C#, used for much of the underlying functionality
- [StronglyTypedId](https://github.com/andrewlock/StronglyTypedId) - For doing this first, but in a much more convoluted, non-ergonomic way

## License

MIT
