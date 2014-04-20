﻿namespace Carbon.Css
{
	using Carbon.Css.Parser;

	public class CssDimension : CssValue
	{
		private CssToken number;
		private CssToken unit;

		public CssDimension(CssToken number, CssToken unit)
			: base(NodeKind.Literal)
		{
			this.number = number;
			this.unit	= unit;
		}

		public override string Text
		{
			get { return number.Text + unit.Text; }
		}

		public override string ToString()
		{
			return Text;
		}
	}
}

// A dimension is a number immediately followed by a unit identifier.
// It corresponds to the DIMENSION token in the grammar. 
// [CSS21] Like keywords, unit identifiers are case-insensitive within the ASCII range.

/*
{num}{E}{M}			{return EMS;}
{num}{E}{X}			{return EXS;}
{num}{P}{X}			{return LENGTH;}
{num}{C}{M}			{return LENGTH;}
{num}{M}{M}			{return LENGTH;}
{num}{I}{N}			{return LENGTH;}
{num}{P}{T}			{return LENGTH;}
{num}{P}{C}			{return LENGTH;}
{num}{D}{E}{G}		{return ANGLE;}
{num}{R}{A}{D}		{return ANGLE;}
{num}{G}{R}{A}{D}	{return ANGLE;}
{num}{M}{S}			{return TIME;}
{num}{S}			{return TIME;}
{num}{H}{Z}			{return FREQ;}
{num}{K}{H}{Z}		{return FREQ;}
{num}{ident}		{return DIMENSION;}

{num}%				{return PERCENTAGE;}
{num}				{return NUMBER;}
*/


/*
EMS,
EXS,
PX,
CM,
MM,
IN,
PT,
PC,
DEG,
RAD,
GRAD,
TURN
*/