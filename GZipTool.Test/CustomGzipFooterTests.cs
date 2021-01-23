using GzipTool.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace GzipTool.Test
{
	[TestClass]
	public class CustomGzipFooterTests
	{
		[TestMethod]
		public void SerializationTest()
		{
			CustomGzipFooter deserializedFooter, originalFooter = new CustomGzipFooter();
			originalFooter.AddChunkInfo(5, 5, 10, 10);
			originalFooter.AddChunkInfo(5, 5, 10, 10);
			originalFooter.AddChunkInfo(100, 200, 300, 400);
			using (var ms = new MemoryStream())
			{
				originalFooter.WriteToStream(ms);
				Assert.AreEqual(originalFooter.GetFooterSize(), ms.Position);
				ms.Seek(0, SeekOrigin.Begin);
				deserializedFooter = CustomGzipFooter.FromStream(ms);
			}
			Assert.AreEqual(originalFooter, deserializedFooter);
		}

		[TestMethod]
		public void EqualsTest()
		{
			CustomGzipFooter
				footer1 = new CustomGzipFooter(),
				footer2 = new CustomGzipFooter();

			footer1.AddChunkInfo(5, 5, 10, 10);
			footer2.AddChunkInfo(5, 5, 10, 10);

			Assert.AreEqual(footer1, footer2);

			footer1.AddChunkInfo(5, 5, 10, 10);
			footer2.AddChunkInfo(5, 5, 10, 11);

			Assert.AreNotEqual(footer1, footer2);
		}
	}
}
