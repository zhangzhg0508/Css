﻿namespace Carbon.Css
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	public class CssBlock : CssNode, IList<CssNode>
	{
		public CssBlock() 
			: base(NodeKind.Block) { }

		public CssBlock(NodeKind kind)
			: base(kind) { }

		// Name or Selector

		public bool IsEmpty
		{
			get { return children.Count != 0; }
		}

		public IEnumerable<CssDeclaration> FindDeclaration(string propertyName)
		{
			return children.OfType<CssDeclaration>().Where(d => d.Name == propertyName);
		}

		public CssDeclaration Get(string name)
		{
			return children.OfType<CssDeclaration>().FirstOrDefault(d => d.Name == name);
		}

		public int Count
		{
			get { return children.Count; }
		}

		public override string Text
		{
			get { throw new NotImplementedException(); }
		}


		#region IList<CssNode> Members

		public int IndexOf(CssNode node)
		{
			return children.IndexOf(node);
		}

		public void Insert(int index, CssNode item)
		{
			children.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			children.RemoveAt(index);
		}

		public CssNode this[int index]
		{
			get { return children[index]; }
			set { children[index] = value; }
		}

		public void Add(CssNode item)
		{
			children.Add(item);
		}

		void ICollection<CssNode>.Clear()
		{
			children.Clear();
		}

		bool ICollection<CssNode>.Contains(CssNode item)
		{
			return children.Contains(item);
		}

		void ICollection<CssNode>.CopyTo(CssNode[] array, int arrayIndex)
		{
			children.CopyTo(array, arrayIndex);
		}

		int ICollection<CssNode>.Count
		{
			get { return children.Count; }
		}

		bool ICollection<CssNode>.IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(CssNode item)
		{
			return children.Remove(item);
		}

		IEnumerator<CssNode> IEnumerable<CssNode>.GetEnumerator()
		{
			return children.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return children.GetEnumerator();
		}

		#endregion
	}
}

// A block starts with a left curly brace ({) and ends with the matching right curly brace (}).
// In between there may be any tokens, except that parentheses (( )), brackets ([ ]), and braces ({ }) must always occur in matching pairs and may be nested.
// Single (') and double quotes (") must also occur in matching pairs, and characters between them are parsed as a string.
// See Tokenization above for the definition of a string.