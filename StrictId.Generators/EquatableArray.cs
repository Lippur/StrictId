using System.Collections;
using System.Collections.Immutable;

namespace StrictId.Generators;

/// <summary>
/// A value-equality wrapper around an <see cref="ImmutableArray{T}"/>. Required for
/// incremental source generators because the default <see cref="ImmutableArray{T}"/>
/// equality is reference-based, which defeats the incremental cache and causes
/// generator pipelines to re-run unnecessarily on every edit.
/// </summary>
/// <typeparam name="T">The element type; must itself implement value equality.</typeparam>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	public static readonly EquatableArray<T> Empty = new(ImmutableArray<T>.Empty);

	private readonly ImmutableArray<T> _array;

	public EquatableArray (ImmutableArray<T> array)
	{
		_array = array;
	}

	public int Length => _array.IsDefault ? 0 : _array.Length;

	public T this [int index] => _array[index];

	public bool IsEmpty => _array.IsDefaultOrEmpty;

	public ImmutableArray<T> AsImmutableArray () => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

	public bool Equals (EquatableArray<T> other)
	{
		if (_array.IsDefault && other._array.IsDefault) return true;
		if (_array.IsDefault || other._array.IsDefault) return false;
		if (_array.Length != other._array.Length) return false;
		for (var i = 0; i < _array.Length; i++)
		{
			if (!_array[i].Equals(other._array[i])) return false;
		}
		return true;
	}

	public override bool Equals (object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode ()
	{
		if (_array.IsDefaultOrEmpty) return 0;
		var hash = 17;
		foreach (var item in _array)
			hash = hash * 31 + (item?.GetHashCode() ?? 0);
		return hash;
	}

	public IEnumerator<T> GetEnumerator () =>
		_array.IsDefault
			? ((IEnumerable<T>)Array.Empty<T>()).GetEnumerator()
			: ((IEnumerable<T>)_array).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();

	public static bool operator == (EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);
	public static bool operator != (EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
