using System.Text.Json;

namespace StrictId.Test.Aot;

/// <summary>
/// Self-contained check runner for the AOT smoke test. Each <c>Check</c> method
/// exercises a single dimension of StrictId's surface and throws
/// <see cref="InvalidOperationException"/> on failure so the console host can
/// surface the exception in its non-zero exit code.
/// </summary>
/// <remarks>
/// <para>
/// The runner deliberately avoids a test framework. NUnit + reflection-driven
/// discovery would defeat the purpose of the AOT test — the goal is to exercise
/// the library through straight-line C# the way an AOT-published app would, and
/// watch for any IL2xxx / IL3050 warnings at publish time.
/// </para>
/// <para>
/// Assertions use a hand-rolled <see cref="AssertEquals"/> helper rather than
/// FluentAssertions because FA depends on reflection to build its diagnostic
/// messages and pulls in a large surface area that clouds the AOT publish diff.
/// </para>
/// </remarks>
internal static class Checks
{
	// ═════ Parse / format round-trips ════════════════════════════════════════

	public static void IdOfT_RoundTripsCanonicalForm ()
	{
		var original = Id<User>.NewId();
		var text = original.ToString();
		AssertEquals(true, text.StartsWith("user_"), "Id<User>.ToString() canonical form must start with 'user_'.");

		var parsed = Id<User>.Parse(text);
		AssertEquals(original, parsed, "Id<User> round-trip through canonical form must preserve value.");
	}

	public static void IdOfT_FormatSpecifiers ()
	{
		var id = Id<User>.NewId();

		var canonical = id.ToString("C");
		var bare = id.ToString("B");
		var guidForm = id.ToString("G");
		var bareGuid = id.ToString("BG");

		AssertEquals(true, canonical.StartsWith("user_"), "C specifier must produce prefixed form.");
		AssertEquals(26, bare.Length, "B specifier must produce a bare 26-char ULID.");
		AssertEquals(true, guidForm.StartsWith("user_") && guidForm.Length > 26 + "user_".Length, "G specifier must produce prefixed GUID form.");
		AssertEquals(36, bareGuid.Length, "BG specifier must produce a bare 36-char GUID form.");
	}

	public static void IdNumberOfT_RoundTripsCanonicalForm ()
	{
		var original = new IdNumber<Invoice>(123456UL);
		var text = original.ToString();
		AssertEquals("inv_123456", text, "IdNumber<Invoice>.ToString() must produce prefixed decimal form.");

		var parsed = IdNumber<Invoice>.Parse(text);
		AssertEquals(original, parsed, "IdNumber<Invoice> round-trip must preserve value.");
	}

	public static void IdStringOfT_RoundTripsCanonicalForm ()
	{
		var original = new IdString<Customer>("cus_abc123");
		var text = original.ToString();
		AssertEquals("cus_abc123", text, "IdString<Customer>.ToString() must produce the canonical prefixed form.");

		var parsed = IdString<Customer>.Parse(text);
		AssertEquals(original, parsed, "IdString<Customer> round-trip must preserve value.");
	}

	public static void GuidOfT_RoundTripsCanonicalForm ()
	{
		var original = Guid<Product>.NewId();
		var text = original.ToString();
		AssertEquals(true, text.StartsWith("prod_"), "Guid<Product>.ToString() canonical form must start with 'prod_'.");

		var parsed = Guid<Product>.Parse(text);
		AssertEquals(original, parsed, "Guid<Product> round-trip through canonical form must preserve value.");
	}

	public static void GuidOfT_FormatSpecifiers ()
	{
		var id = Guid<Product>.NewId();

		var canonical = id.ToString("C");
		var bareD = id.ToString("D");
		var bareN = id.ToString("N");

		AssertEquals(true, canonical.StartsWith("prod_"), "C specifier must produce prefixed form.");
		AssertEquals(36, bareD.Length, "D specifier must produce a bare 36-char GUID.");
		AssertEquals(32, bareN.Length, "N specifier must produce a bare 32-char GUID.");
	}

	public static void CrossTypeEquality_NeverHolds ()
	{
		var userId = new Id<User>(Ulid.MaxValue);
		var orderId = new Id<Order>(Ulid.MaxValue);

		// The two ids have identical underlying ULIDs but are different closed
		// generic types — the type tag must prevent any equality relationship,
		// including boxed reference equality.
		object boxedUser = userId;
		object boxedOrder = orderId;
		AssertEquals(false, boxedUser.Equals(boxedOrder), "Cross-type Id<T> boxed equality must be false.");
		AssertEquals(false, boxedOrder.Equals(boxedUser), "Cross-type Id<T> boxed equality must be symmetric.");
	}

	// ═════ Monotonic generation ══════════════════════════════════════════════

	public static void IdOfT_NewIdBurst_IsUniqueAndTimeOrdered ()
	{
		// StrictId delegates generation to Cysharp.Ulid's Ulid.NewUlid(), which
		// guarantees *timestamp* monotonicity but randomises the low 80 bits each call
		// — so lexicographic ordering within the same millisecond is not guaranteed.
		// The invariants that must hold are: every generated id is unique, and the
		// timestamp component is non-decreasing across the burst. See the Phase 0
		// decision in implementation-plan.md for why StrictId does not wrap the
		// generator in its own monotonic layer.
		const int burstSize = 1024;
		var ids = new Id<User>[burstSize];
		for (var i = 0; i < burstSize; i++)
			ids[i] = Id<User>.NewId();

		var seen = new HashSet<Id<User>>();
		foreach (var id in ids)
		{
			if (!seen.Add(id))
				throw new InvalidOperationException($"NewId() burst produced a duplicate id: '{id}'.");
		}

		for (var i = 1; i < burstSize; i++)
		{
			if (ids[i].Time < ids[i - 1].Time)
				throw new InvalidOperationException(
					$"NewId() burst produced a timestamp regression at index {i}: {ids[i - 1].Time:O} → {ids[i].Time:O}.");
		}
	}

	// ═════ JSON round-trip via source-generated context ══════════════════════

	public static void StrictIdDto_JsonRoundTrip_UsesSourceGeneratedContext ()
	{
		var original = new StrictIdSmokeDto
		{
			UserId = Id<User>.NewId(),
			InvoiceNumber = new IdNumber<Invoice>(42UL),
			CustomerKey = new IdString<Customer>("cus_stripelike1"),
			BareId = Id.NewId(),
			BareNumber = new IdNumber(7UL),
			BareString = new IdString("bare-value"),
			ProductId = Guid<Product>.NewId(),
			OrderNames =
			{
				[Id<Order>.NewId()] = "First order",
				[Id<Order>.NewId()] = "Second order",
			},
		};

		var json = JsonSerializer.Serialize(original, SmokeTestJsonContext.Default.StrictIdSmokeDto);
		var roundTripped = JsonSerializer.Deserialize(json, SmokeTestJsonContext.Default.StrictIdSmokeDto);

		AssertEquals(true, roundTripped is not null, "Deserialize returned null.");
		AssertEquals(original.UserId, roundTripped!.UserId, "UserId changed across round-trip.");
		AssertEquals(original.InvoiceNumber, roundTripped.InvoiceNumber, "InvoiceNumber changed across round-trip.");
		AssertEquals(original.CustomerKey, roundTripped.CustomerKey, "CustomerKey changed across round-trip.");
		AssertEquals(original.BareId, roundTripped.BareId, "BareId changed across round-trip.");
		AssertEquals(original.BareNumber, roundTripped.BareNumber, "BareNumber changed across round-trip.");
		AssertEquals(original.BareString, roundTripped.BareString, "BareString changed across round-trip.");
		AssertEquals(original.ProductId, roundTripped.ProductId, "ProductId changed across round-trip.");
		AssertEquals(original.OrderNames.Count, roundTripped.OrderNames.Count, "OrderNames dictionary size changed.");

		foreach (var (key, value) in original.OrderNames)
		{
			if (!roundTripped.OrderNames.TryGetValue(key, out var roundValue))
				throw new InvalidOperationException($"OrderNames lost key '{key}' during round-trip.");
			AssertEquals(value, roundValue, $"OrderNames value for '{key}' changed during round-trip.");
		}
	}

	// ═════ Shared assertion helper ═══════════════════════════════════════════

	private static void AssertEquals<T> (T expected, T actual, string context)
	{
		if (EqualityComparer<T>.Default.Equals(expected, actual)) return;
		throw new InvalidOperationException($"{context} Expected '{expected}' but got '{actual}'.");
	}
}
