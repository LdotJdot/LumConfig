using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
namespace LumConfg
{
    public static class LumJson
    {
        static public object? Deserialize(string json)
        {
            return LumJsonDeserializer.Parse(json);
        }
        static public string Serialize(object jsonObject,int indent=0)
        {
            return LumJsonSerializer.Parse(jsonObject, indent);
        }
    }

    internal static class LumJsonDeserializer
    {
        public static object? Parse(string json)
        {
            var reader = new JsonReader(json);
            return reader.ReadValue();
        }

        private ref struct JsonReader
        {
            private readonly ReadOnlySpan<char> _span;
            private int _position;

            public JsonReader(string json)
            {
                _span = json.AsSpan();
                _position = 0;
            }

            public object? ReadValue()
            {
                SkipWhitespaceAndComments();

                if (_position >= _span.Length)
                    ThrowFormatException("Unexpected end of JSON");

                var current = _span[_position];
                return current switch
                {
                    '{' => ReadObject(),
                    '[' => ReadArray(),
                    '"' => ReadString(),
                    't' or 'f' => ReadBoolean(),
                    'n' => ReadNull(),
                    _ when IsDigit(current) || current == '-' => ReadNumber(),
                    '/' => ThrowUnexpectedComment(),
                    _ => ThrowUnexpectedCharacter(current)
                };
            }

            private Dictionary<string, object?> ReadObject()
            {
                var obj = new Dictionary<string, object?>();

                _position++; // 跳过 '{'
                SkipWhitespaceAndComments();

                if (TryConsume('}'))
                    return obj;

                while (true)
                {
                    SkipWhitespaceAndComments();

                    if (_span[_position] != '"')
                        ThrowFormatException("Expected string key in object");

                    var key = ReadString();
                    SkipWhitespaceAndComments();

                    Consume(':');
                    SkipWhitespaceAndComments();

                    var value = ReadValue();
                    obj[key] = value;

                    SkipWhitespaceAndComments();

                    if (TryConsume('}'))
                        break;

                    Consume(',');
                    SkipWhitespaceAndComments();
                }

                return obj;
            }

            private List<object?> ReadArray()
            {
                var list = new List<object?>();

                _position++; // 跳过 '['
                SkipWhitespaceAndComments();

                if (TryConsume(']'))
                    return list;

                while (true)
                {
                    SkipWhitespaceAndComments();

                    var value = ReadValue();
                    list.Add(value);

                    SkipWhitespaceAndComments();

                    if (TryConsume(']'))
                        break;

                    Consume(',');
                    SkipWhitespaceAndComments();
                }

                return list;
            }

            private string ReadString()
            {
                _position++; // 跳过 '"'
                int start = _position;
                int length = 0;
                bool hasEscapes = false;

                // 第一遍：计算长度和检测转义字符
                while (_position < _span.Length)
                {
                    var current = _span[_position];

                    if (current == '"')
                        break;

                    if (current == '\\')
                    {
                        hasEscapes = true;
                        _position++; // 跳过转义字符
                        length++; // 跳过转义字符
                        if (_position >= _span.Length)
                            break;
                    }

                    _position++;
                    length++;
                }

                if (_position >= _span.Length || _span[_position] != '"')
                    ThrowFormatException("Unterminated string");

                var resultSpan = _span.Slice(start, length);
                _position++; // 跳过结尾的 '"'

                if (!hasEscapes)
                    return new string(resultSpan);

                return ProcessStringWithEscapes(resultSpan);
            }

            private string ProcessStringWithEscapes(ReadOnlySpan<char> span)
            {
                Span<char> buffer = span.Length <= 256
                    ? stackalloc char[span.Length]
                    : new char[span.Length];

                int bufferIndex = 0;
                int spanIndex = 0;

                while (spanIndex < span.Length)
                {
                    var current = span[spanIndex];

                    if (current == '\\')
                    {
                        spanIndex++;
                        if (spanIndex >= span.Length)
                            ThrowFormatException("Invalid escape sequence");

                        current = span[spanIndex];
                        buffer[bufferIndex++] = current switch
                        {
                            '"' => '"',
                            '\\' => '\\',
                            '/' => '/',
                            'b' => '\b',
                            'f' => '\f',
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            'u' => ProcessUnicodeEscape(span, ref spanIndex),
                            _ => ThrowInvalidEscape(current)
                        };
                        spanIndex++;
                    }
                    else
                    {
                        buffer[bufferIndex++] = current;
                        spanIndex++;
                    }
                }

                return new string(buffer.Slice(0, bufferIndex));
            }

            private char ProcessUnicodeEscape(ReadOnlySpan<char> span, ref int index)
            {
                if (index + 4 >= span.Length)
                    ThrowFormatException("Incomplete Unicode escape sequence");

                var hex = span.Slice(index + 1, 4);
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
                    ThrowFormatException("Invalid Unicode escape sequence");

                index += 4;
                return (char)codePoint;
            }

            private object ReadNumber()
            {
                int start = _position;

                if (TryConsume('-'))
                    start = _position;

                // 快速扫描数字
                while (_position < _span.Length && IsDigit(_span[_position]))
                    _position++;

                bool isDouble = false;

                if (_position < _span.Length && _span[_position] == '.')
                {
                    isDouble = true;
                    _position++;
                    while (_position < _span.Length && IsDigit(_span[_position]))
                        _position++;
                }

                if (_position < _span.Length && (_span[_position] == 'e' || _span[_position] == 'E'))
                {
                    isDouble = true;
                    _position++;
                    if (_position < _span.Length && (_span[_position] == '+' || _span[_position] == '-'))
                        _position++;

                    while (_position < _span.Length && IsDigit(_span[_position]))
                        _position++;
                }

                var numberSpan = _span.Slice(start, _position - start);

                // 方案1：优先尝试解析为整数
                if (!isDouble && TryParseInteger(numberSpan, out long intValue))
                    return intValue;

                // 方案2：使用 double.TryParse（优化版）
                if (TryParseDouble(numberSpan, out double doubleValue))
                    return doubleValue;

                ThrowFormatException("Invalid number format");
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool TryParseInteger(ReadOnlySpan<char> span, out long value)
            {
                value = 0;
                bool isNegative = false;
                int start = 0;

                if (span.Length > 0 && span[0] == '-')
                {
                    isNegative = true;
                    start = 1;
                }

                // 快速整数解析
                for (int i = start; i < span.Length; i++)
                {
                    var c = span[i];
                    if (c < '0' || c > '9')
                        return false;

                    // 检查溢出
                    if (value > (long.MaxValue - (c - '0')) / 10)
                        return false;

                    value = value * 10 + (c - '0');
                }

                if (isNegative)
                    value = -value;

                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool TryParseDouble(ReadOnlySpan<char> span, out double result)
            {
                // 方案A：使用 double.TryParse（最兼容）
                return double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

                // 方案B：手动解析（更高效但复杂）
                // return ManualDoubleParse(span, out result);
            }

            // 可选：手动解析 double（更高效但更复杂）
            private static bool ManualDoubleParse(ReadOnlySpan<char> span, out double result)
            {
                result = 0;
                if (span.IsEmpty) return false;

                int index = 0;
                bool isNegative = false;

                // 处理符号
                if (span[0] == '-')
                {
                    isNegative = true;
                    index++;
                }
                else if (span[0] == '+')
                {
                    index++;
                }

                // 解析整数部分
                long integerPart = 0;
                while (index < span.Length && IsDigit(span[index]))
                {
                    integerPart = integerPart * 10 + (span[index] - '0');
                    index++;
                }

                // 解析小数部分
                double fractionalPart = 0;
                if (index < span.Length && span[index] == '.')
                {
                    index++;
                    double divisor = 10.0;
                    while (index < span.Length && IsDigit(span[index]))
                    {
                        fractionalPart += (span[index] - '0') / divisor;
                        divisor *= 10.0;
                        index++;
                    }
                }

                // 解析指数部分
                int exponent = 0;
                if (index < span.Length && (span[index] == 'e' || span[index] == 'E'))
                {
                    index++;
                    bool expNegative = false;

                    if (index < span.Length && span[index] == '-')
                    {
                        expNegative = true;
                        index++;
                    }
                    else if (index < span.Length && span[index] == '+')
                    {
                        index++;
                    }

                    while (index < span.Length && IsDigit(span[index]))
                    {
                        exponent = exponent * 10 + (span[index] - '0');
                        index++;
                    }

                    if (expNegative)
                        exponent = -exponent;
                }

                if (index != span.Length)
                    return false;

                result = (integerPart + fractionalPart) * Math.Pow(10, exponent);
                if (isNegative)
                    result = -result;

                return true;
            }

            private bool ReadBoolean()
            {
                if (_span[_position] == 't')
                {
                    if (_position + 3 >= _span.Length ||
                        _span[_position + 1] != 'r' ||
                        _span[_position + 2] != 'u' ||
                        _span[_position + 3] != 'e')
                        ThrowFormatException("Expected 'true'");

                    _position += 4;
                    return true;
                }
                else
                {
                    if (_position + 4 >= _span.Length ||
                        _span[_position + 1] != 'a' ||
                        _span[_position + 2] != 'l' ||
                        _span[_position + 3] != 's' ||
                        _span[_position + 4] != 'e')
                        ThrowFormatException("Expected 'false'");

                    _position += 5;
                    return false;
                }
            }

            private object? ReadNull()
            {
                if (_position + 3 >= _span.Length ||
                    _span[_position + 1] != 'u' ||
                    _span[_position + 2] != 'l' ||
                    _span[_position + 3] != 'l')
                    ThrowFormatException("Expected 'null'");

                _position += 4;
                return null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void SkipWhitespaceAndComments()
            {
                while (_position < _span.Length)
                {
                    var current = _span[_position];

                    if (char.IsWhiteSpace(current))
                    {
                        _position++;
                    }
                    else if (current == '/' && _position + 1 < _span.Length)
                    {
                        var next = _span[_position + 1];
                        if (next == '/')
                        {
                            SkipSingleLineComment();
                        }
                        else if (next == '*')
                        {
                            SkipMultiLineComment();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            private void SkipSingleLineComment()
            {
                _position += 2;

                while (_position < _span.Length && _span[_position] != '\n' && _span[_position] != '\r')
                {
                    _position++;
                }

                if (_position < _span.Length)
                {
                    if (_span[_position] == '\r')
                    {
                        _position++;
                        if (_position < _span.Length && _span[_position] == '\n')
                            _position++;
                    }
                    else if (_span[_position] == '\n')
                    {
                        _position++;
                    }
                }
            }

            private void SkipMultiLineComment()
            {
                _position += 2;

                while (_position + 1 < _span.Length)
                {
                    if (_span[_position] == '*' && _span[_position + 1] == '/')
                    {
                        _position += 2;
                        return;
                    }
                    _position++;
                }

                ThrowFormatException("Unterminated multi-line comment");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Consume(char expected)
            {
                SkipWhitespaceAndComments();

                if (_position >= _span.Length || _span[_position] != expected)
                    ThrowExpectedCharacter(expected);

                _position++;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool TryConsume(char expected)
            {
                SkipWhitespaceAndComments();

                if (_position < _span.Length && _span[_position] == expected)
                {
                    _position++;
                    return true;
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsDigit(char c) => (uint)(c - '0') <= 9;

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowFormatException(string message) => throw new FormatException(message);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static object ThrowUnexpectedComment() => throw new FormatException("Unexpected comment start");

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static object ThrowUnexpectedCharacter(char c) => throw new FormatException($"Unexpected character: {c}");

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowExpectedCharacter(char expected) => throw new FormatException($"Expected '{expected}'");

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static char ThrowInvalidEscape(char c) => throw new FormatException($"Invalid escape sequence: \\{c}");
        }
    }



    internal static class LumJsonSerializer
    {
        /// <summary>
        /// 将对象序列化为 JSON 字符串
        /// </summary>
        public static string Parse(object? value, int indent)
        {
            if(value is null)
            {
                return "{}";
            }
            if(value is string s)
            {
                return s;
            }

            if(indent<0) indent = 0;

            var writer = new JsonWriter(indent);
            writer.WriteValue(value);
            return writer.ToString();
        }

        private ref struct JsonWriter
        {
            private readonly StringBuilder _builder;
            private readonly int _indentSize;
            private int _currentIndent;

            public JsonWriter(int indentSize)
            {
                _builder = new StringBuilder(256);
                _indentSize = indentSize;
                _currentIndent = 0;
            }

            public void WriteValue(object? value)
            {
                switch (value)
                {
                    case null:
                        WriteNull();
                        break;
                    case string str:
                        WriteString(str);
                        break;
                    case bool b:
                        WriteBoolean(b);
                        break;
                    case int i:
                        WriteNumber(i);
                        break;
                    case long l:
                        WriteNumber(l);
                        break;
                    case double d:
                        WriteNumber(d);
                        break;
                    case float f:
                        WriteNumber(f);
                        break;
                    case decimal dec:
                        WriteNumber(dec);
                        break;
                    case Dictionary<string, object?> dict:
                        WriteObject(dict);
                        break;
                    case List<object?> list:
                        WriteArray(list);
                        break;
                    case object[] array:
                        WriteArray(array);
                        break;
                    case IEnumerable<object?> enumerable:
                        WriteArray(enumerable);
                        break;
                    default:
                        WriteOther(value);
                        break;
                }
            }

            private void WriteObject(Dictionary<string, object?> dict)
            {
                _builder.Append('{');
                _currentIndent++;

                bool first = true;
                foreach (var kvp in dict)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    WriteNewLine();
                    WriteIndent();
                    WriteString(kvp.Key);
                    _builder.Append(':');
                    if (_indentSize > 0) _builder.Append(' ');
                    WriteValue(kvp.Value);

                    first = false;
                }

                _currentIndent--;
                if (!first) WriteNewLine();
                if (!first) WriteIndent();
                _builder.Append('}');
            }

            private void WriteArray(IEnumerable<object?> enumerable)
            {
                _builder.Append('[');
                _currentIndent++;

                bool first = true;
                foreach (var item in enumerable)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    WriteNewLine();
                    WriteIndent();
                    WriteValue(item);

                    first = false;
                }

                _currentIndent--;
                if (!first) WriteNewLine();
                if (!first) WriteIndent();
                _builder.Append(']');
            }

            private void WriteString(string value)
            {
                _builder.Append('"');

                foreach (var c in value)
                {
                    switch (c)
                    {
                        case '"':
                            _builder.Append("\\\"");
                            break;
                        case '\\':
                            _builder.Append("\\\\");
                            break;
                        case '\b':
                            _builder.Append("\\b");
                            break;
                        case '\f':
                            _builder.Append("\\f");
                            break;
                        case '\n':
                            _builder.Append("\\n");
                            break;
                        case '\r':
                            _builder.Append("\\r");
                            break;
                        case '\t':
                            _builder.Append("\\t");
                            break;
                        default:
                            if (c < ' ')
                            {
                                _builder.Append($"\\u{(int)c:04x}");
                            }
                            else
                            {
                                _builder.Append(c);
                            }
                            break;
                    }
                }

                _builder.Append('"');
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteNumber<T>(T value) where T : IFormattable
            {
                _builder.Append(value.ToString(null, CultureInfo.InvariantCulture));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteBoolean(bool value)
            {
                _builder.Append(value ? "true" : "false");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteNull()
            {
                _builder.Append("null");
            }

            private void WriteOther(object value)
            {
                // 尝试处理其他常见类型
                switch (value)
                {
                    case DateTime dateTime:
                        WriteString(dateTime.ToString("O")); // ISO 8601
                        break;
                    case DateTimeOffset dateTimeOffset:
                        WriteString(dateTimeOffset.ToString("O")); // ISO 8601
                        break;
                    case Guid guid:
                        WriteString(guid.ToString());
                        break;
                    case IFormattable formattable:
                        WriteString(formattable.ToString(null, CultureInfo.InvariantCulture));
                        break;
                    default:
                        WriteString(value.ToString() ?? "null");
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteNewLine()
            {
                if (_indentSize > 0)
                {
                    _builder.Append('\n');
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void WriteIndent()
            {
                if (_indentSize > 0)
                {
                    _builder.Append(' ', _currentIndent * _indentSize);
                }
            }

            public override string ToString() => _builder.ToString();
        }
    }
}