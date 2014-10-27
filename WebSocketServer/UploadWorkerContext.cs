using System;

namespace WSS
{
	public class UploadWorkerContext
	{
		public Database Database { get; private set; }

		public UploadWorkerContext()
		{
			Database = new Database();
		}
	}
}

