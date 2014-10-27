using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WSS
{
	public class TeeStream : Stream
	{
		private readonly Stream Underlying, Additional;

		public TeeStream(Stream underlying, Stream additional)
		{
			Underlying = underlying;
			Additional = additional;
		}

		public override void Flush()
		{
			Additional.Flush();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int ret = Underlying.Read(buffer, offset, count);
			Additional.Write(buffer, offset, ret);
			return ret;
		}

		public override int ReadByte()
		{
			var b = Underlying.ReadByte();
			if (b != -1)
				Additional.WriteByte(checked((byte)b));
			return b;
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			int ret = await Underlying.ReadAsync(buffer, offset, count, cancellationToken);
			await Additional.WriteAsync(buffer, offset, ret, cancellationToken);
			return ret;
		}

		private class MyAsyncResult : IAsyncResult
		{
			public IAsyncResult Underlying { get; set; }
			public byte[] Buffer { get; set; }
			public int Offset { get; set; }
			public object State { get; set; }
			public AsyncCallback Callback { get; set; }
			public int ReturnValue { get; set; }

			public object AsyncState { get { return Underlying.AsyncState; } }
			public System.Threading.WaitHandle AsyncWaitHandle { get { return Underlying.AsyncWaitHandle; } }
			public bool CompletedSynchronously { get { return Underlying.CompletedSynchronously; } }
			public bool IsCompleted { get { return Underlying.IsCompleted; } }
		}

		private void HandleReadCallback(IAsyncResult ar)
		{
			var mar = (MyAsyncResult)ar.AsyncState;
			int ret = Underlying.EndRead(ar);
			mar.ReturnValue = ret;
			Additional.BeginWrite(mar.Buffer, mar.Offset, ret, HandleWriteCallback, mar);
		}

		private void HandleWriteCallback(IAsyncResult ar)
		{
			var mar = (MyAsyncResult)ar.AsyncState;
			Additional.EndWrite(ar);
			mar.Callback(mar);
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			var mar = new MyAsyncResult {
				State = state,
				Buffer = buffer,
				Offset = offset,
				Callback = callback,
			};
			mar.Underlying = Underlying.BeginRead(buffer, offset, count, HandleReadCallback, mar);
			return mar;
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			return ((MyAsyncResult)asyncResult).ReturnValue;
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return Additional.FlushAsync(cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				Underlying.Dispose();
				Additional.Dispose();
			}
			base.Dispose(disposing);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return false; } }
		public override long Length { get { return Underlying.Length; } }
		public override bool CanTimeout { get { return Underlying.CanTimeout; } }
		public override long Position {
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}
		public override int ReadTimeout {
			get { return Underlying.ReadTimeout; }
			set { Underlying.ReadTimeout = value; }
		}
	}
}

