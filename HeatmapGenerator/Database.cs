using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

namespace Flai.Mongo
{
	public static class Database
	{
		[ThreadStatic]
		private static MongoDatabase database = null;

		[ThreadStatic]
		private static string DBName;

		private static MongoDatabase GetDatabase()
		{
			if (database == null)
			{
				DBName = Assembly.GetEntryAssembly().GetName().Name;

				MongoClient client = new MongoClient();
				database = client.GetServer().GetDatabase(DBName);
			}

			return database;
		}

		public static int GetNextValueFromSequence(string sequenceName)
		{
			FindAndModifyArgs args = new FindAndModifyArgs();
			args.Query = (IMongoQuery)new QueryDocument("name", sequenceName);

			args.Update = new UpdateDocument() 
			{ 
				{ 
					"$setOnInsert", new BsonDocument() {
						{"name", sequenceName }
					} 
				},
				{ "$inc", new BsonDocument("seq", 1) }
			};
			args.Upsert = true;


			var counters = GetCollection("__Counters");
			var res = counters.FindAndModify(args);

			if (res.ModifiedDocument == null)
				return 0;

			return res.ModifiedDocument["seq"].AsInt32;
		}


		private static MongoCollection GetCollection<T>()
		{
			return GetCollection(typeof(T));
		}

		private static MongoCollection GetCollection(Type type)
		{
			return GetCollection(type.Name);
		}

		private static MongoCollection GetCollection(string name)
		{
			return GetDatabase().GetCollection(name);
		}

		public static T Load<T>(BsonValue id)
		{
			return GetCollection<T>().FindOneByIdAs<T>(id);
		}

		public static T LoadByObjectID<T>(string objectID)
		{
			return GetCollection<T>().FindOneByIdAs<T>(new BsonObjectId(objectID));
		}


		public static T LoadBy<T>(string field, BsonValue value)
		{
			return GetCollection<T>().FindOneAs<T>(new QueryDocument(field, value));
		}

		public static T Load<T>(IMongoQuery query)
		{
			return GetCollection<T>().FindOneAs<T>(query);
		}

		public static void Save<T>(T t)
		{
			var collection = GetCollection<T>();
			collection.Save(t);
		}

		public static void Save(object t)
		{
			var collection = GetCollection(t.GetType());
			collection.Save(t);
		}

		public static void StoreFile(Stream stream, string fileName, string ContentType)
		{
			MongoGridFSCreateOptions options = new MongoGridFSCreateOptions();
			options.ContentType = ContentType;
			GetDatabase().GridFS.Upload(stream, fileName, options);
		}

		public static void StoreFile(Stream stream, string fileName)
		{
			StoreFile(stream, fileName, "application/octet-stream");
		}

		public static Stream StoreStream(string fileName, string ContentType)
		{
			MongoGridFSCreateOptions options = new MongoGridFSCreateOptions();
			options.ContentType = ContentType;
			return GetDatabase().GridFS.OpenWrite(fileName);
		}

		public static Stream StoreStream(string fileName)
		{
			return StoreStream(fileName, "application/octet-stream");
		}

		public static Stream RetrieveFile(string fileName)
		{
			return GetDatabase().GridFS.FindOne(fileName).OpenRead();
		}

		public static long GetFileSize(string fileName)
		{
			return GetDatabase().GridFS.FindOne(fileName).Length;
		}

		public static string GetFileType(string fileName)
		{
			return GetDatabase().GridFS.FindOne(fileName).ContentType;
		}

		public static bool FileExists(string fileName)
		{
			return GetDatabase().GridFS.FindOne(fileName) != null;
		}

		public static string GetFileName(BsonValue ID)
		{
			return GetDatabase().GridFS.FindOneById(ID).Name;
		}

		public static string GetFilenameByHash(string md5)
		{
			var f = GetDatabase().GridFS.FindOne(new QueryDocument("md5", md5));

			return f == null ? null : f.Name;
		}

	}

}
