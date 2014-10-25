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
		private static MongoDatabase database = null;

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

		public static T Load<T>(IMongoQuery query)
		{
			return GetCollection<T>().FindOneAs<T>(query);
		}

		public static void Save<T>(T t)
		{
			var collection = GetCollection<T>();
			collection.Save(t);
		}

		public static BsonValue StoreFile(Stream stream, string fileName)
		{
			return GetDatabase().GridFS.Upload(stream, fileName).Id;
		}

		public static Stream StoreStream(string fileName)
		{
			return GetDatabase().GridFS.OpenWrite(fileName);
		}

		public static Stream RetrieveFile(string fileName)
		{
			return GetDatabase().GridFS.FindOne(fileName).OpenRead();
		}

		public static string GetFileName(BsonValue ID)
		{
			return GetDatabase().GridFS.FindOneById(ID).Name;
		}

	}

}
