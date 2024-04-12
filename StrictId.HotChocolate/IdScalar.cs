using HotChocolate.Language;
using HotChocolate.Types;

namespace StrictId.HotChocolate;

public sealed class IdScalar : ScalarType<Id, StringValueNode>
{
	public IdScalar () : this(nameof(Id)) { }

	public IdScalar (string name, BindingBehavior bind = BindingBehavior.Implicit) : base(name, bind) { }

	protected override Id ParseLiteral (StringValueNode valueSyntax) => new(valueSyntax.Value);

	protected override StringValueNode ParseValue (Id runtimeValue) => new(runtimeValue.ToString());

	protected override bool IsInstanceOfType (StringValueNode valueSyntax) => Id.IsValid(valueSyntax.Value);

	public override IValueNode ParseResult (object? resultValue) => ParseValue(resultValue);
}

public class IdTypedScalar : ScalarType
{
	// Generic scalars are not allowed in HotChocolate, so sadly this is what we have to do
	private readonly Type _idType;

	public IdTypedScalar (Type type) : base($"{type.Name}Id", BindingBehavior.Implicit)
	{
		_idType = type;
	}

	public override Type RuntimeType => typeof(Id<>).MakeGenericType(_idType);

	public override bool IsInstanceOfType (IValueNode valueSyntax) =>
		valueSyntax is StringValueNode && Id.IsValid((string?)valueSyntax.Value);

	public override object? ParseLiteral (IValueNode valueSyntax) =>
		Activator.CreateInstance(RuntimeType, ((StringValueNode)valueSyntax).Value);

	public override IValueNode ParseValue (object? runtimeValue) =>
		new StringValueNode(Activator.CreateInstance(RuntimeType, runtimeValue)!.ToString()!);

	public override IValueNode ParseResult (object? resultValue) => ParseValue(resultValue);

	public override bool TrySerialize (object? runtimeValue, out object? resultValue)
	{
		resultValue = null;

		if (runtimeValue is null) return true;

		if (runtimeValue.GetType() != RuntimeType) return false;

		resultValue = runtimeValue.ToString();
		return true;
	}

	public override bool TryDeserialize (object? resultValue, out object? runtimeValue)
	{
		runtimeValue = null;

		if (resultValue is not StringValueNode stringValueNode || !Id.IsValid(stringValueNode.Value)) return false;

		runtimeValue = Activator.CreateInstance(RuntimeType, stringValueNode.Value);
		return true;
	}
}