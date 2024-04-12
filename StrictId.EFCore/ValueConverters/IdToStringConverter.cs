using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace StrictId.EFCore.ValueConverters;

public class IdToStringConverter () : ValueConverter<Id, string>(
	id => id.ToString(),
	value => new Id(value)
);

public class IdToStringConverter<T> () : ValueConverter<Id<T>, string>(
	id => id.ToString(),
	value => new Id<T>(value)
);