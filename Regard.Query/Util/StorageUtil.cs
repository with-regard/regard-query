using System.Text;
using System.Text.RegularExpressions;

namespace Regard.Query.Util
{
    class StorageUtil
    {
        private const string c_InvalidChars = @"([\/\\\#\?\-]|[\x00-\x1f]|[\x7f-\x9f])";

        /// <summary>
        /// Replaces 'invalid' characters from a partition or row key. See http://msdn.microsoft.com/en-us/library/windowsazure/dd179338.aspx for
        /// details on what constitutes an invalid character.
        /// <para/>
        /// Replacements consist of the string '-xx-' where 'xx' is the hexadecimal value of the invalid character. '-' is added as an invalid character
        /// to ensure that the results are unique.
        /// </summary>
        public static string SanitiseKey(string key)
        {
            if (key == null) return null;

            return Regex.Replace(key, c_InvalidChars, match =>
            {
                StringBuilder result = new StringBuilder();

                result.Append('-');
                foreach (var matchChar in match.Value)
                {
                    result.Append(((int)matchChar).ToString("x"));
                }
                result.Append('-');

                return result.ToString();
            });
        }
    }
}
