using System.Globalization;

namespace Helix.Avalonia;

/// <summary>
/// Evaluates a small arithmetic expression typed in a number field: + - * / %, parentheses, unary minus,
/// decimals with "." or the culture's separator. A leading "=" is allowed ("=120*0.8"). No variables, no functions.
/// </summary>
public static class HelixExpression
{
    public static bool TryEvaluate(string? text, CultureInfo culture, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var source = text.Trim().TrimStart('=').Replace('−', '-').Replace('×', '*').Replace('÷', '/');
        var decimalSeparator = culture.NumberFormat.NumberDecimalSeparator;
        var groupSeparator = culture.NumberFormat.NumberGroupSeparator;
        var parser = new Parser(source, decimalSeparator, groupSeparator);
        try
        {
            value = parser.Parse();
            return double.IsFinite(value);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed class Parser(string text, string decimalSeparator, string groupSeparator)
    {
        private int _pos;

        public double Parse()
        {
            var result = Sum();
            Skip();
            if (_pos != text.Length) throw new FormatException();
            return result;
        }

        private void Skip()
        {
            while (_pos < text.Length && (char.IsWhiteSpace(text[_pos]) || text[_pos] == ' ' || text[_pos] == ' ')) _pos++;
        }

        private bool Eat(char c)
        {
            Skip();
            if (_pos < text.Length && text[_pos] == c)
            {
                _pos++;
                return true;
            }

            return false;
        }

        private double Sum()
        {
            var value = Product();
            while (true)
            {
                if (Eat('+')) value += Product();
                else if (Eat('-')) value -= Product();
                else return value;
            }
        }

        private double Product()
        {
            var value = Unary();
            while (true)
            {
                if (Eat('*')) value *= Unary();
                else if (Eat('/')) value /= Unary();
                else if (Eat('%')) value %= Unary();
                else return value;
            }
        }

        private double Unary()
        {
            if (Eat('-')) return -Unary();
            if (Eat('+')) return Unary();
            return Atom();
        }

        private double Atom()
        {
            if (Eat('('))
            {
                var inner = Sum();
                if (!Eat(')')) throw new FormatException();
                return inner;
            }

            Skip();
            var start = _pos;
            while (_pos < text.Length && (char.IsDigit(text[_pos]) || text[_pos] is '.' or ',' or '\'' || (groupSeparator.Length == 1 && text[_pos] == groupSeparator[0] && groupSeparator != " ")))
            {
                _pos++;
            }

            var token = text[start.._pos];
            if (token.Length == 0) throw new FormatException();
            return Number(token);
        }

        /// <summary>"1,5" and "1.5" are both 1.5 when the culture uses a comma; "1,234.5" is 1234.5.</summary>
        private double Number(string token)
        {
            token = token.Replace("'", "");
            var lastDot = token.LastIndexOf('.');
            var lastComma = token.LastIndexOf(',');
            string normalized;
            if (lastDot >= 0 && lastComma >= 0)
            {
                // Both: the last one is the decimal separator, the other groups thousands.
                normalized = lastDot > lastComma ? token.Replace(",", "") : token.Replace(".", "").Replace(',', '.');
            }
            else if (lastComma >= 0)
            {
                normalized = decimalSeparator == "," || token.Count(c => c == ',') == 1 && token.Length - lastComma - 1 != 3
                    ? token.Replace(',', '.')
                    : token.Replace(",", "");
            }
            else
            {
                normalized = token;
            }

            return double.Parse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        }
    }
}
