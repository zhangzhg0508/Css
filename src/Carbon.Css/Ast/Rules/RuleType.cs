﻿using System.Text.Json.Serialization;

namespace Carbon.Css;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleType
{
	Unknown				= 0,
	Style				= 1,
	Charset				= 2,
	Import				= 3,
	Media				= 4,
	FontFace			= 5,
	Page				= 6,
	Keyframes			= 7,
	Keyframe			= 8,
	Namespace			= 10,
	CounterStyle		= 11,
	Supports			= 12,
	Document			= 13,
	FontFeatureValues	= 14,
	Viewport			= 15,
	Region				= 16,
}