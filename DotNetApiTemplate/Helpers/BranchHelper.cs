using DotNetApiTemplate.Exceptions;

namespace SupermarketERP.Helpers;

public static class BranchHelper
{
    public static string GenerateBranchCode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidBranchName);
        }

        var prefix = new string(name
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .Take(3)
            .ToArray());

        if (prefix.Length < 3)
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidBranchName);
        }

        var suffix = Guid.NewGuid()
            .ToString("N")[..6]
            .ToUpperInvariant();

        return $"{prefix}-{suffix}";
    }
}