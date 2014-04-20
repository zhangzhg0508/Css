﻿namespace Carbon.Css
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	// A StyleRule rule has a selector, and one or more declarations

	public class CssRule : CssBlock
	{
		private readonly RuleType type;
		private readonly ICssSelector selector;

		public CssRule(RuleType type, ICssSelector selector)
			: base(NodeKind.Rule)
		{
			this.type = type;
			this.selector = selector;
		}

		public CssRule(RuleType type, NodeKind kind)
			: base(kind) 
		{	
			this.type = type;
		}

		public RuleType Type
		{
			get { return type; }
		}

		public ICssSelector Selector
		{
			get { return selector; }
		}
	

		#region Helpers

		public void Expand()
		{
			new DefaultRuleTransformer().Transform(this);
		}

		#endregion

		public override string Text
		{
			get { return ToString(); }
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			using (var sw = new StringWriter(sb))
			{
				new CssWriter(sw, new CssContext());

				return sb.ToString();
			}
		}
	}
}