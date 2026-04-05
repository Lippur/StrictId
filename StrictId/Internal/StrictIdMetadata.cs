namespace StrictId.Internal;

/// <summary>
/// Per-entity-type cache of resolved prefix and separator metadata. Every closed
/// generic instantiation of this class gets its own <see cref="Prefix"/> field, JITted
/// once per <typeparamref name="T"/> and cached forever — no dictionary lookups and no
/// locks on the hot path.
/// </summary>
/// <typeparam name="T">The entity type whose metadata is being cached.</typeparam>
internal static class StrictIdMetadata<T>
{
	/// <summary>
	/// The resolved <see cref="PrefixInfo"/> for <typeparamref name="T"/>. Computed once
	/// on first access by <see cref="StrictIdMetadataResolver.ResolvePrefix"/> and cached
	/// in this static field for the lifetime of the AppDomain.
	/// </summary>
	public static readonly PrefixInfo Prefix = StrictIdMetadataResolver.ResolvePrefix(typeof(T));
}

/// <summary>
/// Per-entity-type cache of resolved <see cref="IdStringAttribute"/> options. Used only
/// by <c>IdString&lt;T&gt;</c>; kept separate from <see cref="StrictIdMetadata{T}"/> so
/// that the ULID and numeric families do not pay for a scan they never use.
/// </summary>
/// <typeparam name="T">The entity type whose string options are being cached.</typeparam>
internal static class IdStringMetadata<T>
{
	/// <summary>
	/// The resolved <see cref="IdStringOptions"/> for <typeparamref name="T"/>. Computed
	/// once on first access and cached for the lifetime of the AppDomain.
	/// </summary>
	public static readonly IdStringOptions Options = StrictIdMetadataResolver.ResolveStringOptions(typeof(T));
}
