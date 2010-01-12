using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Buffers
{
	static class ColorStack
	{
		private static readonly Stack<ConsoleColor> clrstack = new Stack<ConsoleColor>();

		public static void PushColor(ConsoleColor newcolor)
		{
			clrstack.Push(Console.ForegroundColor);
			Console.ForegroundColor = newcolor;
		}

		public static void PopColor()
		{
			Console.ForegroundColor = clrstack.Pop();
		}
	}
}
