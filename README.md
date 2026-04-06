# ![StrictId](assets/StrictId-Light.svg)

***Strongly-typed, ergonomic, fun to use identifiers for everything, without ceremony or magic***

[![StrictId](https://img.shields.io/nuget/v/StrictId?label=StrictId)](https://www.nuget.org/packages/StrictId)
[![EFCore](https://img.shields.io/nuget/v/StrictId.EFCore?label=StrictId.EFCore)](https://www.nuget.org/packages/StrictId.EFCore)
[![AspNetCore](https://img.shields.io/nuget/v/StrictId.AspNetCore?label=StrictId.AspNetCore)](https://www.nuget.org/packages/StrictId.AspNetCore)

---

**StrictId gives you strongly-typed generic structs for entity IDs.**  
Four families cover different backing types: `Id<T>` (ULID), `Guid<T>` (wraps `System.Guid`), `IdNumber<T>` (integer), `IdString<T>` (opaque string). Since `Id<User>` and `Id<Order>` are different closed generics, the compiler catches mix-ups at build time.

Comes with System.Text.Json converters, EF Core value converters/generators, ASP.NET Core integration (OpenAPI, route constraints, ProblemDetails), Roslyn analysers, and AOT support. Single runtime dependency: [Ulid](https://github.com/Cysharp/Ulid).

```csharp
[IdPrefix("user")]
public class User
{
    public Id<User> Id { get; init; } = Id<User>.NewId();         // user_01knfv9xv03499c7bf2brngecz
    public Guid<Tenant> TenantId { get; set; }                     // Guid<T>, strongly-typed Guid
    public IdNumber<Invoice> LatestInvoice { get; set; }           // inv_42, bigint-backed
    public IdString<StripeCustomer> StripeId { get; set; }         // cus_abcDEF123, opaque string
}
```

It's just generic structs and attributes. Decorate your entities with `[IdPrefix]` if you want prefixed string forms, and you're good to go.

## Why StrictId

- **Type safety without boilerplate.** Libraries like [StronglyTypedId](https://github.com/andrewlock/StronglyTypedId) and [Vogen](https://github.com/SteveDunn/Vogen) require declaring a partial type per entity and rely on source generation to create the ID struct for you. StrictId uses closed generics instead, so `Id<User>` works immediately.
- **Simple, obvious API.** Things just work when you need them to, with sane defaults and quality-of-life features. 
- **Ulid, but more.** If you already use the [Ulid](https://github.com/Cysharp/Ulid) library, `Id<T>` wraps it with type safety, prefixed string forms, JSON converters, and EF Core/ASP.NET integration on top.
- **Covers every backing type.** Whether you use Guid, Ulid, slugs, numbers, or all four at the same time, StrictId works seamlessly and consistently. 
- **Drop-in Guid replacement.** `Guid<T>` mirrors the `System.Guid` API, so switching is mostly find-and-replace. Existing database columns work without migration.
- **Robust and reliable.** Lack of magic and 400+ unit and integration tests make sure I don't cause you any sleepless nights

## Get started

```
dotnet add package StrictId
```

Optional but recommended, add a global using:

```csharp
global using StrictId;
```

Create, parse, format:

```csharp
var id = Id<User>.NewId();                                       // Generate a new ULID-backed ID
var parsed = Id<User>.Parse("user_01knfv9xv03499c7bf2brngecz");  // From canonical form
var bare = Id<User>.Parse("01knfv9xv03499c7bf2brngecz");         // Bare ULID also accepted

id.ToString();    // "user_01knfv9xv03499c7bf2brngecz"
id.ToString("B"); // "01knfv9xv03499c7bf2brngecz" (bare)

bool ok = Id<User>.TryParse(input, out var result);
bool valid = Id<User>.IsValid(input);                            // Like TryParse, without the out var
```

This pattern (`NewId()`, `Parse`, `TryParse`, `IsValid`, `ToString` with format specifiers) is the same across all four families.

## Features

- **Four ID families.** `Id<T>` for ULIDs, `Guid<T>` (wraps `System.Guid`), `IdNumber<T>` for integers, `IdString<T>` for opaque strings. Each also has a non-generic variant (`Id`, `IdNumber`, `IdString`) for type-erased scenarios.
- **Compile-time type safety.** `Id<User>` and `Id<Order>` are different closed generic types, so cross-type comparisons, assignments, and method calls are compiler errors.
- **Prefixed string forms.** `[IdPrefix("user")]` on your entity type gives you `user_01knfv...` output. Supports multiple aliases for backwards-compatible parsing, and the separator character is configurable per type or per assembly.
- **System.Text.Json** converters for every family, including dictionary key support. Included in the core package.
- **EF Core** value converters and conventions via [StrictId.EFCore](https://www.nuget.org/packages/StrictId.EFCore). Prefixes live in the type system and are never stored in the database.
- **ASP.NET Core** integration via [StrictId.AspNetCore](https://www.nuget.org/packages/StrictId.AspNetCore), including OpenAPI schemas with per-type patterns and examples, route constraints, and `ProblemDetails` on parse failure.
- **Roslyn analysers** (STRID001–006) catch cross-type `.Value` comparisons, malformed `[IdPrefix]` declarations, wrong-family attribute mismatches, and more at compile time.
- **AOT-friendly.** A bundled source generator populates a registry at module init so JSON and EF Core hot paths skip reflection.
- Single runtime dependency: [Ulid](https://github.com/Cysharp/Ulid).

## Usage

### Id\<T\>, ULID-backed

The default choice. Backed by a [ULID](https://github.com/ulid/spec): 128 bits, time-sortable, lexicographically orderable, and friendlier than GUIDs in URLs and logs.

```csharp
// Create
var id = Id<User>.NewId();                                        // New ULID
var id = Id<User>.NewId(DateTimeOffset.UtcNow);                   // With specific timestamp
var id = new Id<User>(someUlid);                                  // Wrap an existing Ulid
var id = new Id<User>(someGuid);                                  // Wrap a Guid (converted to ULID)

// Implicit conversions into Id<T>
Id<User> id = Ulid.NewUlid();
Id<User> id = Guid.NewGuid();
Id<User> id = Id.NewId();                                         // From non-generic Id

// Parse: accepts prefixed, bare ULID, or GUID string
var id = Id<User>.Parse("user_01knfv9xv03499c7bf2brngecz");
var id = Id<User>.Parse("01knfv9xv03499c7bf2brngecz");
var id = Id<User>.Parse("018ed2a7-8eea-2682-20c5-9f41c7402e73");

// Extract
id.ToUlid();       // Ulid
id.ToGuid();       // System.Guid
id.ToByteArray();  // byte[16]
id.ToBase64();     // Base64 string
id.ToId();         // Non-generic Id (erases the type parameter)
```

**Format specifiers:**

| Specifier     | Output                     | Example                            |
|---------------|----------------------------|------------------------------------|
| `C` (default) | Canonical, prefix + ULID   | `user_01knfv9xv03499c7bf2brngecz`  |
| `B`           | Bare ULID                  | `01knfv9xv03499c7bf2brngecz`       |
| `G`           | Prefix + GUID              | `user_018ed2a7-8eea-2682-20c5-...` |
| `BG`          | Bare GUID                  | `018ed2a7-8eea-2682-20c5-...`      |
| `U`           | Uppercase ULID (v2 compat) | `01KNFV9XV03499C7BF2BRNGECZ`       |

Without a prefix, `C` and `B` produce the same output.

The non-generic `Id` works identically but without prefix support, useful for type-erased scenarios, logging, and generic transport layers.

### Guid\<T\>, drop-in Guid replacement

Wraps `System.Guid` with a type parameter. The API mirrors `System.Guid` (same methods, same format specifiers), so switching from `Guid` to `Guid<T>` is mostly find-and-replace.

```csharp
// Create: same methods you already know
var id = Guid<User>.NewGuid();                                    // V4 (random), same as Guid.NewGuid()
var id = Guid<User>.NewId();                                      // V7 (time-sortable)
var id = Guid<User>.CreateVersion7();                              // Same as NewId(), mirrors Guid.CreateVersion7()
var id = Guid<User>.CreateVersion7(DateTimeOffset.UtcNow);         // V7 with specific timestamp

// Implicit from Guid
Guid<User> id = Guid.NewGuid();

// Explicit back to Guid
Guid raw = (Guid)id;

// Parse: standard GUID formats, optionally prefixed
var id = Guid<User>.Parse("user_550e8400-e29b-41d4-a716-446655440000");
var id = Guid<User>.Parse("550e8400-e29b-41d4-a716-446655440000");

// Extract
id.ToGuid();       // System.Guid
id.ToByteArray();  // byte[16]
```

**Format specifiers**, the standard Guid set plus `C` for the canonical prefixed form:

| Specifier     | Output                                   | Prefix |
|---------------|------------------------------------------|:------:|
| `C` (default) | Prefix + D-format                        |  Yes   |
| `D`           | `550e8400-e29b-41d4-a716-446655440000`   |   No   |
| `N`           | `550e8400e29b41d4a716446655440000`       |   No   |
| `B`           | `{550e8400-e29b-41d4-a716-446655440000}` |   No   |
| `P`           | `(550e8400-e29b-41d4-a716-446655440000)` |   No   |
| `X`           | Hex GUID                                 |   No   |

Without a prefix, `C` and `D` produce the same output, identical to `Guid.ToString()`.

EF Core maps `Guid<T>` to native `uniqueidentifier`/`uuid`, so existing Guid columns work as-is.

### IdNumber\<T\>, integers

For database-assigned auto-increment IDs, counters, or any integer-keyed entity.

```csharp
[IdPrefix("inv")]
public class Invoice;

var id = new IdNumber<Invoice>(42);                                // From any integer type
var id = IdNumber<Invoice>.Parse("inv_42");                        // Prefixed
var id = IdNumber<Invoice>.Parse("42");                            // Bare

id.ToUInt64();   // 42UL
id.ToInt64();    // 42L (throws OverflowException if > long.MaxValue)
id.ToString();   // "inv_42"
id.ToString("B"); // "42"

// Implicit from any integer type
IdNumber<Invoice> id = 42;
```

`IdNumber<T>` has no `NewId()`, numeric IDs come from the database or your own code.

### IdString\<T\>, opaque strings

For third-party IDs (Stripe `cus_...`, Twilio `SM...`), slugs, SKUs, and legacy string keys, anything where the ID is an externally-defined string.

```csharp
[IdPrefix("cus")]
[IdString(MaxLength = 32, CharSet = IdStringCharSet.Alphanumeric)]
public class StripeCustomer;

var id = IdString<StripeCustomer>.Parse("cus_AL8x9Kq4YZ");
id.ToString();       // "cus_AL8x9Kq4YZ"
id.ToString("B");    // "AL8x9Kq4YZ"
id.ToBareString();   // "AL8x9Kq4YZ"

// Implicit from string
IdString<StripeCustomer> id = "AL8x9Kq4YZ";
```

Validation rules are declared per type with `[IdString]`:

| Property     | Default                      | Options                                                                                           |
|--------------|------------------------------|---------------------------------------------------------------------------------------------------|
| `MaxLength`  | 255                          | Any positive integer                                                                              |
| `CharSet`    | `AlphanumericDashUnderscore` | `Any`, `Alphanumeric`, `AlphanumericDash`, `AlphanumericUnderscore`, `AlphanumericDashUnderscore` |
| `IgnoreCase` | `false`                      | `true` for case-insensitive comparison and storage                                                |

`IdString<T>` has no `NewId()`, string IDs come from wherever you decide.

### Prefixes and separators

Decorate entity types to give their IDs a human-readable prefix:

```csharp
[IdPrefix("user")]
public class User;                                    // user_01knfv9xv03499c7bf2brngecz

[IdPrefix("order", IsDefault = true)]
[IdPrefix("ord")]
[IdPrefix("o")]
public class Order;                                   // Outputs "order_...", also parses "ord_..." and "o_..."

[IdPrefix("tenant")]
[IdSeparator(IdSeparator.Colon)]
public class Tenant;                                  // tenant:01knfv9xv03499c7bf2brngecz
```

- Prefixes must match `^[a-z][a-z0-9_]{0,62}$`
- With multiple prefixes, exactly one must be `IsDefault = true`. That one is used on output, all are accepted on parse.
- Attributes inherit from base classes; derived types can override independently
- Malformed prefixes fail the build via STRID003

Available separators: `Underscore` (default), `Slash`, `Period`, `Colon`. Set per-type with `[IdSeparator]`, or set a project-wide default at assembly level:

```csharp
[assembly: IdSeparator(IdSeparator.Colon)]  // All types in this assembly default to ":"
```

Parsing is tolerant: any of the four separator characters is accepted on input, regardless of which one is declared.

### Type safety

Since each family is a closed generic, the compiler prevents you from mixing up IDs:

```csharp
var personId = Id<Person>.NewId();
var dogId = Id<Dog>.NewId();

if (personId == dogId) { }  // Compiler error

Feed(personId);             // Compiler error, persons aren't dogs

// But method overloads work beautifully:
void Feed(Id<Dog> id) => GetDog(id).FeedLeftovers();
void Feed(Id<Person> id) => GetPerson(id).FeedMichelinStarMeal();

Feed(personId);             // We eat well tonight
```

The STRID001 analyser also catches the `.Value` escape hatch. Comparing `personId.Value == dogId.Value` triggers a warning.

### Shared interface

All four families implement `IStrictId<TSelf>`, so you can write generic code that works with any StrictId:

```csharp
public T ParseOrDefault<T>(string? input) where T : struct, IStrictId<T>
    => T.TryParse(input, null, out var id) ? id : T.Empty;
```

The interface includes `Empty`, `HasValue`, `IsValid(string?)`, `Parse`, `TryParse`, `CompareTo`, `ToString` with format specifiers, and span-based formatting. `Parse` throws `FormatException` (matching `IParsable<T>`) with detailed error messages that include the offending input, the expected shape, and registered prefixes.

## EF Core

```
dotnet add package StrictId.EFCore
```

Add `ConfigureStrictId()` to your `DbContext`:

```csharp
using StrictId.EFCore;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.ConfigureStrictId();
    }
}
```

This registers value converters for all four families:

| Family        | Column type                        | Notes                                              |
|---------------|------------------------------------|----------------------------------------------------|
| `Id<T>`       | `char(26)`                         | Bare ULID, fixed-width, ASCII                      |
| `Guid<T>`     | Native `uniqueidentifier` / `uuid` | Works with existing Guid columns                   |
| `IdNumber<T>` | `bigint`                           | Signed 64-bit                                      |
| `IdString<T>` | `varchar(MaxLength)`               | Sized from `[IdString]` attribute, defaults to 255 |

Prefixes are never stored. They exist only in C# and are reconstituted on read. So you can use your existing database columns without any migrations.

### Value generators

Generate IDs automatically when entities are added:

```csharp
modelBuilder.Entity<User>()
    .Property(e => e.Id)
    .HasStrictIdValueGenerator<User>();   // Id<User>.NewId() on add

modelBuilder.Entity<Tenant>()
    .Property(e => e.Id)
    .HasGuidValueGenerator<Tenant>();     // Guid<Tenant>.NewId() (V7) on add
```

### Alternative column type for Id\<T\>

If you prefer a native GUID column for `Id<T>`, use `IdToGuidConverter<T>`, which reorders ULID bytes for SQL Server sort compatibility.

## ASP.NET Core

```
dotnet add package StrictId.AspNetCore
```

Route and query binding work via `ISpanParsable<T>` (.NET 7+):

```csharp
app.MapGet("/users/{id}", (Id<User> id) => db.Users.Find(id));
```

For OpenAPI, route constraints, and error handling, add `AddStrictId()`:

```csharp
using StrictId.AspNetCore;

builder.Services.AddStrictId();
```

This registers:

**OpenAPI**: StrictId parameters and properties render as `string` schemas with a per-type `pattern`, `example`, and `description` instead of the raw underlying type.

**Route constraints**: pre-filter URL segments before they reach your action:

```csharp
app.MapGet("/users/{id:id}", ...);            // Matches ULID or GUID format
app.MapGet("/invoices/{id:idnumber}", ...);   // Matches decimal integers
app.MapGet("/products/{id:idstring}", ...);   // Matches non-empty strings
app.MapGet("/tenants/{id:strictguid}", ...);  // Matches GUID format
```

(`strictguid` instead of `guid` because ASP.NET Core already has a built-in `guid` constraint.)

**ProblemDetails**: StrictId parse failures become RFC 7807 `400 Bad Request` responses with the full diagnostic in `detail`.

**TypeConverters**: for legacy model binding paths (`System.ComponentModel`, configuration binding).

Cherry-pick if you only want some of it:

```csharp
builder.Services.AddStrictIdOpenApi();
builder.Services.AddStrictIdRouteConstraints();
builder.Services.AddStrictIdTypeConverters();
builder.Services.AddStrictIdProblemDetails();
```

## Analysers

Six Roslyn analysers ship inside the main package:

| ID       | Severity | What it catches                                                                        |
|----------|----------|----------------------------------------------------------------------------------------|
| STRID001 | Warning  | Cross-type `.Value` comparison (`userId.Value == orderId.Value`)                       |
| STRID002 | Info     | `default(Id<T>)` assigned to an `Id` property, probably meant `NewId()`                |
| STRID003 | Error    | Invalid `[IdPrefix]`: bad grammar, duplicates, missing `IsDefault`                     |
| STRID004 | Error    | Invalid `[IdSeparator]`: out-of-range enum cast                                        |
| STRID005 | Warning  | Wrong ID family for entity's attributes (e.g. using `Id<T>` when `T` has `[IdString]`) |
| STRID006 | Warning  | StrictId closed with an open generic type parameter instead of a concrete type         |

## v1/v2 to v3 migration

v3 is a ground-up rewrite, but your existing IDs should continue to work in most cases. The highlights:

- `new Id<T>("string")` is gone, use `Id<T>.Parse("string")`
- Default `ToString()` is now lowercase, use `"U"` format specifier for v2 uppercase
- `Id<T>` to `Id` conversion is now explicit (other direction stays implicit)
- `IId` interface replaced by `IStrictId<TSelf>`

## Acknowledgements

- [Ulid](https://github.com/Cysharp/Ulid), ULID implementation for C#, the engine behind `Id<T>`
- [StronglyTypedId](https://github.com/andrewlock/StronglyTypedId), for exploring this space first

## License

MIT
