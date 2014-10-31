using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WSS
{
	public class DoubleBufferedTeeStream : Stream
	{
		private static readonly int BufferSize = 1024 * 16; // 16K
		private readonly Stream Underlying, Additional;

		private int Offset, PrimaryLength;
		private byte[] Primary, Secondary;

		private Task<int> SecondaryRead;
		private Task TeeWrite;

		public DoubleBufferedTeeStream(Stream underlying, Stream additional)
		{
			if (!underlying.CanRead)
				throw new ArgumentException("underlying cant read wtf m8");
			if (!additional.CanWrite)
				throw new ArgumentException("additional cant write wtf m8");

			Underlying = underlying;
			Additional = additional;

			Primary = new byte[BufferSize];
			Secondary = new byte[BufferSize];

			TeeWrite = Task.FromResult(null); // bootstrap: there is no previous TeeWrite
			SecondaryRead = ReadIntoSecondaryAfterTeeWriteIsFinished(Secondary); // start reading into the secondary buffer
			Offset = PrimaryLength = 0; // as soon as anyone wants to read, we have to switch buffers
		}

		public override void Flush()
		{
		}

		private async Task<int> ReadIntoSecondaryAfterTeeWriteIsFinished(byte[] buf)
		{
			await TeeWrite; // we can't reuse the buffer as long as it isn't fully written
			int read = await Underlying.ReadAsync(buf, 0, buf.Length); // alright, now let's read in some data
			TeeWrite = Additional.WriteAsync(buf, 0, read);
			return read;
		}

		/// <summary>
		/// Switchs the buffers.
		/// </summary>
		/// <returns><c>true</c>, if buffers were switched, <c>false</c> if we reached the end of the stream.</returns>
		private bool SwitchBuffers()
		{
			PrimaryLength = SecondaryRead.Result; // wait until secondary is fully read
			if (PrimaryLength == 0)
				return false;

			// the last read went into Secondary, so that's the new Primary
			var oldPrimary = Primary;
			Primary = Secondary;
			Secondary = oldPrimary;
			Offset = 0; // we just read, so reset offset to 0

			// once the current TeeWrite is finished, we can start reusing the old Primary (now Secondary)
			SecondaryRead = ReadIntoSecondaryAfterTeeWriteIsFinished(oldPrimary);
			return true;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (Offset >= PrimaryLength)
				if (!SwitchBuffers())
					return 0;

			Offset += count = Math.Min(count, PrimaryLength - Offset);
			Buffer.BlockCopy(Primary, Offset, buffer, offset, count);
			return count;
		}

		public override int ReadByte()
		{
			var b = Underlying.ReadByte();
			if (b != -1)
				Additional.WriteByte(checked((byte)b));
			return b;
		}

		// cancellation is not supported because fuck you, that's why
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (Offset >= PrimaryLength) {
				await SecondaryRead; // wait now, so the Result call won't block
				if (!SwitchBuffers())
					return 0;
			}

			// at this point we can just jump to the synchronous implementation
			// the (eventual) waiting is done
			// all we have to do is copy
			return Read(buffer, offset, count);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				TeeWrite.Wait();

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
		public override long Length { get { throw new NotSupportedException(); } }
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

