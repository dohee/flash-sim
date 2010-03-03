using System;
using System.Collections.Generic;
using Buffers.Memory;

namespace Buffers.Program
{
	abstract class TraceParser
	{
		public abstract void ParseLine(string[] parts, out uint startPageId, out uint length, out AccessType type);

		public static TraceParser CreateParser(string[] lineParts)
		{
			TraceParser p;

			if ((p = PositionLengthParser.TryCreate(lineParts)) != null ||
				(p = RelnodeBlocknumParser.TryCreate(lineParts)) != null ||
				(p = SPCParser.TryCreate(lineParts)) != null)
				return p;
			else
				return null;
		}
	}


	class PositionLengthParser : TraceParser
	{
		public static TraceParser TryCreate(string[] parts)
		{
			uint tmp, rw;
			return (parts.Length == 3 &&
				uint.TryParse(parts[0], out tmp) &&
				uint.TryParse(parts[1], out tmp) &&
				uint.TryParse(parts[2], out rw) &&
				(rw == 0 || rw == 1)) ?
				new PositionLengthParser() : null;
		}

		public override void ParseLine(string[] parts, out uint startPageId, out uint length, out AccessType type)
		{
			startPageId = uint.Parse(parts[0]);
			length = uint.Parse(parts[1]);
			type = (uint.Parse(parts[2]) == 0 ? AccessType.Read : AccessType.Write);
		}
	}


	class RelnodeBlocknumParser : TraceParser
	{
		public static TraceParser TryCreate(string[] parts)
		{
			uint tmp;
			float ftmp;

			if (parts.Length == 4 &&
				float.TryParse(parts[0], out ftmp) &&
				uint.TryParse(parts[1], out tmp) &&
				uint.TryParse(parts[2], out tmp))
			{
				string p3 = parts[3].ToLower();
				return (p3 == "r" || p3 == "w") ?
					new RelnodeBlocknumParser(0) : null;
			}
			else if (parts.Length == 5 &&
				uint.TryParse(parts[0], out tmp) &&
				float.TryParse(parts[1], out ftmp) &&
				uint.TryParse(parts[2], out tmp) &&
				uint.TryParse(parts[3], out tmp))
			{
				string p3 = parts[4].ToLower();
				return (p3 == "r" || p3 == "w") ?
					new RelnodeBlocknumParser(1) : null;
			}

			return null;
		}

		private readonly int partofs;

		public RelnodeBlocknumParser(int partofs)
		{
			this.partofs = partofs;
		}

		public override void ParseLine(string[] parts, out uint startPageId, out uint length, out AccessType type)
		{
			startPageId = uint.Parse(parts[1 + partofs]) + uint.Parse(parts[2 + partofs]);
			length = 1;
			type = (parts[3 + partofs].ToLower() == "r" ? AccessType.Read : AccessType.Write);
		}
	}


	class SPCParser : TraceParser
	{
		public static TraceParser TryCreate(string[] parts)
		{
			uint tmp;
			float ftmp;

			if (parts.Length >= 5 &&
				uint.TryParse(parts[0], out tmp) &&
				uint.TryParse(parts[1], out tmp) &&
				uint.TryParse(parts[2], out tmp) &&
				float.TryParse(parts[4], out ftmp))
			{
				string p3 = parts[3].ToLower();
				return (p3 == "r" || p3 == "w") ?
					new SPCParser() : null;
			}

			return null;
		}

		public override void ParseLine(string[] parts, out uint startPageId, out uint length, out AccessType type)
		{
			startPageId = uint.Parse(parts[1]);
			length = Math.Max(1, (uint.Parse(parts[2]) + (Config.PageSize - 1)) / Config.PageSize);
			length = Math.Max(length, 1);
			type = (parts[3].ToLower() == "r" ? AccessType.Read : AccessType.Write);
		}
	}

}
