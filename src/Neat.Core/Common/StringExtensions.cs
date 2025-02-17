using System.Text.RegularExpressions;
namespace Neat.Core.Common;

public static class StringExtensions
{
    public static string GetAbbreviation(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var letters = Regex
            .Split(value, @"(?=[A-Z])") // split by case change, like HelloWorld -> [Hello, World]
            .Where(x => x.Length > 0)
            .Select(x => x[0].ToString()); // get first letter of each word

        var result = string.Join("", letters);
        if (string.IsNullOrWhiteSpace(result)) result = value[0].ToString();

        return result.ToUpper();
    }
}
