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

namespace WSS
{
	public class Database
	{
		private readonly MongoDatabase database = new MongoClient().GetServer()
			.GetDatabase(Assembly.GetEntryAssembly().GetName().Name);

		public int GetNextValueFromSequence(string sequenceName)
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


		private MongoCollection GetCollection<T>()
		{
			return GetCollection(typeof(T));
		}

		private MongoCollection GetCollection(Type type)
		{
			return GetCollection(type.Name);
		}

		private MongoCollection GetCollection(string name)
		{
			return database.GetCollection(name);
		}

		public T Load<T>(BsonValue id)
		{
			return GetCollection<T>().FindOneByIdAs<T>(id);
		}

		public T LoadByObjectID<T>(string objectID)
		{
			return GetCollection<T>().FindOneByIdAs<T>(new BsonObjectId(new ObjectId(objectID)));
		}


		public T LoadBy<T>(string field, BsonValue value)
		{
			return GetCollection<T>().FindOneAs<T>(new QueryDocument(field, value));
		}

		public T Load<T>(IMongoQuery query)
		{
			return GetCollection<T>().FindOneAs<T>(query);
		}

		public void Save<T>(T t)
		{
			var collection = GetCollection<T>();
			collection.Save(t);
		}

		public void Save(object t)
		{
			var collection = GetCollection(t.GetType());
			collection.Save(t);
		}

		public void StoreFile(Stream stream, string fileName, string ContentType)
		{
			MongoGridFSCreateOptions options = new MongoGridFSCreateOptions();
			options.ContentType = ContentType;
			database.GridFS.Upload(stream, fileName, options);
		}

		public void StoreFile(Stream stream, string fileName)
		{
			StoreFile(stream, fileName, "application/octet-stream");
		}

		public Stream StoreStream(string fileName, string ContentType)
		{
			MongoGridFSCreateOptions options = new MongoGridFSCreateOptions();
			options.ContentType = ContentType;
			return database.GridFS.OpenWrite(fileName);
		}

		public Stream StoreStream(string fileName)
		{
			return StoreStream(fileName, "application/octet-stream");
		}

		public Stream RetrieveFile(string fileName)
		{
			return database.GridFS.FindOne(fileName).OpenRead();
		}

		public long GetFileSize(string fileName)
		{
			return database.GridFS.FindOne(fileName).Length;
		}

		public string GetFileType(string fileName)
		{
			return database.GridFS.FindOne(fileName).ContentType;
		}

		public bool FileExists(string fileName)
		{
			return database.GridFS.FindOne(fileName) != null;
		}

		public string GetFileName(BsonValue ID)
		{
			return database.GridFS.FindOneById(ID).Name;
		}

		public string GetFilenameByHash(string md5)
		{
			var f = database.GridFS.FindOne(new QueryDocument("md5", md5));

			return f == null ? null : f.Name;
		}

	}

}
