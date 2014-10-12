using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/*
 * https://github.com/mono/mono/blob/master/mcs/class/System.Web/System.Web/HttpRequest.cs
 */

namespace FuckThisFuckingCGIFuck
{
	//
	// Stream-based multipart handling.
	//
	// In this incarnation deals with an HttpInputStream as we are now using
	// IntPtr-based streams instead of byte [].   In the future, we will also
	// send uploads above a certain threshold into the disk (to implement
	// limit-less HttpInputFiles). 
	//

	public class HttpMultipart {
		public class Element {
			public string ContentType;
			public string Name;
			public string Filename;
			public byte[] Data;
			public string AsText { get { return Encoding.ASCII.GetString(Data.Take(16).ToArray()); } }
			public string Text { get { return Encoding.ASCII.GetString(Data); } }

			public override string ToString()
			{
				return string.Format("Element: ContentType={0} Name={1} Filename={2} AsText={4} Data.Length={3}", ContentType, Name, Filename, Data.Length, AsText);
			} 
		}

		Stream data;
		string boundary;
		byte [] boundary_bytes;
		byte [] buffer;
		bool at_eof;
		Encoding encoding;
		StringBuilder sb;

		const byte HYPHEN = (byte) '-', LF = (byte) '\n', CR = (byte) '\r';

		string lastElementName = null;

		// See RFC 2046 
		// In the case of multipart entities, in which one or more different
		// sets of data are combined in a single body, a "multipart" media type
		// field must appear in the entity's header.  The body must then contain
		// one or more body parts, each preceded by a boundary delimiter line,
		// and the last one followed by a closing boundary delimiter line.
		// After its boundary delimiter line, each body part then consists of a
		// header area, a blank line, and a body area.  Thus a body part is
		// similar to an RFC 822 message in syntax, but different in meaning.
		/*
		public HttpMultipart (Stream data, string b, Encoding encoding)
		{
			this.data = data;
			boundary = b;
			boundary_bytes = encoding.GetBytes (b);
			buffer = new byte [boundary_bytes.Length + 2]; // CRLF or '--'
			this.encoding = encoding;
			sb = new StringBuilder ();
		}*/

		public HttpMultipart (Stream data, string b, Encoding encoding, string lastElementName)
		{
			this.data = data;
			boundary = b;
			boundary_bytes = encoding.GetBytes ("\r\n--" + b + "\r\n");
			buffer = new byte [boundary_bytes.Length + 2]; // CRLF or '--'
			this.encoding = encoding;
			sb = new StringBuilder ();
			this.lastElementName = lastElementName;
		}

		string ReadLine ()
		{
			// CRLF or LF are ok as line endings.
			bool got_cr = false;
			int b = 0;
			sb.Length = 0;
			while (true) {
				b = data.ReadByte ();
				if (b == -1) {
					return null;
				}

				if (b == LF) {
					break;
				}
				got_cr = (b == CR);
				sb.Append ((char) b);
			}

			if (got_cr)
				sb.Length--;

			return sb.ToString ();

		}

		static string GetContentDispositionAttribute (string l, string name)
		{
			int idx = l.IndexOf (name + "=\"");
			if (idx < 0)
				return null;
			int begin = idx + name.Length + "=\"".Length;
			int end = l.IndexOf ('"', begin);
			if (end < 0)
				return null;
			if (begin == end)
				return "";
			return l.Substring (begin, end - begin);
		}

		string GetContentDispositionAttributeWithEncoding (string l, string name)
		{
			int idx = l.IndexOf (name + "=\"");
			if (idx < 0)
				return null;
			int begin = idx + name.Length + "=\"".Length;
			int end = l.IndexOf ('"', begin);
			if (end < 0)
				return null;
			if (begin == end)
				return "";

			string temp = l.Substring (begin, end - begin);
			byte [] source = new byte [temp.Length];
			for (int i = temp.Length - 1; i >= 0; i--)
				source [i] = (byte) temp [i];

			return encoding.GetString (source);
		}

		public bool ReadBoundary ()
		{
			try {
				string line = ReadLine ();
				while (line == "")
					line = ReadLine ();
				if (line [0] != '-' || line [1] != '-')
					return false;

				if (!StrUtils.EndsWith (line, boundary, false))
					return true;
			} catch {
			}

			return false;
		}

		string ReadHeaders ()
		{
			string s = ReadLine ();
			if (s == "")
				return null;

			return s;
		}

		bool CompareBytes (byte [] orig, byte [] other)
		{
			for (int i = orig.Length - 1; i >= 0; i--)
				if (orig [i] != other [i])
					return false;

			return true;
		}

		byte[] MoveToNextBoundary(int maxlength)
		{
			var bytes = new List<byte>();
			int matchLength = 0;
			while (true) {
				var x = data.ReadByte();
				bytes.Add(checked((byte)x));
				//Console.WriteLine("{0} {1}", x, (char)x);
				if ((x < 0) || (bytes.Count > maxlength))
					throw new Exception(String.Format("get your shit together " + bytes.Count));
				if (x == boundary_bytes[matchLength]) {
					if (++matchLength == boundary_bytes.Length)
						return bytes.Take(bytes.Count - boundary_bytes.Length).ToArray();
				} else
					matchLength = 0;
			}
		}
		/*
		byte[] MoveToNextBoundary (int maxlength)
		{
			long retval = 0;
			bool got_cr = false;

			long start = data.Position;
			int state = 0;
			int c = data.ReadByte ();
			while (true) {
				if (data.Position - start > maxlength)
					throw new InvalidDataException("data too long");

				if (c == -1)
					return -1;

				if (state == 0 && c == LF) {
					retval = data.Position - 1;
					if (got_cr)
						retval--;
					state = 1;
					c = data.ReadByte ();
				} else if (state == 0) {
					got_cr = (c == CR);
					c = data.ReadByte ();
				} else if (state == 1 && c == '-') {
					c = data.ReadByte ();
					if (c == -1)
						return -1;

					if (c != '-') {
						state = 0;
						got_cr = false;
						continue; // no ReadByte() here
					}

					int nread = data.Read (buffer, 0, buffer.Length);
					int bl = buffer.Length;
					if (nread != bl)
						return -1;

					if (!CompareBytes (boundary_bytes, buffer)) {
						state = 0;
						data.Position = retval + 2;
						if (got_cr) {
							data.ReadByte(); //data.Position++;
							got_cr = false;
						}
						c = data.ReadByte ();
						continue;
					}

					if (buffer [bl - 2] == '-' && buffer [bl - 1] == '-') {
						at_eof = true;
					} else if (buffer [bl - 2] != CR || buffer [bl - 1] != LF) {
						state = 0;
						data.Position = retval + 2;
						if (got_cr) {
							data.ReadByte(); //data.Position++;
							got_cr = false;
						}
						c = data.ReadByte ();
						continue;
					}
					data.Position = retval + 2;
					if (got_cr)
						data.ReadByte();
						//data.Position++;
					break;
				} else {
					// state == 1
					state = 0; // no ReadByte() here
				}
			}

			return retval;
		}*/

		public Element ReadNextElement (int maxlength)
		{
			if (at_eof/* || ReadBoundary ()*/)
				return null;

			Element elem = new Element ();
			string header;
			bool stopDoNotMovePutYourHandsInTheAir = false;

			while ((header = ReadHeaders ()) != null) {
				if (StrUtils.StartsWith (header, "Content-Disposition:", true)) {
					elem.Name = GetContentDispositionAttribute (header, "name");
					elem.Filename = StripPath (GetContentDispositionAttributeWithEncoding (header, "filename"));

					if (elem.Name == lastElementName)
						stopDoNotMovePutYourHandsInTheAir = true;

				} else if (StrUtils.StartsWith (header, "Content-Type:", true)) {
					elem.ContentType = header.Substring ("Content-Type:".Length).Trim ();
				}
			}

			if (stopDoNotMovePutYourHandsInTheAir)
				return null;

			//long pos = MoveToNextBoundary (maxlength);
			//if (pos == -1)
			//	return null;
			//elem.Length = pos - start;
			//Console.WriteLine("gonna do fancy stuff for {0}", elem.Name);
			elem.Data = MoveToNextBoundary(maxlength);

			return elem;
		}

		static string StripPath (string path)
		{
			if (path == null || path.Length == 0)
				return path;

			if (path.IndexOf (":\\") != 1 && !path.StartsWith ("\\\\"))
				return path;
			return path.Substring (path.LastIndexOf ('\\') + 1);
		}
	}
}

