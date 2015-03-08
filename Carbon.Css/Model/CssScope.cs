﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Carbon.Css
{
	public class CssScope
	{
		private readonly CssScope parent;
		private readonly IDictionary<string, CssValue> items;

		private int counter = 0;

		public CssScope(IDictionary<string, CssValue> items)
		{
			this.items = items;
		}

		public CssScope(CssScope parent = null)
		{
			this.parent = parent;
			this.items = new Dictionary<string, CssValue>(); // StringComparer.OrdinalIgnoreCase
		}

		public object This { get; set; }

		public CssScope Parent
		{
			get { return parent; }
		}

		public CssValue this[string name]
		{
			get { return GetValue(name); }
			set { items[name] = value; }
		}

		public void Add(string name, CssValue value)
		{
			items.Add(name, value);
		}

		public bool TryGetValue(string key, out CssValue value)
		{
			return items.TryGetValue(key, out value);
		}

		public CssValue GetValue(string name)
		{
			counter++;

			if (counter > 10000) throw new Exception("recussion detected");

			CssValue value;

			if (items.TryGetValue(name, out value))
			{
				if (value.Kind == NodeKind.Variable)
				{
					var variable = (CssVariable)value;

					if (variable.Symbol == name) throw new Exception("Self referencing");

					return GetValue(variable.Symbol);
				}

				return value;
			}

			if (parent != null && parent.TryGetValue(name, out value))
			{
				if (value.Kind == NodeKind.Variable)
				{

					var variable = (CssVariable)value;

					if (variable.Symbol == name) throw new Exception("Self referencing");

					return parent.GetValue(variable.Symbol);
				}

				return value;

			}


			return new CssString(string.Format("/* ${0} not found */", name));
		}

		public int Count
		{
			get { return items.Count; }
		}

		public void Clear()
		{
			items.Clear();
		}

	}
}
