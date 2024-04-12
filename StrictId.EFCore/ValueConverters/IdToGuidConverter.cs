using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

public class IdToGuidConverter () : ValueConverter<Id, Guid>(
	id => id.ToGuid(),
	value => new Id(value)
);

public class IdTypedToGuidConverter<T> () : ValueConverter<Id<T>, Guid>(
	id => id.ToGuid(),
	value => new Id<T>(value)
);