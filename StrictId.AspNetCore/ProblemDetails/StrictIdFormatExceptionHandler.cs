using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace StrictId.AspNetCore.ProblemDetails;

/// <summary>
/// Maps <see cref="FormatException"/>s raised by StrictId parsers to an RFC 7807
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> response with
/// <see cref="StatusCodes.Status400BadRequest"/>. Registered via
/// <see cref="StrictIdAspNetCoreExtensions.AddStrictIdProblemDetails"/>.
/// </summary>
/// <remarks>
/// <para>
/// Route binding and JSON deserialisation already surface 400s for most parse
/// failures, so the primary audience for this handler is code paths where a
/// <see cref="FormatException"/> would otherwise escape unhandled to the pipeline and
/// be rendered as a 500: unwrapped JSON body failures without the default validation
/// pipeline, legacy TypeConverter-based binding, or deliberate <c>Parse</c> calls from
/// action bodies.
/// </para>
/// <para>
/// Recognition is based on <see cref="Exception.Source"/>: <see cref="Exception.Source"/>
/// defaults to the assembly name of the throwing method, so every
/// <see cref="FormatException"/> originating inside StrictId carries
/// <c>Source == "StrictId"</c>. The handler walks the inner-exception chain so wrapped
/// failures (for example <c>JsonException → FormatException</c>) are also caught.
/// Matching on <see cref="Exception.Source"/> rather than exception type keeps the
/// handler scoped: a <see cref="FormatException"/> raised by arbitrary third-party code
/// still falls through to the framework's default handling.
/// </para>
/// </remarks>
public sealed class StrictIdFormatExceptionHandler : IExceptionHandler
{
	internal const string StrictIdAssemblyName = "StrictId";

	/// <inheritdoc />
	public async ValueTask<bool> TryHandleAsync (
		HttpContext httpContext,
		Exception exception,
		CancellationToken cancellationToken
	)
	{
		if (FindStrictIdFormatException(exception) is not { } strictIdFailure)
			return false;

		httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

		var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
		{
			Status = StatusCodes.Status400BadRequest,
			Title = "StrictId parse failed",
			Detail = strictIdFailure.Message,
			Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
		};

		// Prefer IProblemDetailsService when registered so the app's
		// CustomizeProblemDetails hook still runs; fall back to writing JSON directly
		// so this handler is useful even when AddProblemDetails() has not been called.
		var problemDetailsService = httpContext.RequestServices.GetService(typeof(IProblemDetailsService)) as IProblemDetailsService;
		if (problemDetailsService is not null)
		{
			var context = new ProblemDetailsContext
			{
				HttpContext = httpContext,
				ProblemDetails = problemDetails,
				Exception = exception,
			};
			if (await problemDetailsService.TryWriteAsync(context))
				return true;
		}

		await httpContext.Response.WriteAsJsonAsync(
			problemDetails,
			options: null,
			contentType: "application/problem+json",
			cancellationToken: cancellationToken);
		return true;
	}

	/// <summary>
	/// Walks the exception chain looking for a <see cref="FormatException"/> whose
	/// <see cref="Exception.Source"/> identifies it as originating inside the StrictId
	/// assembly. Returns the matching exception, or <see langword="null"/> on miss.
	/// </summary>
	private static FormatException? FindStrictIdFormatException (Exception? exception)
	{
		for (var current = exception; current is not null; current = current.InnerException)
		{
			if (current is FormatException fe && string.Equals(fe.Source, StrictIdAssemblyName, StringComparison.Ordinal))
				return fe;
		}
		return null;
	}
}
