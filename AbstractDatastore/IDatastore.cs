using System;
using System.IO;

namespace AbstractDatastore
{
	public interface IDatastore
	{
		void Save<T>(T t);
		Stream StoreStream(string fileName);
		Stream StoreStream(string fileName, string ContentType);
		string GetFilenameByHash(string md5);
		T LoadBy<T>(string field, string value);
	}
}
