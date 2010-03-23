using System;
using Buffers;

namespace Buffers.Devices
{
	public abstract class BlockDeviceBase : IBlockDevice
	{
		/// <summary> 真实的读操作 </summary>
		/// <remarks> 继承者注意：必须实现 DoRead、DoWrite 两个，
		/// 或者 DoAccess 一个，而不能同时实现这三个 </remarks>
		protected virtual void DoRead(uint pageid, byte[] result)
		{
			DoAccess(pageid, result, AccessType.Read);
		}
		/// <summary> 真实的写操作 </summary>
		/// <remarks> 继承者注意：必须实现 DoRead、DoWrite 两个，
		/// 或者 DoAccess 一个，而不能同时实现这三个 </remarks>
		protected virtual void DoWrite(uint pageid, byte[] data)
		{
			DoAccess(pageid, data, AccessType.Write);
		}
		/// <summary> 真实的读写操作 </summary>
		/// <remarks> 继承者注意：必须实现 DoRead、DoWrite 两个，
		/// 或者 DoAccess 一个，而不能同时实现这三个 </remarks>
		protected virtual void DoAccess(uint pageid, byte[] resultOrData, AccessType type) { }

		/// <summary> 在真实读操作执行前执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法开头处调用 base.BeforeRead </remarks>
		protected virtual void BeforeRead(uint pageid) { }
		/// <summary> 在真实写操作执行前执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法开头处调用 base.BeforeWrite </remarks>
		protected virtual void BeforeWrite(uint pageid) { }
		/// <summary> 在真实读操作执行后执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法结尾处调用 base.AfterRead </remarks>
		protected virtual void AfterRead(uint pageid) { ReadCount++; }
		/// <summary> 在真实写操作执行后执行此动作 </summary>
		/// <remarks> 继承者注意：必须在方法结尾处调用 base.AfterWrite </remarks>
		protected virtual void AfterWrite(uint pageid) { WriteCount++; }

		public virtual string Name { get { return this.GetType().Name; } }
		public virtual string Description { get { return null; } }
		public virtual uint PageSize { get; protected set; }
		public int ReadCount { get; private set; }
		public int WriteCount { get; private set; }

		public void Read(uint pageid, byte[] result)
		{
			BeforeRead(pageid);
			DoRead(pageid, result);
			AfterRead(pageid);
		}
		public void Write(uint pageid, byte[] data)
		{
			BeforeWrite(pageid);
			DoWrite(pageid, data);
			AfterWrite(pageid);
		}

		#region Dispose 函数族
		private bool _disposed_BlockDeviceBase = false;

		~BlockDeviceBase()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool isDisposing)
		{
			if (_disposed_BlockDeviceBase)
				return;

			if (isDisposing) { }// 清理托管资源

			// 清理非托管资源

			_disposed_BlockDeviceBase = true;
		}
		#endregion
	}
}