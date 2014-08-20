﻿namespace Carbon.Css.Parser
{
	using Carbon.Css.Helpers;
	using System;
	using System.Collections.Generic;

	public class ParseException : Exception
	{
		private readonly int position = 0;

		public ParseException(string message, int position = 0)
			: base(message) { 
		
			this.position = position;
		}

		public int Position
		{
			get { return position; }
		}

		public SourceLocation Location { get; set; }

		public IList<LineInfo> Lines { get; set; }

		public static ParseException UnexpectedEOF(string context)
		{
			return new ParseException(string.Format("Unexpected EOF reading '{0}'.", context));
		}
	}

	public class UnexpectedModeChange : ParseException
	{
		// "Current mode is:" + current + ". Leaving " + mode + "."

		private int position;

		public UnexpectedModeChange(LexicalMode currentMode, LexicalMode leavingMode, int position)
			: base(String.Format("Unexpected mode change. Expected {0}. Was {1}.", currentMode.ToString(), leavingMode.ToString()))
		{
			this.position = position;
		}
	}

	public class UnbalancedBlock : ParseException
	{
		// "Current mode is:" + current + ". Leaving " + mode + "."

		public UnbalancedBlock(LexicalMode currentMode, CssToken startToken)
			: base("The block is unclosed, '}' expected", startToken.Position) { }
	}

	public class UnexpectedTokenException : ParseException
	{
		private readonly CssToken token;

		public UnexpectedTokenException(LexicalMode mode, CssToken token)
			: base(String.Format("Unexpected token reading {0}. Was '{1}'.", mode, token.Kind))
		{
			this.token = token;
		}

		public UnexpectedTokenException(LexicalMode mode, TokenKind expectedKind, CssToken token)
			: base(String.Format("Unexpected token reading {0}. Expected '{1}'. Was '{2}'.", mode, expectedKind, token.Kind), token.Position)
		{
			this.token = token;
		}

		public UnexpectedTokenException(CssToken token)
			: base(String.Format("Unexpected token. Was '{0}:{1}'.", token.Kind, token.Text), token.Position)
		{
			this.token = token;
		}

		public CssToken Token
		{
			get { return token; }
		}
	}
}