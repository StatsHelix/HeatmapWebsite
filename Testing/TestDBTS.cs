using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

using WSS;

namespace Testing
{
	[TestFixture]
	public class TestDBTS
	{
		[Test]
		public void TestBasicFunctionality()
		{
			var rng = new Random(1337);
			var data = new byte[512 * 1024];
			rng.NextBytes(data);

			using (var underlying = new MemoryStream(data))
			using (var additional = new MemoryStream())
			using (var verify = new MemoryStream())
			using (var dbts = new DoubleBufferedTeeStream(underlying, additional)) {
				dbts.CopyTo(verify);
				Assert.AreEqual(data, verify.GetBuffer());
				Assert.AreEqual(data, additional.GetBuffer());
			}
		}

		[Test]
		public void TestRandomReads()
		{
			var rng = new Random(1337);
			var data = new byte[512 * 1024];
			rng.NextBytes(data);

			using (var underlying = new MemoryStream(data))
			using (var additional = new MemoryStream())
			using (var dbts = new DoubleBufferedTeeStream(underlying, additional)) {
				var buf = new byte[1024 * 17]; // 17K
				int actually;
				for (int offset = 0; offset < data.Length; offset += actually) {
					actually = dbts.Read(buf, 0, rng.Next(buf.Length) + 1);
					Assert.IsTrue(data.Skip(offset).Take(actually).SequenceEqual(buf.Take(actually)));
				}

				Assert.AreEqual(data, additional.GetBuffer());
			}
		}
	}
}

