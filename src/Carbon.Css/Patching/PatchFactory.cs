﻿namespace Carbon.Css;

internal static class PatchFactory
{
    public static readonly PrefixNamePatcher PrefixName                 = new ();
    public static readonly PrefixNameAndValuePatcher PrefixNameAndValue = new ();

    // transform 0.04s linear, opacity 0.04s linear, visibility 0.04s linear;
    // -webkit-transform 0.04s linear, opacity 0.04s linear, visibility 0.04s linear;

    public static CssValue PatchValue(CssValue value, in BrowserInfo browser)
    {
        if (value.Kind != NodeKind.ValueList) return value;

        var valueList = (CssValueList)value;

        var list = new List<CssValue>();

        foreach (var node in valueList)
        {
            if (node.Kind is NodeKind.ValueList) // For comma seperated componented lists
            {
                list.Add(PatchValue(node, browser));
            }
            else if (node.Kind is NodeKind.String && ((CssString)node).Text is "transform")
            {
                list.Add(new CssString(browser.Prefix.Text + "transform"));
            }
            else
            {
                list.Add(node);
            }
        }

        return new CssValueList(list, valueList.Seperator);
    }
}
