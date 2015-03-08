﻿namespace Carbon.Css
{
	using Carbon.Css.Color;
	using Carbon.Css.Parser;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	public class CssWriter
	{
		private readonly TextWriter writer;
		private readonly CssContext context;
		private readonly ICssResolver resolver;
		private int includeCount = 0;
		private int importCount = 0;

		private CssContext currentContext;

		public CssWriter(TextWriter writer, CssContext context = null, ICssResolver resolver = null)
		{
			this.writer = writer;
			this.context = context ?? new CssContext();
			this.resolver = resolver;

			this.currentContext = this.context;
		}

		public void WriteRoot(StyleSheet sheet)
		{
			importCount++;

			if (importCount > 100) throw new Exception("Exceded importCount of 100");

			var i = 0;

			foreach (var child in sheet.Children)
			{
				var rule = child as CssRule;

				if (rule == null) continue;

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


		public void InlineImport(ImportRule importRule, StyleSheet sheet)
		{
			// var relativePath = importRule.Url;
			var absolutePath = importRule.Url.GetAbsolutePath(resolver.ScopedPath);

			// Assume to be scss if there is no extension
			if (!absolutePath.Contains('.')) absolutePath += ".scss";

			writer.Write(Environment.NewLine + "/* " + absolutePath.TrimStart('/') + " */" + Environment.NewLine);

			var text = resolver.GetText(absolutePath.TrimStart('/'));

			if (text != null)
			{
				if (Path.GetExtension(absolutePath) == ".scss")
				{
					try
					{
						var css = StyleSheet.Parse(text, context);

						WriteRoot(css);
					}
					catch (ParseException ex)
					{
						// response.StatusCode = 500;

						writer.WriteLine("body, html { background-color: red !important; }");
						writer.WriteLine("body * { display: none; }");

						writer.WriteLine(string.Format("/* --- Parse Error in '{0}':{1} ", absolutePath, ex.Message));

						if (ex.Lines != null)
						{
							foreach (var line in ex.Lines)
							{
								writer.Write(string.Format("{0}. ", line.Number.ToString().PadLeft(5)));

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
					writer.Write(text);
				}
			}
			else
			{
				writer.Write("/* NOT FOUND */" + Environment.NewLine);
			}
		}
		public void WriteValue(CssNode value)
		{
			switch(value.Kind)
			{
				case NodeKind.Variable	: WriteVariable((CssVariable)value);	break;
				case NodeKind.ValueList	: WriteValueList((CssValueList)value);	break;
				case NodeKind.Function	: WriteFunction((CssFunction)value);	break;
				default					: writer.Write(value.Text);				break;
			}
		}

		public void WriteValueList(CssValueList list)
		{
			var i = 0;

			foreach (var value in list.Children)
			{
				if (i != 0)
				{
					writer.Write(list.Seperator == ValueListSeperator.Space ? " " : ", ");
				}

				WriteValue(value);

				i++;
			}
		}

		public void WriteFunction(CssFunction function)
		{
			// {name}({args})

			// If rgba & args = 2

			if (function.Name == "rgba")
			{
				var args = GetArgs(function.Args).ToArray();

				if (args.Length == 2 && args[0].ToString().StartsWith("#"))
				{
					writer.Write(CssFunctions.Rgba(args));

					return;
				}
			}
			else if (function.Name == "lighten")
			{
				var args = GetArgs(function.Args).ToArray();

				writer.Write(CssFunctions.Lighten(args));

				return;
			}
			else if (function.Name == "darken")
			{
				var args = GetArgs(function.Args).ToArray();

				writer.Write(CssFunctions.Darken(args));

				return;
			}
			else if (function.Name == "saturate")
			{
				var args = GetArgs(function.Args).ToArray();

				writer.Write(CssFunctions.Saturate(args));

				return;
			}
			else if (function.Name == "desaturate")
			{
				var args = GetArgs(function.Args).ToArray();

				writer.Write(CssFunctions.Desaturate(args));

				return;
			}
			else if (function.Name == "adjust-hue")
			{
				var args = GetArgs(function.Args).ToArray();

				writer.Write(CssFunctions.AdjustHue(args));

				return;
			}
			
			writer.Write(function.Name);

			writer.Write("(");

			WriteValue(function.Args);
			
			writer.Write(")");
		}

		public IEnumerable<CssValue> GetArgs(CssValue value)
		{
			switch(value.Kind)
			{
				case NodeKind.Variable:
					var x = (CssVariable)value;

					if (x.Value != null)
					{
						yield return x.Value;
					}
					else
					{
						yield return context.Scope.GetValue(x.Symbol);
					}
					break;
				case NodeKind.ValueList:
					{
						var list = (CssValueList)value;

						if (list.Seperator == ValueListSeperator.Space) yield return list;

						// Break out comma seperated values
						foreach (var v in list)
						{
							foreach (var item in GetArgs((CssValue)v)) yield return item;
						}
					}
					
					break;
				case NodeKind.Function	: yield return value;	break;
				default					: yield return value;	break;
			}
		
		}

		public void WriteVariable(CssVariable variable)
		{
			if (variable.Value == null)
			{
				variable.Value = context.Scope.GetValue(variable.Symbol);
			}

			WriteValue(variable.Value);
		}

		public void WriteImportRule(ImportRule rule)
		{
			// TODO: normalize value
			writer.Write("@import " + rule.Url.ToString() + ';');
		}

		public void WriteRule(CssRule rule, int level = 0)
		{
			var i = 0;

			if (rule.SkipTransforms)
			{
				_WriteRule(rule, level);

				return;
			}

			foreach (var r in Rewrite(rule))
			{				
				foreach (var nr in context.Rewriters.Rewrite(r))
				{
					if (i != 0) writer.WriteLine();

					_WriteRule(nr, level);

					i++;
				}				
			}
		}

		public void _WriteRule(CssRule rule, int level = 0)
		{
			Indent(level);

			switch (rule.Type)
			{
				case RuleType.Import	: WriteImportRule((ImportRule)rule);				break;
				case RuleType.Media		: WriteMediaRule((MediaRule)rule, level);			break;
				case RuleType.Style		: WriteStyleRule((StyleRule)rule, level);			break;
				case RuleType.FontFace	: WriteFontFaceRule((FontFaceRule)rule, level);		break;
				case RuleType.Keyframes	: WriteKeyframesRule((KeyframesRule)rule, level);	break;

				// Unknown rules
				default:
					if (rule is AtRule) WriteAtRule((AtRule)rule, level);
					
					break;
			}
		}

		public void WriteAtRule(AtRule rule, int level)
		{
			writer.Write("@" + rule.AtName);

			if (rule.SelectorText != null)
			{
				writer.Write(" " + rule.SelectorText);
			}

			writer.Write(" ");

			WriteBlock(rule, level);
		}

		public void WriteStyleRule(StyleRule rule, int level)
		{
			WriteSelector(rule.Selector);

			writer.Write(" ");

			WriteBlock(rule, level);
		}

		public void WriteSelector(CssSelector selector)
		{
			if(selector.Count == 1) 
			{
				writer.Write(selector.Text);
			}
			else
			{
				var i = 0;

				foreach(var s in selector)
				{
					if (i != 0)
					{
						writer.Write("," + Environment.NewLine);
					}

					writer.Write(s);

					i++;
				}
			}
		}

		public void WriteMediaRule(MediaRule rule, int level)
		{
			writer.Write("@media {0} ", rule.RuleText); // Write selector

			WriteBlock(rule, level);
		}

		public void WriteFontFaceRule(FontFaceRule rule, int level)
		{
			writer.Write("@font-face "); // Write selector

			WriteBlock(rule, level);
		}

		public void WriteKeyframesRule(KeyframesRule rule, int level)
		{
			writer.Write("@keyframes {0} ", rule.Name); // Write selector

			WriteBlock(rule, level);
		}

		public void WriteBlock(CssBlock block, int level)
		{
			writer.Write("{"); // Block Start	

			var condenced = false;
			var count = 0;

			// Write the declarations
			foreach (var node in block.Children) // TODO: Change to an immutable list?
			{
				if (node.Kind == NodeKind.Include)
				{
					var b2 = new CssBlock(NodeKind.Block);

					b2.Add(node);

					ExpandInclude((IncludeNode)node, b2);

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

					if (block.Children.Count == 1)
					{
						condenced = true;
					}
					else
					{
						if (count == 0) writer.WriteLine();

						Indent(level);

						writer.Write(" ");
					}

					writer.Write(" ");

					WriteDeclaration(declaration);

					writer.Write(";");

				}
				else if (node.Kind == NodeKind.Rule)  // Nested rule
				{
					if (count == 0) writer.WriteLine();

					var childRule = (CssRule)node;

					WriteRule(childRule, level + 1);				
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
				writer.Write(" ");
			}
			else
			{
				Indent(level);
			}

			writer.Write("}"); // Block end
		}

		public void WriteDeclaration(CssDeclaration declaration)
		{
			writer.Write(declaration.Name);
			writer.Write(": ");
			WriteValue(declaration.Value);
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

		private readonly List<CssRule> root = new List<CssRule>();

		public IEnumerable<CssRule> Rewrite(CssRule rule)
		{
			var styleRule = rule as StyleRule;

			if (styleRule == null || rule.All(r => r.Kind == NodeKind.Declaration))
			{
				yield return rule;

				yield break;
			}

			var clone = (StyleRule)rule.CloneNode();

			root.Clear();

			root.Add(clone);

			// Expand includes
			foreach (var includeNode in clone.Children.OfType<IncludeNode>().ToArray())
			{
				ExpandInclude(
					includeNode,
					clone
				);

				clone.Children.Remove(includeNode);
			}

			foreach (var nestedRule in clone.Children.OfType<StyleRule>().ToArray())
			{
				foreach (var r in Expand(
					rule: nestedRule,
					parent: clone
				))
				{
					root.Add(r);
				}
			}

			foreach (var r in root)
			{
				if (!r.Childless) yield return r;
			}
		}

		public IEnumerable<CssRule> Expand(StyleRule rule, CssRule parent)
		{
			var newRule = new StyleRule(ExpandSelector(rule));

			foreach (var childNode in rule.Children.ToArray())
			{
				if (childNode is StyleRule)
				{
					var childRule = (StyleRule)childNode;

					foreach (var r in Expand(childRule, rule))
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

			if (!newRule.Childless) yield return newRule;
		}

		public void ExpandInclude(IncludeNode include, CssBlock rule)
		{
			includeCount++;

			if (includeCount > 1000) throw new Exception("Exceded include limit of 1,000");

			MixinNode mixin;

			if (!context.Mixins.TryGetValue(include.Name, out mixin))
			{
				throw new Exception(string.Format("Mixin '{0}' not registered", include.Name));
			}

			var index = rule.Children.IndexOf(include);

			var childContext = GetScope(mixin.Parameters, include.Args);

			var i = 0;

			foreach (var node in mixin.Children.ToArray())
			{
				// Bind variables

				if (node is IncludeNode)
				{
					ExpandInclude(
						(IncludeNode)node,
						rule
					);

					mixin.Children.Remove(node);
				}

				BindVariables(node, childContext);

				rule.Insert(i + 1, node.CloneNode());

				i++;
			}
		}


		public void BindVariables(CssNode node, CssScope scope)
		{
			if (node.Kind == NodeKind.Declaration)
			{
				var declaration = (CssDeclaration)node;

				BindVariables(declaration.Value, scope);
			}
			else if (node.Kind == NodeKind.Variable)
			{
				var variable = (CssVariable)node;

				variable.Value = scope.GetValue(variable.Symbol);
			}
			else if (node.HasChildren)
			{
				foreach (var n in node.Children)
				{
					BindVariables(n, scope);
				}
			}
		}

		public CssScope GetScope(IList<CssParameter> paramaters, CssValue args)
		{
			var list = new List<CssValue>();

			if (args != null)
			{
				var valueList = args as CssValueList;

				if (valueList == null)
				{
					list.Add(args); // Single Value
				}

				if (valueList != null && valueList.Seperator == ValueListSeperator.Comma)
				{
					list.AddRange(valueList.Children.OfType<CssValue>());
				}
			}

			var child = new CssScope(context.Scope);

			var i = 0;

			foreach (var p in paramaters)
			{
				var val = (list != null && list.Count >= i + 1) ? list[i] : p.DefaultValue;

				child.Add(p.Name, val);

				i++;
			}

			return child;
		}

		#endregion


		#region Selector Expansion

		public static CssSelector ExpandSelector(StyleRule rule)
		{
			var parts = new Stack<CssSelector>();

			parts.Push(rule.Selector);

			StyleRule current = rule;

			while ((current = current.Parent as StyleRule) != null)
			{
				parts.Push(current.Selector);

				if (parts.Count > 6)
				{
					throw new Exception(string.Format("Cannot nest more than 6 levels deep. Was {0}. ", string.Join(" ", parts)));
				}
			}

			var i = 0;

			var sb = new StringBuilder();

			foreach (var selector in parts)
			{
				if (selector.Contains("&"))
				{
					var x = selector.ToString().Replace("&", sb.ToString());

					sb.Clear();

					sb.Append(x);

					i++;

					continue;
				}

				if (i != 0) sb.Append(' ');

				i++;

				// h1, h2, h3

				if (selector.Count > 1)
				{
					var parentSelector = sb.ToString();

					sb.Clear();

					var c = GetSelector(parts.Skip(i));

					var q = 0;

					foreach (var a in selector)
					{
						if (q != 0) sb.Append(", ");

						sb.Append(parentSelector + a);

						if (c != null)
						{
							sb.Append(" " + c);
						}

						q++;
					}

					break;
				}
				else
				{
					sb.Append(selector);
				}
			}

			return new CssSelector(sb.ToString());

		}

		private static string GetSelector(IEnumerable<CssSelector> selectors)
		{
			// TODO: & support

			var i = 0;

			var sb = new StringBuilder();

			foreach (var selector in selectors)
			{
				if (i != 0) sb.Append(' ');

				sb.Append(selector);
			}

			return sb.ToString();

		}

		#endregion
	}
}