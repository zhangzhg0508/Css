﻿namespace Carbon.Css
{
	using System;
	using System.Collections;
	using System.Collections.Generic;

	public abstract class CssNode : IEnumerable<CssNode>
	{
		private readonly NodeKind kind;
		private CssNode parent;

		public CssNode(NodeKind kind, CssNode parent = null)
		{
			this.kind = kind;
			this.parent = parent;
		}

		public NodeKind Kind
		{
			get { return kind; }
		}

		// Replace with tostring or span
		public abstract string Text { get; }

		public CssNode Parent
		{
			get { return parent; }
			set { parent = value; }
		}

		internal Whitespace Leading { get; set; }

		internal Whitespace Trailing { get; set; }

		// ChildNodes

		public virtual IList<CssNode> Children
		{
			get { return new CssNode[0]; }
		}

		public bool HasChildren
		{
			get { return Children != null && Children.Count > 0; }
		}

		public virtual CssNode Clone()
		{
			throw new Exception(this.Kind + "/" + this.GetType().Name);

			throw new NotImplementedException();
		}

		#region IEnumerator

		IEnumerator<CssNode> IEnumerable<CssNode>.GetEnumerator()
		{
			return Children.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Children.GetEnumerator();
		}

		#endregion
	}
}