using System.Text;

namespace WhatsAppBot
{
    public static class StringExtensions
    {
        public static string RemovePunctuation(this string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s)
            {
                if (!char.IsPunctuation(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
