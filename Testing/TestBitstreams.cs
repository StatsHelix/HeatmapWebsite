using NUnit.Framework;
using System;

using DemoInfo.BitStreamImpl;
using System.IO;
using DemoInfo;

namespace Testing
{
	[TestFixture]
	public class TestBitstreams
	{
		private Random rng;
		private byte[] data;
		private Stream memstream1, memstream2;
		private IBitStream dbgAll;

		[SetUp]
		public void Init()
		{
			rng = new Random(1337);
			data = new byte[128 * 1024]; // 128K
			rng.NextBytes(data);

			var reference = new BitArrayStream(data);
			memstream1 = new MemoryStream(data);
			memstream2 = new MemoryStream(data);
			var managedBs = new ManagedBitStream();
			managedBs.Initialize(memstream1);
			var unsafeBs = new UnsafeBitStream();
			unsafeBs.Initialize(memstream2);

			var dbgManaged = new DebugBitStream(reference, managedBs);
			dbgAll = new DebugBitStream(dbgManaged, unsafeBs);
		}

		[TearDown]
		public void Dispose()
		{
			rng = null;
			data = null;
			memstream1.Dispose();
			memstream2.Dispose();
			dbgAll.Dispose();
		}

		[Test]
		public void TestReadInt()
		{
			int bitOffset = 0;
			int totalBits = data.Length * 8;

			while (bitOffset < totalBits) {
				int thisTime = Math.Min(rng.Next(32) + 1, totalBits - bitOffset);
				dbgAll.ReadInt(thisTime);
				bitOffset += thisTime;
			}
		}

		[Test]
		public void TestReadSignedInt()
		{
			int bitOffset = 0;
			int totalBits = data.Length * 8;

			while (bitOffset < totalBits) {
				int thisTime = Math.Min(rng.Next(32) + 1, totalBits - bitOffset);
				dbgAll.ReadSignedInt(thisTime);
				bitOffset += thisTime;
			}
		}

		[Test]
		public void TestReadByte()
		{
			int bitOffset = 0;
			int totalBits = data.Length * 8;

			while (bitOffset < totalBits) {
				int thisTime = Math.Min(rng.Next(8) + 1, totalBits - bitOffset);
				dbgAll.ReadByte(thisTime);
				bitOffset += thisTime;
			}
		}

		[Test]
		public void TestReadBytes()
		{
			int offset = 0;
			while (offset < data.Length) {
				int thisTime = rng.Next(data.Length - offset) + 1;
				dbgAll.ReadBytes(thisTime);
				offset += thisTime;
			}
		}
	}
}

