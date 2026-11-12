using System.Globalization;
using System.Text;

namespace Path2Ico.Services
{
    /// <summary>
    /// Normalizes SVG path data so that WPF's Geometry.Parse can consume it.
    /// The only incompatible construct is the arc command, where SVG allows the
    /// large-arc/sweep flags to be written without separators (e.g. "a5 5 0 011 1").
    /// </summary>
    public static class SvgPathNormalizer
    {
        public static string Normalize(string data)
        {
            if (string.IsNullOrEmpty(data)) return data;

            var builder = new StringBuilder(data.Length + 16);
            int i = 0;
            while (i < data.Length)
            {
                char c = data[i];
                if (c == 'a' || c == 'A')
                {
                    builder.Append(c);
                    i++;
                    i = ReadArcArguments(data, i, builder);
                }
                else
                {
                    builder.Append(c);
                    i++;
                }
            }
            return builder.ToString();
        }

        private static int ReadArcArguments(string data, int index, StringBuilder builder)
        {
            int start = index;
            while (true)
            {
                int cursor = index;
                double rx, ry, rotation, x, y;
                char largeArc, sweep;

                if (!TryReadNumber(data, ref cursor, out rx)) break;
                if (!TryReadNumber(data, ref cursor, out ry)) break;
                if (!TryReadNumber(data, ref cursor, out rotation)) break;
                if (!TryReadFlag(data, ref cursor, out largeArc)) break;
                if (!TryReadFlag(data, ref cursor, out sweep)) break;
                if (!TryReadNumber(data, ref cursor, out x)) break;
                if (!TryReadNumber(data, ref cursor, out y)) break;

                builder.Append(' ').Append(Format(rx));
                builder.Append(' ').Append(Format(ry));
                builder.Append(' ').Append(Format(rotation));
                builder.Append(' ').Append(largeArc);
                builder.Append(' ').Append(sweep);
                builder.Append(' ').Append(Format(x));
                builder.Append(' ').Append(Format(y));

                index = cursor;
            }
            return index == start ? start : index;
        }

        private static bool TryReadNumber(string data, ref int index, out double value)
        {
            value = 0;
            int n = data.Length;
            int cursor = index;
            while (cursor < n && IsSeparator(data[cursor])) cursor++;

            int numberStart = cursor;
            if (cursor < n && (data[cursor] == '+' || data[cursor] == '-')) cursor++;

            bool hasDigits = false;
            while (cursor < n && char.IsDigit(data[cursor])) { cursor++; hasDigits = true; }
            if (cursor < n && data[cursor] == '.')
            {
                cursor++;
                while (cursor < n && char.IsDigit(data[cursor])) { cursor++; hasDigits = true; }
            }
            if (!hasDigits) return false;

            if (cursor < n && (data[cursor] == 'e' || data[cursor] == 'E'))
            {
                int exponent = cursor + 1;
                if (exponent < n && (data[exponent] == '+' || data[exponent] == '-')) exponent++;
                bool hasExponentDigits = false;
                while (exponent < n && char.IsDigit(data[exponent])) { exponent++; hasExponentDigits = true; }
                if (hasExponentDigits) cursor = exponent;
            }

            value = double.Parse(data.Substring(numberStart, cursor - numberStart), CultureInfo.InvariantCulture);
            index = cursor;
            return true;
        }

        private static bool TryReadFlag(string data, ref int index, out char flag)
        {
            flag = '0';
            int n = data.Length;
            int cursor = index;
            while (cursor < n && IsSeparator(data[cursor])) cursor++;

            if (cursor < n && (data[cursor] == '0' || data[cursor] == '1'))
            {
                flag = data[cursor];
                index = cursor + 1;
                return true;
            }
            return false;
        }

        private static bool IsSeparator(char c)
        {
            return c == ',' || char.IsWhiteSpace(c);
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", CultureInfo.InvariantCulture);
        }
    }
}
