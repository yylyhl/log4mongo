﻿using log4net.Appender;
using log4net.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Log4Mongo
{
	public class MongoDBAppender : AppenderSkeleton
	{
		private readonly List<MongoAppenderFileld> _fields = new List<MongoAppenderFileld>();

		/// <summary>
		/// MongoDB database connection in the format:
		/// mongodb://[username:password@]host1[:port1][,host2[:port2],...[,hostN[:portN]]][/[database][?options]]
		/// See https://www.mongodb.org/display/DOCS/Connections
		/// If no database specified, default to "log4net"
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// The connectionString name to use in the connectionStrings section of your *.config file
		/// If not specified or connectionString name does not exist will use ConnectionString value
		/// </summary>
		public string ConnectionStringName { get; set; }

        /// <summary>
        /// Name of the collection in database
        /// Defaults to "logs"
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// The Friendly Name of the certificate. This value will be used if SSL is set to true
        /// The default StoreLocation is LocalMachine and StoreName is My
        /// </summary>
        public string CertificateFriendlyName { get; set; }

		/// <summary>
		/// If set, create a TTL index to expire after specified number of seconds
		/// </summary>
		public long ExpireAfterSeconds { get; set; }

		/// <summary>
		/// Maximum number of documents in collection
		/// See https://docs.mongodb.org/manual/core/capped-collections/
		/// </summary>
		public string NewCollectionMaxDocs { get; set; }

		/// <summary>
		/// Maximum size of collection
		/// See https://docs.mongodb.org/manual/core/capped-collections/
		/// </summary>
		public string NewCollectionMaxSize { get; set; }

		public void AddField(MongoAppenderFileld fileld)
		{
			_fields.Add(fileld);
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			var collection = GetCollection();
			collection.InsertOneAsync(BuildBsonDocument(loggingEvent));
			CreateExpiryAfterIndex(collection);
		}

		protected override void Append(LoggingEvent[] loggingEvents)
		{
			var collection = GetCollection();
			collection.InsertManyAsync(loggingEvents.Select(BuildBsonDocument));
			CreateExpiryAfterIndex(collection);
		}

		private IMongoCollection<BsonDocument> GetCollection()
		{
			var db = GetDatabase();
			var collectionName = CollectionName ?? "logs";
            if (collectionName.EndsWith("yyyyMM"))
            {
                var prefix = collectionName.Split("yyyyMM")[0];
                if (string.IsNullOrWhiteSpace(prefix)) prefix = "logs";
                prefix = prefix.Replace("%", string.Empty);
                collectionName = prefix + DateTime.Now.ToString("yyyyMM");
            }
            else if (collectionName.EndsWith("yyyyMMdd"))
            {
                var prefix = collectionName.Split("yyyyMMdd")[0];
                if (string.IsNullOrWhiteSpace(prefix)) prefix = "logs";
                prefix = prefix.Replace("%", string.Empty);
                collectionName = prefix + DateTime.Now.ToString("yyyyMMdd");
            }

            EnsureCollectionExists(db, collectionName);

			var collection = db.GetCollection<BsonDocument>(collectionName);
			return collection;
		}

		private void EnsureCollectionExists(IMongoDatabase db, string collectionName)
		{
			if (!CollectionExists(db, collectionName))
			{
				CreateCollection(db, collectionName);
			}
		}

		private bool CollectionExists(IMongoDatabase db, string collectionName)
		{
			var filter = new BsonDocument("name", collectionName);
			return db.ListCollections(new ListCollectionsOptions { Filter = filter }).Any();
		}

		private void CreateCollection(IMongoDatabase db, string collectionName)
		{
			var cob = new CreateCollectionOptions();

			SetCappedCollectionOptions(cob);

			db.CreateCollectionAsync(collectionName, cob);
		}

		private void SetCappedCollectionOptions(CreateCollectionOptions options)
		{
			var newCollectionMaxSize = UnitResolver.Resolve(NewCollectionMaxSize);
			var newCollectionMaxDocs = UnitResolver.Resolve(NewCollectionMaxDocs);

            if (newCollectionMaxSize <= 0) return;

            options.Capped = true;
            options.MaxSize = newCollectionMaxSize;
            if (newCollectionMaxDocs > 0)
            {
                options.MaxDocuments = newCollectionMaxDocs;
            }
        }

		private string GetConnectionString()
		{
			ConnectionStringSettings connectionStringSetting = ConfigurationManager.ConnectionStrings[ConnectionStringName];
			return connectionStringSetting != null ? connectionStringSetting.ConnectionString : ConnectionString;
		}

		private IMongoDatabase GetDatabase()
		{
			string connStr = GetConnectionString();

			if (string.IsNullOrWhiteSpace(connStr))
			{
				throw new InvalidOperationException("Must provide a valid connection string");
			}

			var url = MongoUrl.Create(connStr);
			var settings = MongoClientSettings.FromUrl(url);
			settings.SslSettings = url.UseTls ? GetSslSettings() : null;
            return new MongoClient(settings).GetDatabase(url.DatabaseName ?? "log4net");
		}

		private SslSettings GetSslSettings()
		{
			SslSettings sslSettings = null;

			if (!string.IsNullOrEmpty(CertificateFriendlyName))
			{
				X509Certificate2 certificate = GetCertificate(CertificateFriendlyName);

				if (null != certificate)
				{
                    sslSettings = new SslSettings
                    {
                        ClientCertificates = new List<X509Certificate2>() { certificate }
                    };
                }
			}

			return sslSettings;
		}

		private X509Certificate2 GetCertificate(string certificateFriendlyName)
		{
			X509Certificate2 certificateToReturn = null;
			X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
			store.Open(OpenFlags.ReadOnly);

			var certificates = store.Certificates;

			foreach (X509Certificate2 certificate in certificates)
			{
				if (certificate.FriendlyName.Equals(certificateFriendlyName))
				{
					certificateToReturn = certificate;
					break;
				}
			}

			store.Close();

			return certificateToReturn;
		}

		private BsonDocument BuildBsonDocument(LoggingEvent log)
		{
			if (_fields.Count == 0)
			{
				return BackwardCompatibility.BuildBsonDocument(log);
			}
			var doc = new BsonDocument();
			foreach (MongoAppenderFileld field in _fields)
			{
                object value = field.Layout.Format(log);
                // if the object is complex and can't be mapped to a simple object, convert to bson document
                if (!BsonTypeMapper.TryMapToBsonValue(value, out BsonValue bsonValue))
                {
                    bsonValue = value.ToBsonDocument();
                }
                doc.Add(field.Name, bsonValue);
            }
			return doc;
		}

		private void CreateExpiryAfterIndex(IMongoCollection<BsonDocument> collection)
        {
            if (ExpireAfterSeconds <= 0) return;
            collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending((FieldDefinition<BsonDocument>)"timestamp"), new CreateIndexOptions()
            {
                Name = "expireAfterSecondsIndex",
                ExpireAfter = new TimeSpan?(new TimeSpan(ExpireAfterSeconds * 10000000L))
            }), null, new System.Threading.CancellationToken());
        }
	}
}