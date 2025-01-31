﻿using System.IO;

namespace Carbon.Css;

public sealed class ImportRule : CssRule
{
    public ImportRule(CssUrlValue url)
    {
        Url = url;
    }

    public override RuleType Type => RuleType.Import;

    public CssUrlValue Url { get; }

    public void WriteTo(TextWriter writer)
    {
        writer.Write("@import ");

        Url.WriteTo(writer);

        writer.Write(';');
    }

    public override string ToString()
    {
        using var writer = new StringWriter();

        WriteTo(writer);

        return writer.ToString();
    }
}

/*
6.3 The @import rule

The '@import' rule allows users to import style rules from other style sheets. 
In CSS 2.1, any @import rules must precede all other rules (except the @charset rule, if present).
See the section on parsing for when user agents must ignore @import rules. 
The '@import' keyword must be followed by the URI of the style sheet to include. 
A string is also allowed; it will be interpreted as if it had url(...) around it.

The following lines are equivalent in meaning and illustrate both '@import' syntaxes (one with "url()" and one with a bare string):

@import "mystyle.css";
@import url("mystyle.css");
*/
