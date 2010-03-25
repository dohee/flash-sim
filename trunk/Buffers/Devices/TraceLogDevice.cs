using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Buffers.Utilities;

namespace Buffers.Devices
{
	public class TraceLogDevice : BlockDeviceBase
	{
		private readonly string path;
		private readonly StreamWriter writer;

		public TraceLogDevice(string path)
		{
			FileStream stream = new FileStream(path, FileMode.Create,
				FileAccess.Write, FileShare.Read, 128 * 1024,
				FileOptions.Asynchronous | FileOptions.SequentialScan);

			this.path = stream.Name;
			this.writer = new StreamWriter(stream, Encoding.Default, 128 * 1024);
		}

		#region Derived Dispose 函数族
		private bool _disposed_TraceLogDevice = false;

		protected override void Dispose(bool isDisposing)
		{
			if (_disposed_TraceLogDevice)
				return;

			if (isDisposing)
				writer.Close(); // 清理托管资源

			// 清理非托管资源

			base.Dispose(isDisposing);
			_disposed_TraceLogDevice = true;
		}
		#endregion

		public override string Name { get { return "TraceLog"; } }
		public override string Description { get { return Utils.FormatDescription("Path", path); } }

		protected override void DoRead(uint pageid, byte[] result)
		{
			writer.WriteLine(pageid + "\t1\t0");
		}
		protected override void DoWrite(uint pageid, byte[] data)
		{
			writer.WriteLine(pageid + "\t1\t1");
		}
	}
}
