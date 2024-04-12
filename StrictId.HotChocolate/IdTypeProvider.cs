using HotChocolate.Utilities;

namespace StrictId.HotChocolate;

/// <summary>
/// This has been replaced by the IdScalar, but is kept in case it is needed for a special case
/// </summary>
public class IdTypeProvider : IChangeTypeProvider
{
	public bool TryCreateConverter (Type source, Type target, ChangeTypeProvider root, out ChangeType converter)
	{
		if (source == typeof(Id) && target == typeof(string))
		{
			converter = input => ((Id)input!).ToString();
			return true;
		}
		
		if (source == typeof(string) && target == typeof(Id))
		{
			converter = input => Id.Parse((string)input!);
			return true;
		}
		
		if (source == typeof(Id?) && target == typeof(string))
		{
			converter = input => ((Id?)input)?.ToString();
			return true;
		}
		
		if (source == typeof(string) && target == typeof(Id?))
		{
			converter = input => Id.TryParse((string)input!, out var id) ? id : null!;
			return true;
		}
		
		if (source == typeof(Guid) && target == typeof(Id))
		{
			converter = input => new Id((Guid)input!);
			return true;
		}
		
		if (source == typeof(Id) && target == typeof(Guid))
		{
			converter = input => ((Id)input!).ToGuid();
			return true;
		}
		
		// Typed IDs
		
		if (source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Id<>) && target == typeof(string))
		{
			converter = input => input!.ToString();
			return true;
		}
		
		if (source == typeof(string) && target.IsGenericType && target.GetGenericTypeDefinition() == typeof(Id<>))
		{
			converter = input => Activator.CreateInstance(typeof(Id<>).MakeGenericType(target.GetGenericArguments()[0]), args: input);
			return true;
		}
		
		if (source == typeof(Guid) && target.IsGenericType && target.GetGenericTypeDefinition() == typeof(Id<>))
		{
			converter = input => Activator.CreateInstance(typeof(Id<>).MakeGenericType(target.GetGenericArguments()[0]), args: input);
			return true;
		}
		
		if (source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Id<>) && target == typeof(Guid))
		{
			converter = input => new Id(input!.ToString()!).ToGuid();
			return true;
		}

		if (source == typeof(Id) && target.IsGenericType && target.GetGenericTypeDefinition() == typeof(Id<>))
		{
			converter = input => Activator.CreateInstance(typeof(Id<>).MakeGenericType(target.GetGenericArguments()[0]), args: input);
			return true;
		}
		
		if (source.IsGenericType && source.GetGenericTypeDefinition() == typeof(Id<>) && target == typeof(Id))
		{
			converter = input => new Id(input!.ToString()!);
			return true;
		}
		
		converter = input => input;
		return false;
	}
}