﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;

namespace Carbon.Css
{
	public class CssSelector : IEnumerable<string>
	{
		private readonly List<string> parts;

		public CssSelector(TokenList tokens)
		{
			var sb = new StringBuilder();

			foreach (var token in tokens)
			{
				if (token.IsTrivia)
				{
					// Prettify the trivia
					sb.Append(" ");
				}
				else
				{
					sb.Append(token.Text);
				}
			}

			this.parts = new List<string>(sb.ToString().Split(',').Select(t => t.Trim()));
		}

		public CssSelector(string text)
		{
			#region Preconditions

			if (text == null) throw new ArgumentNullException("text");

			#endregion

			if (text.Contains(','))
			{
				this.parts = new List<string>(text.Split(',').Select(t => t.Trim()));
			}
			else
			{
				this.parts = new List<string>(new[] { text });
			}
		}

		public CssSelector(List<string> parts)
		{
			this.parts = parts;
		}

		public int Count => parts.Count;

		public string this[int index] => parts[index];

		public bool Contains(string text)
		{
			if (parts.Count == 1) return parts[0].Contains(text);

			foreach (var part in parts)
			{
				if (part.Contains(text)) return true;
			}

			return false;
		}

		public override string ToString()
		{
			if (parts.Count == 1) return parts[0];

			return string.Join(", ", parts);
		}

		public IEnumerator<string> GetEnumerator() => parts.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => parts.GetEnumerator();
	}

	// a:hover
	// #id
	// .className
	// .className, .anotherName			(Multiselector or group)
}