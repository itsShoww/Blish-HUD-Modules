using System.Text.RegularExpressions;

namespace Nekres.Mumble_Info_Module
{
    public static class StringExtensions
    {
        public static string SplitAtUpperCase(this string source)
        {
            return Regex.Replace(source, "([A-Z]|[1-9])", " $1", RegexOptions.Compiled);
        }
    }
}
