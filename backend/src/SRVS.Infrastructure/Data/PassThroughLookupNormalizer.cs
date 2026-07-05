using Microsoft.AspNetCore.Identity;

namespace SRVS.Web.Data;

/// <summary>
/// A no-op lookup normalizer that returns values unchanged.
/// Used because we removed the NormalizedUserName / NormalizedEmail columns
/// from the database, so Identity should not uppercase or transform values.
/// </summary>
public sealed class PassThroughLookupNormalizer : ILookupNormalizer
{
	public string NormalizeName(string? name) => name ?? string.Empty;

	public string NormalizeEmail(string? email) => email ?? string.Empty;
}
