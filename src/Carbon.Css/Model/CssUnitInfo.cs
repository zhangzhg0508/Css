﻿using System.Runtime.CompilerServices;
using System.Text;

namespace Carbon.Css;

public sealed class CssUnitInfo : IEquatable<CssUnitInfo>
{
    public static readonly CssUnitInfo Number = new (string.Empty, NodeKind.Number);

    // <length> (Relative) | em, ex, cm, ch, rem, vh, vw, vmin, vmax	                                         | relative to
    public static readonly CssUnitInfo Em    = new(CssUnitNames.Em,   NodeKind.Length, CssUnitFlags.Relative); // | font size of the element
    public static readonly CssUnitInfo Ex    = new(CssUnitNames.Ex,   NodeKind.Length, CssUnitFlags.Relative); // | x-height of the element’s font
    public static readonly CssUnitInfo Cap   = new("cap",             NodeKind.Length, CssUnitFlags.Relative); // | cap height (the nominal height of capital letters) of the element’s font
    public static readonly CssUnitInfo Ch    = new("ch",              NodeKind.Length, CssUnitFlags.Relative); // | average character advance of a narrow glyph in the element’s font, as represented by the “0” (ZERO, U+0030) glyph
    public static readonly CssUnitInfo Ic    = new("ic",              NodeKind.Length, CssUnitFlags.Relative); // | average character advance of a fullwidth glyph in the element’s font, as represented by the “水” (CJK water ideograph, U+6C34) glyph
    public static readonly CssUnitInfo Rem   = new(CssUnitNames.Rem,  NodeKind.Length, CssUnitFlags.Relative); // | font size of the root element
    public static readonly CssUnitInfo Lh    = new(CssUnitNames.Lh,   NodeKind.Length, CssUnitFlags.Relative); // | line height of the element
    public static readonly CssUnitInfo Rlh   = new(CssUnitNames.Rlh,  NodeKind.Length, CssUnitFlags.Relative); // | line height of the root element
    public static readonly CssUnitInfo Vw    = new(CssUnitNames.Vw,   NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s width
    public static readonly CssUnitInfo Vh    = new(CssUnitNames.Vh,   NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s height
    public static readonly CssUnitInfo Vi    = new(CssUnitNames.Vi,   NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s size in the root element’s inline axis
    public static readonly CssUnitInfo Vb    = new(CssUnitNames.Vb,   NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s size in the root element’s block axis
    public static readonly CssUnitInfo Vmin  = new(CssUnitNames.Vmin, NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s smaller dimension
    public static readonly CssUnitInfo Vmax  = new(CssUnitNames.Vmax, NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s larger dimension
                                             
    public static readonly CssUnitInfo Cqw   = new(CssUnitNames.Cqw,   NodeKind.Length, CssUnitFlags.Relative); // 1% of a query container's width
    public static readonly CssUnitInfo Cqh   = new(CssUnitNames.Cqh,   NodeKind.Length, CssUnitFlags.Relative); // 1% of a query container's height
    public static readonly CssUnitInfo Cqi   = new(CssUnitNames.Cqi,   NodeKind.Length, CssUnitFlags.Relative); // 1% of a query container's inline size
    public static readonly CssUnitInfo Cqb   = new(CssUnitNames.Cqb,   NodeKind.Length, CssUnitFlags.Relative); // 1% of a query container's block size
    public static readonly CssUnitInfo Cqmin = new(CssUnitNames.Cqmin, NodeKind.Length, CssUnitFlags.Relative); // The smaller value of either cqi or cqb
    public static readonly CssUnitInfo Cqmax = new(CssUnitNames.Cqmax, NodeKind.Length, CssUnitFlags.Relative); // The larger value of either cqi or cqb

    public static readonly CssUnitInfo Dvh   = new(CssUnitNames.Dvh,  NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s dynamic height
    public static readonly CssUnitInfo Dvw   = new(CssUnitNames.Dvw,  NodeKind.Length, CssUnitFlags.Relative); // | 1% of viewport’s dynamic width

    // <length> | px, mm, cm, in, pt, pc
    public static readonly CssUnitInfo Cm    = new("cm",            NodeKind.Length); // centimeters	        1cm = 96px/2.54
    public static readonly CssUnitInfo Mm    = new("mm",            NodeKind.Length); // millimeters	        1mm = 1/10th of 1cm
    public static readonly CssUnitInfo Q     = new("q",             NodeKind.Length); // quarter-millimeters	1Q = 1/40th of 1cm
    public static readonly CssUnitInfo In    = new("in",            NodeKind.Length); // inches	                1in = 2.54cm = 96px
    public static readonly CssUnitInfo Pc    = new("pc",            NodeKind.Length); // picas	                1pc = 1/6th of 1in
    public static readonly CssUnitInfo Pt    = new("pt",            NodeKind.Length); // points	                1pt = 1/72th of 1in
    public static readonly CssUnitInfo Px    = new(CssUnitNames.Px, NodeKind.Length); // pixels	                1px = 1/96th of 1in

    // <percentages> | %
    public static readonly CssUnitInfo Percentage = new ("%", NodeKind.Percentage);

    // <angle> deg, grad, rad, turn
    public static readonly CssUnitInfo Deg  = new(CssUnitNames.Deg,  NodeKind.Angle);
    public static readonly CssUnitInfo Grad = new(CssUnitNames.Grad, NodeKind.Angle);
    public static readonly CssUnitInfo Rad  = new(CssUnitNames.Rad,  NodeKind.Angle);
    public static readonly CssUnitInfo Turn = new(CssUnitNames.Turn, NodeKind.Angle);

    // <time> | s, ms
    public static readonly CssUnitInfo S    = new(CssUnitNames.S,  NodeKind.Time);
    public static readonly CssUnitInfo Ms   = new(CssUnitNames.Ms, NodeKind.Time);

    // <frequency> | Hz, kHz
    public static readonly CssUnitInfo Hz   = new(CssUnitNames.Hz, NodeKind.Frequency);
    public static readonly CssUnitInfo Khz  = new("khz",           NodeKind.Frequency);

    // <resolution> | dpi, dpcm, dppx, x
    public static readonly CssUnitInfo Dpi  = new("dpi",          NodeKind.Resolution);
    public static readonly CssUnitInfo Dpcm = new("dpcm",         NodeKind.Resolution);
    public static readonly CssUnitInfo Dppx = new("dppx",         NodeKind.Resolution);
    public static readonly CssUnitInfo X    = new(CssUnitNames.X, NodeKind.Resolution);

    public static readonly Dictionary<string, CssUnitInfo> items = new () {
        // <length> : relative
        { CssUnitNames.Em    , Em },
        { CssUnitNames.Ex    , Ex },
        { "cap"              , Cap },
        { "ch"               , Ch },
        { "ic"               , Ic },
        { CssUnitNames.Rem   , Rem },
        { CssUnitNames.Lh    , Lh },
        { CssUnitNames.Rlh   , Rlh },
        { CssUnitNames.Cqw   , Cqw },
        { CssUnitNames.Cqh   , Cqh },
        { CssUnitNames.Cqi   , Cqi },
        { CssUnitNames.Cqb   , Cqb },
        { CssUnitNames.Cqmin , Cqmin },
        { CssUnitNames.Cqmax , Cqmax },
        { CssUnitNames.Dvw   , Dvw },
        { CssUnitNames.Dvh   , Dvh },
        { CssUnitNames.Vw    , Vw },
        { CssUnitNames.Vh    , Vh },
        { CssUnitNames.Vi    , Vi },
        { CssUnitNames.Vb    , Vb },
        { CssUnitNames.Vmin  , Vmin },
        { CssUnitNames.Vmax  , Vmax },

        // <length> 
        { "cm"              , Cm },
        { "mm"              , Mm },
        { "q"               , Q },
        { "in"              , In },
        { "pc"              , Pc },
        { "pt"              , Pt },
        { CssUnitNames.Px   , Px },
            
        // <percentage>
        { CssUnitNames.Percent, Percentage },

        // <angle>
        { CssUnitNames.Deg  , Deg },
        { CssUnitNames.Grad , Grad },
        { CssUnitNames.Rad  , Rad },
        { CssUnitNames.Turn , Turn },

        // <time>
        { CssUnitNames.S    , S },
        { CssUnitNames.Ms   , Ms },

        // <frequency>
        { CssUnitNames.Hz   , Hz },
        { "kHz"             , Khz },

        // <resolution>
        { "dpi"             , Dpi },
        { "dpcm"            , Dpcm },
        { "dppx"            , Dppx },
        { CssUnitNames.X    , X },
    };

    internal CssUnitInfo(string name, NodeKind kind, CssUnitFlags flags = default)
    {
        Name = name;
        Kind = kind;
        Flags = flags;
    }

    public string Name { get; }

    public NodeKind Kind { get; }

    public CssUnitFlags Flags { get; }

    [SkipLocalsInit]
    public static CssUnitInfo Get(ReadOnlySpan<byte> utf8Bytes)
    {
        if (utf8Bytes.Length > CssUnitNames.MaxLength)
        {
            return new CssUnitInfo(Encoding.UTF8.GetString(utf8Bytes), NodeKind.Unknown);
        }

        Span<char> buffer = stackalloc char[4];

        var chars = buffer[0..Encoding.ASCII.GetChars(utf8Bytes, buffer)];

        return Get(chars);
    }

    public static CssUnitInfo Get(ReadOnlySpan<char> name)
    {
        if (name.Length is 1)
        {
            switch (name[0])
            {
                case '%': return Percentage;
                case 's': return S;
                case 'x': return X;
            }
        }
        else if (name.Length is 2)
        {
            switch ((name[0], name[1]))
            {
                case ('p', 'x'): return Px;
                case ('e', 'm'): return Em;
                case ('v', 'h'): return Vh;
                case ('v', 'w'): return Vw;
            }
        }
        else if (name.Length is 3)
        {
            switch (name)
            {
                case "cqb": return Cqb;
                case "cqh": return Cqh;
                case "cqi": return Cqi;
                case "cqw": return Cqw;
                case "deg": return Deg;
                case "dvh": return Dvh;
                case "dvw": return Dvw;
                case "rem": return Rem;
                case "rlh": return Rlh;
            }
        }
        else if (name.Length is 5)
        {
            switch (name)
            {
                case "cqmax": return Cqmin;
                case "cqmin": return Cqmax;
            }
        }

        string text = name.ToString();

        if (items.TryGetValue(text, out var unit))
        {
            return unit;
        }

        return new CssUnitInfo(text, NodeKind.Unknown);
    }

    public bool Equals(CssUnitInfo? other)
    {
        if (other is null) return this is null;

        return ReferenceEquals(this, other) || Name.Equals(other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return obj is CssUnitInfo other && Equals(other);
    }

    public override int GetHashCode() => Name.GetHashCode();
}   

// PERF notes: A dictionary lookup is ~1.67x faster than a large switch statement