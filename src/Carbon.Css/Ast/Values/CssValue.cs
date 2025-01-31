﻿using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

using Carbon.Css.Parser;

namespace Carbon.Css;

public abstract class CssValue : CssNode
{
    public CssValue(NodeKind kind)
        : base(kind)
    { }

    public static CssUnitValue Number(double value)
    {
        if (value is 0)
        {
            return CssUnitValue.Zero;
        }

        return new CssUnitValue(value, CssUnitInfo.Number);
    }

    public static CssUnitValue Em(double value)
    {
        return new CssUnitValue(value, CssUnitInfo.Em);
    }

    public static CssUnitValue Px(double value)
    {
        return new CssUnitValue(value, CssUnitInfo.Px);
    }

    public static CssValue Parse(string text)
    {
        ArgumentException.ThrowIfNullOrEmpty(text);

        if (char.IsAsciiDigit(text[0]) && TryParseNumberOrMeasurement(text, out CssUnitValue? value))
        {
            return value;
        }

        var reader = new SourceReader(new StringReader(text));

        using var tokenizer = new CssTokenizer(reader, LexicalMode.Value);

        var parser = new CssParser(tokenizer);

        return parser.ReadValueList();
    }

    // 60px
    // 6.5em

    private static bool TryParseNumberOrMeasurement(ReadOnlySpan<char> text, [NotNullWhen(true)] out CssUnitValue? value)
    {
        int unitIndex = -1;

        char point;

        for (int i = 0; i < text.Length; i++)
        {
            point = text[i];

            if (point is ' ' or ',')
            {
                value = null;
                return false;
            }

            if (char.IsAsciiDigit(point) || point is '.')
            {
            }
            else if (unitIndex is -1)
            {
                unitIndex = i;
            }
        }

        value = (unitIndex > 0)
            ? new CssUnitValue(double.Parse(text[..unitIndex], provider: CultureInfo.InvariantCulture), CssUnitNames.Get(text[unitIndex..]))
            : new CssUnitValue(double.Parse(text, provider: CultureInfo.InvariantCulture), CssUnitInfo.Number);

        return true;
    }

    public static CssValue FromComponents(IEnumerable<CssValue> components)
    {
        // A property value can have one or more components.
        // Components are seperated by a space & may include functions, literals, dimensions, etc

        var enumerator = components.GetEnumerator();

        enumerator.MoveNext();

        var first = enumerator.Current;

        if (!enumerator.MoveNext())
        {
            return first;
        }

        var values = new List<CssValue> {
            first,
            enumerator.Current
        };

        while (enumerator.MoveNext())
        {
            values.Add(enumerator.Current);
        }

        return new CssValueList(values, CssValueSeperator.Space);
    }

    public static bool AreCompatible(CssValue left, CssValue right, BinaryOperator operation)
    {
        return operation switch
        {
            BinaryOperator.Divide => false,
            BinaryOperator.Add or BinaryOperator.Subtract => left.Kind == right.Kind,
            BinaryOperator.Multiply =>
                    left.Kind == right.Kind ||
                    left.Kind is NodeKind.Percentage or NodeKind.Number ||
                    right.Kind is NodeKind.Percentage or NodeKind.Number,
            BinaryOperator.Mod => right.Kind is NodeKind.Number,
            _ => true
        };
    }

    internal virtual void WriteTo(scoped ref ValueStringBuilder sb)
    {
        sb.Append(ToString());
    }

    internal virtual void WriteTo(TextWriter writer)
    {
        writer.Write(ToString());
    }
}