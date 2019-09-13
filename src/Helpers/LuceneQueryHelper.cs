using System.Text;

namespace EPiServer.DynamicLuceneExtensions.Helpers
{
    public static class LuceneQueryHelper
    {
        public static string EscapeParenthesis(string value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < value.Length; ++index)
            {
                char ch = value[index];
                switch (ch)
                {
                    case '(':
                    case ')':
                        stringBuilder.Append('\\');
                        break;
                }
                stringBuilder.Append(ch);
            }
            return stringBuilder.ToString();
        }

        public static string Escape(string value)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < value.Length; ++index)
            {
                char ch = value[index];
                switch (ch)
                {
                    case '!':
                    case '"':
                    case '&':
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                    case '-':
                    case ':':
                    case '?':
                    case '[':
                    case '\\':
                    case ']':
                    case '^':
                    case '{':
                    case '|':
                    case '}':
                    case '~':
                        stringBuilder.Append('\\');
                        break;
                }
                stringBuilder.Append(ch);
            }
            return stringBuilder.ToString();
        }
    }
}