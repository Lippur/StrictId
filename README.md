# ![StrictId](assets/StrictId-Light.svg)

***Strongly-typed, ergonomic, fun to use identifiers for your entities without any magic***


[![StrictId](https://img.shields.io/nuget/v/StrictId?label=StrictId)](https://www.nuget.org/packages/StrictId)
[![EFCore](https://img.shields.io/nuget/v/StrictId.EFCore?label=StrictId.EFCore)](https://www.nuget.org/packages/StrictId.EFCore)
[![AspNetCore](https://img.shields.io/nuget/v/StrictId.AspNetCore?label=StrictId.AspNetCore)](https://www.nuget.org/packages/StrictId.AspNetCore)

---

## What

```csharp
[IdPrefix("user")]
public class Person {
    public Id<Person> Id { get; init; } = Id<Person>.NewId(); // user_01knfv9xv03499c7bf2brngecz
    public Id<Dog> BestFriendId { get; set; }                 // Strongly-typed — mixing these up is a compiler error
    public IdNumber<Invoice> LatestInvoice { get; set; }      // bigint-backed, optionally prefixed
    public IdString<StripeCustomer> StripeId { get; set; }    // Opaque string, charset/length configurable
    public List<Id> History { get; set; } = [];               // Non-generic form also included
}
```

- **Three ID families:** `Id<T>` (Ulid/Guid-backed), `IdNumber<T>` (integer-backed), `IdString<T>` (opaque-string-backed) — all strongly-typed so `Id<Person>` and `Id<Dog>` can never be compared, assigned, or passed interchangeably
- **Prefixed string form** via `[IdPrefix("user")]` — canonical output like `user_01knfv...`, multiple aliases accepted on parse, separator character configurable
- **Built-in System.Text.Json** converters for every family
- **EF Core** value converters for every family (via [StrictId.EFCore](https://www.nuget.org/packages/StrictId.EFCore)) — one line to wire up your `DbContext`
- **ASP.NET Core** integration (via [StrictId.AspNetCore](https://www.nuget.org/packages/StrictId.AspNetCore)) — OpenAPI schemas with per-type patterns and examples, route constraints, `ProblemDetails` on parse failure
- **Ergonomic, no ceremony** — no partial records, no `StrictId.Init()`, no startup order trap. Annotate your entity types and go
- **Roslyn analyzers** (STRID001–006) catch cross-type `.Value` comparisons, malformed `[IdPrefix]` declarations, open-generic closings, and friends at compile time
- **AOT-friendly.** A Roslyn source generator ships inside the main package and populates a registry at module init so JSON and EF Core hot paths skip reflection
- [Ulid](https://github.com/ulid/spec) as the underlying value for `Id<T>`, round-trip convertible to `Guid`, `string`, or bytes
- Tiny footprint, single runtime dependency (`Ulid`)

## How

*Recommended, but optional*
In your global usings file, add the following to save yourself a few keystrokes:
```csharp
global using StrictId;
```

### Create
```csharp
Id<Person>.NewId();                                            // Generate a new ID
Id<Person>.Parse("user_01knfv9xv03499c7bf2brngecz");           // Canonical prefixed form
Id<Person>.Parse("01knfv9xv03499c7bf2brngecz");                // Bare ULID
Id<Person>.Parse("018ed2a7-8eea-2682-20c5-9f41c7402e73");      // GUID string
new Id<Person>(Ulid.NewUlid());                                // Wrap a ULID
new Id<Person>(Guid.NewGuid());                                // Wrap a GUID
new Id<Person>(Id.NewId());                                    // Wrap a non-typed Id

Id<Person> id = Ulid.NewUlid(); // Implicit from Ulid
Id<Person> id = Guid.NewGuid(); // Implicit from Guid
Id<Person> id = Id.NewId();     // Implicit from non-typed Id

bool ok = Id<Person>.TryParse("user_01knfv9xv03499c7bf2brngecz", out var id);
```

The non-typed `Id` works the same way; it just has no prefix.  
**Breaking change from v2:** `new Id<T>("string")` no longer parses, use `Id<T>.Parse(...)` instead.

### Convert
```csharp
var id = Id<Person>.Parse("user_01knfv9xv03499c7bf2brngecz");

id.ToString();        // "user_01knfv9xv03499c7bf2brngecz"  (canonical, default)
id.ToString("B");     // "01knfv9xv03499c7bf2brngecz"        (bare ULID)
id.ToString("G");     // "user_018ed2a7-8eea-2682-20c5-..."  (canonical with GUID suffix)
id.ToString("U");     // "01KNFV9XV03499C7BF2BRNGECZ"        (v2 uppercase compat)

id.ToUlid();          // Same as Ulid.Parse("01knfv9xv0...")
id.ToGuid();          // Same as Guid.Parse("018ed2a7-8eea-...")
id.ToByteArray();     // byte[]
id.ToId();            // Non-generic Id
```

**Breaking change from v2:** default output is now lowercase. Use the `"U"` specifier to get v2's uppercase ULID form.

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

### Prefixes and separators

Decorate your entity types to pick a prefix and separator:

```csharp
[IdPrefix("user")]
public class Person;                                  // user_01knfv9xv03499c7bf2brngecz

[IdPrefix("order", IsDefault = true)]
[IdPrefix("ord")]
[IdPrefix("o")]
public class Order;                                   // canonical "order_...", also accepts "ord_..." and "o_..."

[IdPrefix("tenant")]
[IdSeparator(IdSeparator.Colon)]
public class Tenant;                                  // tenant:01knfv9xv03499c7bf2brngecz
```

Prefixes must match `^[a-z][a-z0-9_]{0,62}$`. Separators are a closed enum: `Underscore` (default), `Slash`, `Period`, `Colon`. Attributes inherit from base classes; derived types can override either attribute independently. Malformed prefixes fail the build via the STRID003 analyzer.

### IdNumber and IdString

```csharp
[IdPrefix("inv")]
public class Invoice;

[IdPrefix("cus")]
[IdString(MaxLength = 32, CharSet = IdStringCharSet.Alphanumeric)]
public class StripeCustomer;

IdNumber<Invoice>.Parse("inv_42");                    // prefixed decimal form
new IdNumber<Invoice>(42UL);                          // from any integer width

IdString<StripeCustomer>.Parse("cus_abcDEF123");      // validated against [IdString] rules
```

`IdNumber<T>` and `IdString<T>` have no `NewId()` — numeric IDs come from the database (or your own code), and string IDs are whatever the upstream system hands you.

### With Entity Framework Core

**Install [StrictId.EFCore](https://www.nuget.org/packages/StrictId.EFCore) via NuGet**

```csharp
using StrictId.EFCore;

public class MyDatabase (DbContextOptions<MyDatabase> options) : DbContext(options)
{
    protected override void ConfigureConventions (ModelConfigurationBuilder builder)
    {
        builder.ConfigureStrictId();
    }
}
```

`ConfigureStrictId()` covers all three families. `Id<T>` maps to a fixed-width `char(26)` holding the bare ULID, `IdNumber<T>` to `bigint`, `IdString<T>` to `varchar(MaxLength)` sized from the `[IdString]` attribute. Prefixes live in the type system, not in the column — they are reconstituted on read, never stored. An alternative `IdToGuidConverter<T>` is available if you prefer the native `uniqueidentifier` column type for `Id<T>`.

### With ASP.NET Core

**Install [StrictId.AspNetCore](https://www.nuget.org/packages/StrictId.AspNetCore) via NuGet**

Route and query binding work out of the box in .NET 7+ via `ISpanParsable<T>`:

```csharp
app.MapGet("/users/{id}", (Id<Person> id) => db.Users.Find(id));
```

For the polish on top — OpenAPI schemas, route constraints, `ProblemDetails` mapping — add one line:

```csharp
using StrictId.AspNetCore;

builder.Services.AddStrictId();
```

That gives you:
- **OpenAPI** — every StrictId parameter and property renders as a string schema with a per-type `pattern`, `example`, and `description`, instead of the underlying `Ulid`/`ulong`/`string` shape
- **Route constraints** `id`, `idnumber`, `idstring` — pre-filter URL segments before dispatch with `{id:id}` and friends
- **TypeConverters** for legacy model binding (XAML, configuration, `System.ComponentModel`)
- **ProblemDetails** — StrictId parse failures surface as RFC 7807 `400 Bad Request` with the parse diagnostic in `detail`

Cherry-pick with `AddStrictIdOpenApi()`, `AddStrictIdRouteConstraints()`, `AddStrictIdTypeConverters()`, or `AddStrictIdProblemDetails()` if you only want some of it.

## Why

- Using primitives such as Guid or Ulid as the type for IDs can easily lead to mixing up method arguments and assignments
- Using value objects makes your code easier to read and more DDD-friendly (see [primitive obsession](https://refactoring.guru/smells/primitive-obsession))
- Other similar packages are cumbersome, non-compatible, and full of magic™. StrictId, at its heart, is just a generic struct.
- Ulid as the underlying type for `Id<T>` provides [neat benefits](https://github.com/ulid/spec) over simple Guids — ordered, database-friendly, and they look nicer as strings

## Acknowledgements

- [Ulid](https://github.com/Cysharp/Ulid) - Library for ULID in C#, used for much of the underlying functionality
- [StronglyTypedId](https://github.com/andrewlock/StronglyTypedId) - For doing this first, but in a much more convoluted, non-ergonomic way

## License

MIT
