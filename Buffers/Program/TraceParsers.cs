using System;
using System.Collections.Generic;
using Buffers.Memory;

namespace Buffers.Program
{
	abstract class TraceParser
	{
		public abstract void ParseLine(string[] parts, out RWQuery query, out RWQuery[] extraQueries);

		public static TraceParser CreateParser(string[] lineParts)
		{
			if (lineParts.Length == 3)
				return new PositionLengthParser();
			else if (lineParts.Length == 4)
				return new RelnodeBlocknumParser();
			else
				return null;
		}
	}

	class PositionLengthParser : TraceParser
	{
		public override void ParseLine(string[] parts, out RWQuery query, out RWQuery[] extraQueries)
		{
			uint pageid = uint.Parse(parts[0]);
			uint length = uint.Parse(parts[1]);
			uint rw = uint.Parse(parts[2]);

			query = new RWQuery(pageid, (rw == 0 ? AccessType.Read : AccessType.Write));

			if (length == 1)
			{
				extraQueries = null;
				return;
			}

			AccessType type = query.Type;
			extraQueries = new RWQuery[length - 1];

			for (int i = 0; i < extraQueries.Length; i++)
				extraQueries[i] = new RWQuery(++pageid, type);
		}
	}

	class RelnodeBlocknumParser : TraceParser
	{
		public override void ParseLine(string[] parts, out RWQuery query, out RWQuery[] extraQueries)
		{
			extraQueries = null;

			query = new RWQuery(
				uint.Parse(parts[1]) + uint.Parse(parts[2]),
				parts[3] == "R" ? AccessType.Read : AccessType.Write);
		}
	}
}
