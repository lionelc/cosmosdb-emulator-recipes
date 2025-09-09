using Microsoft.EntityFrameworkCore;
using EFCore.CosmosDB.Test.Models;
using System.Net;

namespace EFCore.CosmosDB.Test.Data
{
    public class CosmosDbContext : DbContext
    {
        // Update these constants with your own Cosmos DB endpoint and key
        private const string EndpointUrl = "http://localhost:8081";  // Use your actual endpoint and match protocol (http/https)
        private const string PrimaryKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public DbSet<TestDocument> TestDocuments { get; set; }
        public DbSet<MessageDocument> MessageDocuments { get; set; }
        public DbSet<SimpleTestDocument> SimpleTestDocuments { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Create a unique database name for each test run
                string databaseName = $"efcore-test-{Guid.NewGuid():N}";
                
                optionsBuilder.UseCosmos(
                    EndpointUrl,
                    PrimaryKey,
                    databaseName,
                    options =>
                    {
                        options.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);
                        options.HttpClientFactory(() => new HttpClient(new HttpClientHandler
                        {
                            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                        }));
                    });

                // Enable sensitive data logging for debugging
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
                
                // Add console logging
                optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure TestDocument
            modelBuilder.Entity<TestDocument>(entity =>
            {
                entity.ToContainer("TestDocuments");
                entity.HasPartitionKey(e => new { e.PartitionKey, e.QueryField });
                entity.Property(e => e.Id).ToJsonProperty("id");
                entity.Property(e => e.PartitionKey).ToJsonProperty("pk");
                entity.Property(e => e.QueryField).ToJsonProperty("queryfield");
                entity.Property(e => e.City).ToJsonProperty("city");
            });

            // Configure MessageDocument
            modelBuilder.Entity<MessageDocument>(entity =>
            {
                entity.ToContainer("MessageDocuments");
                entity.HasPartitionKey(e => new { e.PartitionKey, e.QueryField });
                entity.Property(e => e.Id).ToJsonProperty("id");
                entity.Property(e => e.MessageId).ToJsonProperty("messageId");
                entity.Property(e => e.PartitionKey).ToJsonProperty("pk");
                entity.Property(e => e.QueryField).ToJsonProperty("queryfield");
                entity.OwnsMany(e => e.Data, data =>
                {
                    data.ToJsonProperty("data");
                    data.Property(d => d.SystemId).ToJsonProperty("systemId");
                    data.Property(d => d.Date).ToJsonProperty("date");
                    data.Property(d => d.CategoryId).ToJsonProperty("categoryId");
                    data.Property(d => d.Subcategory).ToJsonProperty("subcategory");
                    data.Property(d => d.Name).ToJsonProperty("name");
                    data.Property(d => d.ValueType).ToJsonProperty("valueType");
                    data.Property(d => d.Quantity).ToJsonProperty("quantity");
                    data.Property(d => d.Unit).ToJsonProperty("unit");
                    data.Property(d => d.Data).ToJsonProperty("data");
                });
            });

            // Configure SimpleTestDocument
            modelBuilder.Entity<SimpleTestDocument>(entity =>
            {
                entity.ToContainer("SimpleTestDocuments");
                entity.HasPartitionKey(e => new { e.PartitionKey, e.QueryField });
                entity.Property(e => e.Id).ToJsonProperty("id");
                entity.Property(e => e.Foo).ToJsonProperty("foo");
                entity.Property(e => e.PartitionKey).ToJsonProperty("pk");
                entity.Property(e => e.QueryField).ToJsonProperty("queryfield");
                entity.Property(e => e.ETag).ToJsonProperty("_etag");
            });
        }
    }
}
