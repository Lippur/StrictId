namespace StrictId.Test.Aot;

/// <summary>
/// Marker types used across the AOT smoke test. All have <c>[IdPrefix]</c> declared
/// so the StrictId source generator emits module-init registrations for the
/// corresponding <c>Id&lt;T&gt;</c>, <c>IdNumber&lt;T&gt;</c>, and
/// <c>IdString&lt;T&gt;</c> closed generics — which is what the smoke test is
/// validating. A type without an <c>[IdPrefix]</c> would fall through to the
/// reflection fallback in the JSON factories and trigger the IL3050 warnings the
/// test exists to catch.
/// </summary>
public static class TestEntities;

/// <summary>User entity with the <c>user</c> prefix.</summary>
[IdPrefix("user")]
public sealed class User;

/// <summary>Order entity with multiple prefix aliases.</summary>
[IdPrefix("order", IsDefault = true)]
[IdPrefix("ord")]
public sealed class Order;

/// <summary>Stripe-style customer entity with an alphanumeric string ID constraint.</summary>
[IdPrefix("cus")]
[IdString(MaxLength = 32, CharSet = IdStringCharSet.Alphanumeric)]
public sealed class Customer;

/// <summary>Invoice entity used to exercise the numeric family with a prefix.</summary>
[IdPrefix("inv")]
public sealed class Invoice;
