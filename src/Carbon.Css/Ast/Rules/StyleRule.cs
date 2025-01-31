﻿using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Carbon.Css;

public sealed class StyleRule : CssRule
{
    public StyleRule(CssSelector selector)
    {
        Selector = selector;
    }

    public StyleRule(string selectorText)
        : this(CssSelector.Parse(selectorText)) { }

    public StyleRule(string selectorText, IReadOnlyList<CssNode> children)
        : this(CssSelector.Parse(selectorText))
    {
        foreach (var child in children)
        {
            child.Parent = this;

            base.Children.Add(child);
        }
    }

    public override RuleType Type => RuleType.Style;

    public CssSelector Selector { get; }

    public int Depth { get; set; }

    public override StyleRule CloneNode()
    {
        var clone = new StyleRule(Selector) { Depth = Depth };

        clone.Flags = Flags;

        foreach (var child in Children)
        {
            clone.Add(child.CloneNode());
        }

        return clone;
    }

    public override string ToString()
    {
        var sb = StringBuilderCache.Aquire();

        using var sw = new StringWriter(sb);

        var writer = new CssWriter(sw);

        writer.WriteStyleRule(this, 0);

        return StringBuilderCache.ExtractAndRelease(sb);
    }

    public void WriteTo(TextWriter writer)
    {
        new CssWriter(writer).WriteStyleRule(this, 0);
    }

    #region Add Helper

    public void Add(string name, string value)
    {
        _children.Add(new CssDeclaration(name, value));
    }

    #endregion
}