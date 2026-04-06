namespace StrictId.AspNetCore.Test;

/// <summary>
/// Marker types used across the AspNetCore integration tests. Kept in a single file so
/// the per-test-file fixtures do not need to re-declare the standard happy-path entity
/// shapes. Each marker is a <c>public sealed class</c> so closed generics over it are
/// visible both to runtime reflection and the StrictId source generator.
/// </summary>
public static class TestEntities;

/// <summary>A user entity with a canonical <c>user</c> prefix and default separator.</summary>
[IdPrefix("user")]
public sealed class User;

/// <summary>An order entity with multiple prefix aliases — <c>order</c> (canonical), <c>ord</c>, <c>o</c>.</summary>
[IdPrefix("order", IsDefault = true)]
[IdPrefix("ord")]
[IdPrefix("o")]
public sealed class Order;

/// <summary>An entity used to exercise the numeric family with a canonical <c>inv</c> prefix.</summary>
[IdPrefix("inv")]
public sealed class Invoice;

/// <summary>A customer entity: <c>cus</c> prefix, alphanumeric suffix up to 32 chars.</summary>
[IdPrefix("cus")]
[IdString(MaxLength = 32, CharSet = IdStringCharSet.Alphanumeric)]
public sealed class Customer;

/// <summary>A product entity used to exercise the Guid family with a <c>prod</c> prefix.</summary>
[IdPrefix("prod")]
public sealed class Product;

/// <summary>An entity with no prefix — StrictIds round-trip bare.</summary>
public sealed class Anonymous;
