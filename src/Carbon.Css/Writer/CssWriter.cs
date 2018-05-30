﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Carbon.Css
{
    using Parser;

    public class CssWriter : IDisposable
    {
        private readonly TextWriter writer;
        private readonly CssContext context;
        private readonly ICssResolver resolver;
        private int includeCount = 0;
        private int importCount = 0;
        private int nodeCount = 0;

        private BrowserInfo[] browserSupport;

        private CssScope scope;

        public CssWriter(TextWriter writer, CssContext context = null, CssScope scope = null, ICssResolver resolver = null)
        {
            this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
            this.context = context ?? new CssContext();
            this.resolver = resolver;
            this.scope = scope ?? new CssScope();

            this.browserSupport = this.context.BrowserSupport;
        }

        public void WriteRoot(StyleSheet sheet)
        {
            importCount++;

            if (importCount > 200)
            {
                throw new Exception("Exceded importCount of 200");
            }

            var i = 0;

            foreach (var node in sheet.Children)
            {
                if (node.Kind == NodeKind.If)
                {
                    EvaluateIf((IfBlock)node);
                }
                else if (node.Kind == NodeKind.For)
                {
                    EvaluateFor((ForBlock)node);
                }
                else if (node.Kind == NodeKind.Comment)
                {
                    if (i != 0) writer.WriteLine();

                    i++;

                    WriteComment((CssComment)node);
                }

                else if (node.Kind == NodeKind.Assignment)
                {
                    var variable = (CssAssignment)node;

                    scope[variable.Name] = variable.Value;
                }
                else if (node is CssRule rule)
                {
                    if (i != 0) writer.WriteLine();

                    i++;

                    if (rule.Type == RuleType.Import)
                    {
                        var importRule = (ImportRule)rule;

                        if (!importRule.Url.IsPath || resolver == null)
                        {
                            WriteImportRule(importRule);
                        }
                        else
                        {
                            InlineImport(importRule, sheet);
                        }
                    }
                    else
                    {
                        WriteRule(rule);
                    }
                }
            }
        }

        #region Expressions

        public void EvaluateIf(IfBlock block, int level = 0)
        {
            CssValue result = EvalulateExpression(block.Condition);

            if (ToBoolean(result))
            {
                WriteBlockBody(block, level);
            }
        }

        public void EvaluateFor(ForBlock block, int level = 0)
        {
            int start = (int)((CssUnitValue)EvalulateExpression(block.Start)).Value;
            int end = (int)((CssUnitValue)EvalulateExpression(block.End)).Value;

            if (end < start)
            {
                throw new Exception("end must be after the start");
            }

            if (end - start > 10000)
            {
                throw new Exception("Must be less than 10,000");
            }

            scope = scope.GetChildScope();

            int a = 0;

            for (int i = start; i <= end; i++)
            {
                if (a > 0) writer.WriteLine();

                scope[block.Variable.Symbol] = new CssUnitValue(i, default);

                WriteBlockBody(block, level);

                a++;
            }

            scope = scope.Parent;
        }

        private void WriteBlockBody(CssBlock block, int level = 0)
        {
            int i = 0;

            foreach (CssNode child in block.Children)
            {
                if (child is CssRule rule)
                {
                    if (i > 0) writer.WriteLine();

                    WriteRule(rule);

                    i++;
                }
                else if (child is CssAssignment assignment)
                {
                    scope[assignment.Name] = assignment.Value;
                }
                else if (child is CssDeclaration declaration)
                {
                    if (i > 0) writer.WriteLine();

                    WriteDeclaration(declaration, level);

                    i++;
                }
            }
        }


        public bool ToBoolean(object value) => (value is CssBoolean b) ? b.Value : false;

        public CssValue EvalulateExpression(CssValue expression)
        {
            switch (expression.Kind)
            {
                case NodeKind.Variable   : return scope.GetValue(((CssVariable)expression).Symbol);
                case NodeKind.Expression : return EvalBinaryExpression((BinaryExpression)expression);
                case NodeKind.Function   : return EvalFunction((CssFunction)expression);
                default                  : return expression;
            }
        }

        public CssValue EvalBinaryExpression(BinaryExpression expression)
        {
            var left = EvalulateExpression(expression.Left);
            var right = EvalulateExpression(expression.Right);

            switch (expression.Operator)
            {
                case BinaryOperator.Multiply : return ((CssUnitValue)expression.Left).Multiply(expression.Right);
                case BinaryOperator.Add      : return ((CssUnitValue)expression.Left).Add(expression.Right);
            }

            var leftS = left.ToString();
            var rightS = right.ToString();

            if (left.Kind == NodeKind.Undefined)
            {
                leftS = "undefined";
            }

            switch (expression.Operator)
            {
                case BinaryOperator.Equals    : return new CssBoolean(leftS == rightS);
                case BinaryOperator.NotEquals : return new CssBoolean(leftS != rightS);
                case BinaryOperator.Gt        : return new CssBoolean(float.Parse(leftS) >  float.Parse(rightS));
                case BinaryOperator.Gte       : return new CssBoolean(float.Parse(leftS) >= float.Parse(rightS));
                case BinaryOperator.Lt        : return new CssBoolean(float.Parse(leftS) <  float.Parse(rightS));
                case BinaryOperator.Lte       : return new CssBoolean(float.Parse(leftS) <= float.Parse(rightS));
            }

            return new CssBoolean(true);
        }

        public CssValue EvalFunction(CssFunction function)
        {
            if (CssFunctions.TryGet(function.Name, out var func))
            {
                var args = GetArgs(function.Arguments).ToArray();

                return func(args);
            }

            throw new Exception($"function named '{function.Name}' not registered");
        }

        #endregion

        public void InlineImport(ImportRule importRule, StyleSheet sheet)
        {
            // var relativePath = importRule.Url;
            var absolutePath = importRule.Url.GetAbsolutePath(resolver.ScopedPath);

            if (absolutePath[0] == '/')
            {
                absolutePath = absolutePath.Substring(1);
            }

            // Assume to be scss if there is no extension
            if (!absolutePath.Contains('.'))
            {
                absolutePath += ".scss";
            }

            writer.WriteLine();
            writer.WriteLine("/* " + absolutePath + " */");

            var stream = resolver.Open(absolutePath);

            if (stream == null)
            { 
                writer.WriteLine("/* NOT FOUND */");

                return;
            }

            if (absolutePath.EndsWith(".scss"))
            {
                try
                {
                    var css = StyleSheet.Parse(stream, context);

                    WriteRoot(css);
                }
                catch (SyntaxException ex)
                {
                    writer.WriteLine("body, html { background-color: red !important; }");
                    writer.WriteLine("body * { display: none; }");

                    writer.WriteLine($"/* --- Parse Error in '{absolutePath}' : {ex.Message} ");

                    if (ex.Lines != null)
                    {
                        foreach (var line in ex.Lines)
                        {
                            writer.Write(line.Number.ToString().PadLeft(4)); // 1
                            writer.Write(". ");

                            if (line.Number == ex.Location.Line)
                            {
                                writer.Write("* ");
                            }

                            writer.WriteLine(line.Text);
                        }
                    }

                    writer.Write("*/");

                    return;
                }
            }
            else
            {
                // Copy the file line by line...

                using (var reader = new StreamReader(stream))
                {
                    string line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        writer.WriteLine(line);
                    }
                }
            }
        }

        public void WriteComment(CssComment comment)
        {
            writer.Write("/* ");
            writer.Write(comment.Text);
            writer.WriteLine(" */");
        }

        public void WriteValue(CssNode value)
        {
            if (nodeCount > 50000) throw new Exception("Exceded limit of 50,000 nodes");

            nodeCount++;

            switch (value.Kind)
            {
                case NodeKind.Variable           : WriteVariable((CssVariable)value); break;
                case NodeKind.ValueList          : WriteValueList((CssValueList)value); break;
                case NodeKind.Function           : WriteFunction((CssFunction)value); break;
                case NodeKind.Expression         : WriteValue(EvalulateExpression((CssValue)value)); break;
                case NodeKind.InterpolatedString : WriteInterpolatedString((CssInterpolatedString)value); break;
                default                          : writer.Write(value.ToString()); break;
            }
        }

        public void WriteValueList(CssValueList list)
        {
            var i = 0;

            foreach (CssValue value in list)
            {
                if (i != 0)
                {

                    if (list[i - 1] is CssInterpolatedString last)
                    {
                        WriteTrivia(last.Trailing);
                    }

                    else if (list.Seperator == ValueSeperator.Space)
                    {
                        writer.Write(' ');
                    }
                    else
                    {
                        writer.Write(", ");
                    }
                }

                WriteValue(value);

                i++;
            }
        }

        private void WriteTrivia(Trivia trivia)
        {
            if (trivia == null) return;

            foreach (var token in trivia)
            {
                writer.Write(token.Text);
            }
        }

        public void WriteFunction(CssFunction function)
        {
            // {name}({args})

            if (CssFunctions.TryGet(function.Name, out var func))
            {
                var args = GetArgs(function.Arguments).ToArray();

                writer.Write(func(args));

                return;
            }

            writer.Write(function.Name);

            writer.Write('(');

            WriteValue(function.Arguments);

            writer.Write(')');
        }

        public IEnumerable<CssValue> GetArgs(CssValue value)
        {
            switch (value.Kind)
            {
                case NodeKind.Variable:
                    yield return scope.GetValue(((CssVariable)value).Symbol);

                    break;

                case NodeKind.ValueList:
                    var list = (CssValueList)value;

                    if (list.Seperator == ValueSeperator.Space) yield return list;

                    // Break out comma seperated values
                    foreach (var v in list)
                    {
                        foreach (var item in GetArgs(v))
                        {
                            yield return item;
                        }
                    }

                    break;

                case NodeKind.Expression:
                    yield return EvalBinaryExpression((BinaryExpression)value);

                    break;

                // Function, etc 
                default:
                    yield return value;
                    break;
            }

        }

        public void WriteVariable(CssVariable variable)
        {
            var value = scope.GetValue(variable.Symbol);

            WriteValue(value);
        }

        public void WriteInterpolatedString(CssInterpolatedString node)
        {
            WriteValue(node.Expression);
        }

        public void WriteImportRule(ImportRule rule)
        {
            // TODO: normalize value
            writer.Write(rule.ToString());
        }

        public void WriteRule(CssRule rule, int level = 0)
        {
            var i = 0;

            foreach (var r in Rewrite(rule))
            {
                if (i != 0) writer.WriteLine();

                _WriteRule(r, level);

                i++;
            }
        }

        public void _WriteRule(CssRule rule, int level = 0)
        {
            Indent(level);

            switch (rule)
            {
                case ImportRule importRule      : WriteImportRule(importRule);             break;
                case MediaRule mediaRule        : WriteMediaRule(mediaRule, level);        break;
                case StyleRule styleRule        : WriteStyleRule(styleRule, level);        break;
                case FontFaceRule fontFaceRule  : WriteFontFaceRule(fontFaceRule, level);  break;
                case KeyframesRule keyFrameRule : WriteKeyframesRule(keyFrameRule, level); break;
                case UnknownRule atRule         : WriteAtRule(atRule, level);              break;

                default: throw new Exception("Unhandled rule:" + rule.GetType().Name);
            }
        }

        public void WriteAtRule(UnknownRule rule, int level)
        {
            writer.Write('@');
            writer.Write(rule.Name);

            if (rule.Text != null)
            {
                writer.Write(' ');
                rule.Text.WriteTo(writer);
            }

            writer.Write(' ');

            WriteBlock(rule, level);
        }

        public void WriteStyleRule(StyleRule rule, int level)
        {
            WriteSelector(rule.Selector);

            writer.Write(' ');

            WriteBlock(rule, level);
        }

        public void WriteSelectorList(CssSelectorList selector)
        {  
            for (int i = 0; i < selector.Count; i++)
            {
                if (i != 0)
                {
                    writer.WriteLine(",");
                }

                WriteSelector(selector[i]);
            }
        }

        public void WriteSelector(in CssSelector selector)
        {
            for (int i = 0; i < selector.Count; i++)
            {
                if (i != 0)
                {
                    writer.WriteLine(",");
                }

                selector[i].WriteTo(writer);
            }
        }

        public void WriteMediaRule(MediaRule rule, int level)
        {
            writer.Write("@media ");

            rule.Text.WriteTo(writer);
            writer.Write(' ');
            WriteBlock(rule, level);
        }

        public void WriteFontFaceRule(FontFaceRule rule, int level)
        {
            writer.Write("@font-face "); // Write selector

            WriteBlock(rule, level);
        }

        public void WriteKeyframesRule(KeyframesRule rule, int level)
        {
            if (context.BrowserSupport != null)
            {
                // -moz-
                if (context.Compatibility.Firefox > 0 && context.Compatibility.Firefox < 16)
                {
                    WriteKeyframesRule(BrowserInfo.Firefox(context.Compatibility.Firefox), rule, level);

                    writer.WriteLine();
                }

                // -webkit- 
                if (context.Compatibility.Safari > 0 && context.Compatibility.Safari < 9)
                {
                    WriteKeyframesRule(BrowserInfo.Safari(context.Compatibility.Safari), rule, level);

                    writer.WriteLine();
                }
            }

            writer.Write("@keyframes ");
            writer.Write(rule.Name);
            writer.Write(' ');

            browserSupport = null;

            WriteBlock(rule, level); // super standards

            browserSupport = context.BrowserSupport;
        }

        private void WriteKeyframesRule(BrowserInfo browser, KeyframesRule rule, int level)
        {
            browserSupport = new[] { browser };

            writer.Write('@');
            writer.Write(browser.Prefix.Text);
            writer.Write("keyframes ");
            writer.Write(rule.Name);
            writer.Write(' ');

            WriteBlock(rule, level);

            browserSupport = context.BrowserSupport;
        }

        public void WriteBlock(CssBlock block, int level)
        {
            var prevScope = scope;

            writer.Write('{'); // Block start

            var condenced = false;
            var count = 0;

            // Write the declarations
            foreach (var node in block.Children) // TODO: Change to an immutable list?
            {
                if (node.Kind == NodeKind.Include)
                {
                    var b2 = new CssBlock(NodeKind.Block) {
                        node
                    };

                    scope = ExpandInclude((IncludeNode)node, b2);

                    foreach (var rule in b2.OfType<CssRule>())
                    {
                        writer.WriteLine();

                        WriteRule(rule, level + 1);

                        count++;
                    }
                }
                else if (node.Kind == NodeKind.Declaration)
                {
                    var declaration = (CssDeclaration)node;

                    if (block.Children.Count == 1 && !declaration.Info.NeedsExpansion(declaration, browserSupport))
                    {
                        condenced = true;

                        writer.Write(' ');

                        WriteDeclaration(declaration, 0);
                    }
                    else
                    {
                        if (count == 0) writer.WriteLine();

                        WritePatchedDeclaration(declaration, level + 1);
                    }
                }
                else if (node.Kind == NodeKind.Rule)  // Nested rule
                {
                    if (count == 0) writer.WriteLine();

                    var childRule = (CssRule)node;

                    WriteRule(childRule, level + 1);
                }
                else if (node.Kind == NodeKind.If)
                {
                    EvaluateIf((IfBlock)node, level + 1);
                }
                else if (node.Kind == NodeKind.For)
                {
                }

                if (!condenced)
                {
                    writer.WriteLine();
                }

                count++;
            }

            // Limit to declaration
            if (condenced)
            {
                writer.Write(' ');
            }
            else
            {
                Indent(level);
            }

            writer.Write('}'); // Block end

            prevScope = scope;
        }

        public void WriteDeclaration(CssDeclaration declaration, int level)
        {
            Indent(level);

            writer.Write(declaration.Name);
            writer.Write(": ");
            WriteValue(declaration.Value);
            writer.Write(';');
        }

        public void WritePatchedDeclaration(CssDeclaration declaration, int level)
        {
            var prop = declaration.Info;

            if (browserSupport != null && prop.Compatibility.HasPatches)
            {
                var prefixes = BrowserPrefixKind.None;

                for (int i = 0; i < browserSupport.Length; i++)
                {
                    ref BrowserInfo browser = ref browserSupport[i];

                    // Skip the prefix if we've already added it
                    if (prefixes.HasFlag(browser.Prefix.Kind)) continue;

                    if (!prop.Compatibility.HasPatch(declaration, browser)) continue;

                    CssPatch patch = prop.Compatibility.GetPatch(declaration, browser);

                    Indent(level);

                    writer.Write(patch.Name);
                    writer.Write(": ");
                    WriteValue(patch.Value);
                    writer.Write(';');

                    writer.WriteLine();

                    prefixes |= browser.Prefix.Kind;
                }
            }

            // Finally, write the standards declaration

            WriteDeclaration(declaration, level);
        }

        #region Helpers

        public void Indent(int level)
        {
            // Indent two characters for each level
            for (int i = 0; i < level; i++)
            {
                writer.Write("  ");
            }
        }

        #endregion

        #region Sass

        public IEnumerable<CssRule> Rewrite(CssRule rule)
        {
            var styleRule = rule as StyleRule;

            if (styleRule == null || rule.All(r => r.Kind == NodeKind.Declaration))
            {
                yield return rule;

                yield break;
            }

            // Figure out how to eliminate this clone

            var clone = (StyleRule)rule.CloneNode();

            // Expand includes
            foreach (var includeNode in clone.Children.OfType<IncludeNode>().ToArray())
            {
                scope = ExpandInclude(includeNode, clone);

                clone.Children.Remove(includeNode);
            }

            var root = new List<CssRule>
            {
                clone
            };

            foreach (var nestedRule in clone.Children.OfType<StyleRule>().ToArray())
            {
                foreach (var r in ExpandStyleRule(nestedRule, parent: clone))
                {
                    root.Add(r);
                }
            }

            foreach (var r in root)
            {
                if (r.HasChildren) yield return r;
            }
        }

        public IEnumerable<CssRule> ExpandStyleRule(StyleRule rule, CssRule parent)
        {
            var newRule = new StyleRule(ExpandSelector(rule));

            foreach (var childNode in rule.Children.ToArray())
            {
                if (childNode is StyleRule childRule)
                {
                    foreach (var r in ExpandStyleRule(childRule, rule))
                    {
                        yield return r;
                    }
                }
                else
                {
                    newRule.Add(childNode);
                }
            }

            parent.Remove(rule); // Remove from parent node after it's been processed

            if (newRule.HasChildren) yield return newRule;
        }

        public CssScope ExpandInclude(IncludeNode include, CssBlock rule)
        {
            includeCount++;

            if (includeCount > 1000)
            {
                throw new Exception("Exceded include limit of 1,000");
            }

            if (!context.Mixins.TryGetValue(include.Name, out MixinNode mixin))
            {
                throw new Exception($"Mixin '{include.Name}' not registered");
            }

            var index = rule.Children.IndexOf(include);

            var childScope = GetScope(mixin.Parameters, include.Args);

            var i = 0;

            foreach (var node in mixin.Children.ToArray())
            {
                // Bind variables

                if (node is IncludeNode includeNode)
                {
                    ExpandInclude(includeNode, rule);

                    mixin.Children.Remove(node);
                }

                rule.Insert(i + 1, node.CloneNode());

                i++;
            }

            return childScope;
        }

        public CssScope GetScope(IReadOnlyList<CssParameter> paramaters, CssValue args)
        {
            CssValue[] list = null;

            if (args != null)
            {
                if (args is CssValueList valueList)
                {
                    if (valueList.Seperator == ValueSeperator.Comma)
                    {
                        list = valueList.OfType<CssValue>().ToArray();
                    }
                }
                else
                {
                    list = new[] { args }; // Single Value
                }
            }

            var child = scope.GetChildScope();

            var i = 0;

            foreach (CssParameter p in paramaters)
            {
                var val = (list != null && list.Length >= i + 1) ? list[i] : p.DefaultValue;

                child.Add(p.Name, val);

                i++;
            }

            return child;
        }

        #endregion

        #region Selector Expansion

        public static CssSelector ExpandSelector(StyleRule rule)
        {
            var ancestors = new List<CssSelector>();

            ancestors.Add(rule.Selector);

            StyleRule current = rule;

            while ((current = current.Parent as StyleRule) != null)
            {
                ancestors.Add(current.Selector);

                if (ancestors.Count > 6)
                {
                    var debugParts = string.Join(" ", ancestors);

                    throw new Exception($"May not nest more than 6 levels deep. Was {debugParts}.");
                }
            }

            ancestors.Reverse();

            int i = 0;

            var result = new List<TokenList>();

            // { &.open { } }

            var span = new TokenList();

            foreach (var ancestor in ancestors)
            {
                if (ancestor.Contains(TokenKind.Ampersand))
                {
                    var prev = span.Clone();

                    span.Clear();

                    foreach (var token in ancestor[0])
                    {
                        if (token.Kind == TokenKind.Ampersand)
                        {
                            for (var ti = 0; ti < prev.Count; ti++)
                            {
                                var p = prev[ti];

                                // skip leading trivia on the last ancestor
                                if (ti + 1 == prev.Count && p.IsTrivia) continue;

                                span.Add(p);
                            }
                        }
                        else
                        {
                            span.Add(token);
                        }
                    }
                    
                    i++;

                    continue;
                }

                i++;

                // h1, h2, h3
               
                // Only works one level deep?

                if (ancestor.Count > 1)
                {
                    var parentSelector = span.Clone();

                    var c = GetSelector(ancestors.Skip(i));

                    for (int selectorIndex = 0; selectorIndex < ancestor.Count; selectorIndex++)
                    {
                        var s = ancestor[selectorIndex];

                        span = new TokenList();

                        if (parentSelector.Count > 0)
                        {
                            span.AddRange(parentSelector);

                            if (!parentSelector[parentSelector.Count - 1].IsTrivia)
                            {
                                span.Add(new CssToken(TokenKind.Whitespace, ' ', 0));
                            }
                        }

                        span.AddRange(ancestor[selectorIndex]);

                        if (!s[s.Count - 1].IsTrivia)
                        {
                            span.Add(new CssToken(TokenKind.Whitespace, ' ', 0));
                        }

                        // Remaining selectors

                        span.AddRange(c);

                        result.Add(span);
                    }

                    span = null;

                    break;
                }
                else
                {
                    foreach (var token in ancestor[0])
                    {
                        span.Add(token);
                    }
                }
            }

            if (span != null)
            {
                result.Add(span);
            }

            return new CssSelector(result);
        }

        private static TokenList GetSelector(IEnumerable<CssSelector> selectors)
        {
            var tokens = new TokenList();

            foreach (var selector in selectors)
            {
                foreach (var list in selector)
                {
                    tokens.AddRange(list);
                }
            }

            return tokens;
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        #endregion
    }
}
 