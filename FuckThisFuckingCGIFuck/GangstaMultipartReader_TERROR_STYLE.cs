using System;
using System.IO;

namespace FuckThisFuckingCGIFuck
{
	public interface GangstaMultipartReader_TERROR_STYLE /* alligatoah ftw */
	{
		Stream Stream { get; }

		/// <summary>
		/// Gets the next element. (name; length)
		/// You may read _length_ bytes from <see cref="Stream"/>. Then you have to call this again.
		/// </summary>
		/// <returns>The next element or null at the end of the stream.</returns>
		Tuple<string, int> GetNextElement();
	}
}

