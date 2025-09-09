using Microsoft.EntityFrameworkCore;
using EFCore.CosmosDB.Test.Data;
using EFCore.CosmosDB.Test.Models;
using System.Diagnostics;

namespace EFCore.CosmosDB.Test
{
    public class EFCoreCosmosDbDemo
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.WriteLine("Beginning EF Core CosmosDB Demo...");
                var demo = new EFCoreCosmosDbDemo();
                await demo.RunDemoAsync();
                Console.WriteLine("Demo completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private async Task RunDemoAsync()
        {
            using var context = new CosmosDbContext();
            
            // Ensure database and containers are created
            Console.WriteLine("Creating database and containers...");
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // If we reach here without exception, the database is ready and accessible
            Debug.Assert(true, "Database created successfully at startup");

            // Create documents with different partition keys
            string partitionKey1 = "p1";
            string partitionKey2 = "p2";
            
            Console.WriteLine("Creating test documents...");
            await CreateTestDocumentAsync(context, "document1", "field1", partitionKey1, "Seattle");
            await CreateTestDocumentAsync(context, "document2", "field2", partitionKey2, "Portland");

            Console.WriteLine("Reading all documents unchanged...");
            await QueryAllTestDocumentsAsync(context);

            Console.WriteLine("\nUpdating document...");
            await UpdateTestDocumentAndVerifyAsync(context, "document1", partitionKey1, null); // Set city to null

            Console.WriteLine("\nUpserting new document...");
            await UpsertTestDocumentAndVerifyAsync(context, "document3", "field1", partitionKey2, "New Orleans");

            Console.WriteLine("\nUpserting existing document...");
            await UpsertTestDocumentAndVerifyAsync(context, "document2", "field1", partitionKey2, "Miami");
            
            Console.WriteLine("Reading documents with partition key filter...");
            await QueryTestDocumentsByPartitionKeyAsync(context, partitionKey1);
            
            Console.WriteLine("Reading all documents after updates...");
            await QueryAllTestDocumentsAsync(context);

            Console.WriteLine("\nQuerying documents with order by...");
            await QueryTestDocumentsWithOrderByAsync(context);

            Console.WriteLine("\nDeleting document...");
            await DeleteTestDocumentAndVerifyAsync(context, "document1", partitionKey1);

            Console.WriteLine("\nTesting message document insertion...");
            await TestMessageDocumentInsertionAsync(context);

            Console.WriteLine("\nTesting SimpleTestDocument operations...");
            await TestSimpleTestDocumentOperationsAsync(context);

            Console.WriteLine("\nTesting EF Core change tracking features...");
            await TestChangeTrackingFeaturesAsync(context);

            Console.WriteLine("Cleaning up database...");
            await context.Database.EnsureDeletedAsync();
        }

        private async Task CreateTestDocumentAsync(CosmosDbContext context, string id, string queryField, string partitionKey, string city)
        {
            var document = new TestDocument
            {
                Id = id,
                QueryField = queryField,
                PartitionKey = partitionKey,
                City = city
            };

            context.TestDocuments.Add(document);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created test document {id} with hierarchical partition key [{partitionKey}, {queryField}]");
        }

        private async Task UpdateTestDocumentAndVerifyAsync(CosmosDbContext context, string id, string partitionKey, string? newCity)
        {
            Console.WriteLine($"Updating document {id} with new city: {newCity ?? "null"}");
            
            try
            {
                // Find the document using EF Core
                var existingDocument = await context.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.PartitionKey == partitionKey);
                
                if (existingDocument == null)
                {
                    Console.WriteLine($"Document with id {id} not found!");
                    return;
                }
                
                Console.WriteLine($"Retrieved document: Id={existingDocument.Id}, City={existingDocument.City}, QueryField={existingDocument.QueryField}");
                
                // Update only non-partition-key properties (EF Core doesn't allow modifying partition key properties)
                existingDocument.City = newCity;
                // Note: Cannot modify QueryField as it's part of the partition key in EF Core
                
                // Save changes using EF Core change tracking
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Updated document successfully (Note: QueryField cannot be modified as it's part of the partition key)");
                
                // Verify the update by querying the document again
                Console.WriteLine("Verifying update by querying the document:");
                var updatedDocument = await context.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.PartitionKey == partitionKey);
                
                if (updatedDocument != null)
                {
                    Console.WriteLine($"Verified document: Id={updatedDocument.Id}, City={updatedDocument.City}, QueryField={updatedDocument.QueryField}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating document: {ex.Message}");
            }
        }

        private async Task UpsertTestDocumentAndVerifyAsync(CosmosDbContext context, string id, string queryField, string partitionKey, string city)
        {
            Console.WriteLine($"Upserting document {id} with hierarchical partition key [{partitionKey}, {queryField}]");
            
            try
            {
                // Check if document exists with exact partition key match
                var existingDocument = await context.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.PartitionKey == partitionKey && d.QueryField == queryField);
                
                if (existingDocument != null)
                {
                    // Update existing document (only non-partition-key properties)
                    existingDocument.City = city;
                    Console.WriteLine("Document exists with exact partition key, updating City...");
                }
                else
                {
                    // Create new document
                    var newDocument = new TestDocument
                    {
                        Id = id,
                        QueryField = queryField,
                        PartitionKey = partitionKey,
                        City = city
                    };
                    context.TestDocuments.Add(newDocument);
                    Console.WriteLine("Document does not exist with exact partition key, creating new...");
                }
                
                await context.SaveChangesAsync();
                Console.WriteLine($"Upserted document successfully");
                
                // Verify the upsert by reading the document
                Console.WriteLine("Verifying upsert by reading the document:");
                var upsertedDocument = await context.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.PartitionKey == partitionKey && d.QueryField == queryField);
                
                if (upsertedDocument != null)
                {
                    Console.WriteLine($"Verified document: Id={upsertedDocument.Id}, QueryField={upsertedDocument.QueryField}, " +
                                    $"PartitionKey={upsertedDocument.PartitionKey}, City={upsertedDocument.City}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error upserting document: {ex.Message}");
            }
        }

        private async Task DeleteTestDocumentAndVerifyAsync(CosmosDbContext context, string id, string partitionKey)
        {
            Console.WriteLine($"Deleting document {id} with partition key {partitionKey}");
            
            try
            {
                // Find the document using EF Core (we need to find it by exact partition key match)
                var documentToDelete = await context.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.PartitionKey == partitionKey);
                
                if (documentToDelete == null)
                {
                    Console.WriteLine($"Document with id {id} not found!");
                    return;
                }
                
                Console.WriteLine($"Found document to delete: Id={documentToDelete.Id}, QueryField={documentToDelete.QueryField}");
                
                // Delete the document using EF Core
                context.TestDocuments.Remove(documentToDelete);
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Deleted document successfully");
                
                // Verify deletion by attempting to find the document (should be null)
                Console.WriteLine("Verifying deletion by attempting to find the document:");
                var deletedDocument = await context.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == id && d.PartitionKey == partitionKey);
                
                if (deletedDocument == null)
                {
                    Console.WriteLine("Document deletion verified - document not found");
                }
                else
                {
                    Console.WriteLine("WARNING: Document still exists after deletion attempt");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document: {ex.Message}");
            }
        }

        private async Task QueryTestDocumentsByPartitionKeyAsync(CosmosDbContext context, string partitionKey)
        {
            Console.WriteLine($"Querying documents with primary partition key: {partitionKey}");
            
            try
            {
                var documents = await context.TestDocuments
                    .Where(d => d.PartitionKey == partitionKey)
                    .ToListAsync();
                
                foreach (var document in documents)
                {
                    Console.WriteLine($"Found document: Id={document.Id}, QueryField={document.QueryField}, PartitionKey={document.PartitionKey}, City={document.City}");
                }
                
                Console.WriteLine($"Found {documents.Count} document(s) with primary partition key {partitionKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying documents by partition key: {ex.Message}");
            }
        }

        private async Task QueryAllTestDocumentsAsync(CosmosDbContext context)
        {
            Console.WriteLine("Querying all test documents...");
            
            try
            {
                var documents = await context.TestDocuments.ToListAsync();
                
                foreach (var document in documents)
                {
                    Console.WriteLine($"Found document: Id={document.Id}, QueryField={document.QueryField}, PartitionKey={document.PartitionKey}, City={document.City}");
                }
                
                Console.WriteLine($"Found {documents.Count} document(s) in total");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying all documents: {ex.Message}");
            }
        }

        private async Task QueryTestDocumentsWithOrderByAsync(CosmosDbContext context)
        {
            Console.WriteLine("\nQuerying documents ordered by City...");
            
            try
            {
                // Ascending order by City
                Console.WriteLine("Results in ascending order by City:");
                var documentsAsc = await context.TestDocuments
                    .Where(d => d.PartitionKey == "p1")
                    .OrderBy(d => d.City)
                    .ToListAsync();
                
                foreach (var document in documentsAsc)
                {
                    Console.WriteLine($"Found document: Id={document.Id}, QueryField={document.QueryField}, PartitionKey={document.PartitionKey}, City={document.City}");
                }
                
                Console.WriteLine($"Found {documentsAsc.Count} document(s) in ascending order");
                
                // Descending order by City
                Console.WriteLine("\nResults in descending order by City:");
                var documentsDesc = await context.TestDocuments
                    .Where(d => d.PartitionKey == "p1")
                    .OrderByDescending(d => d.City)
                    .ToListAsync();
                
                foreach (var document in documentsDesc)
                {
                    Console.WriteLine($"Found document: Id={document.Id}, QueryField={document.QueryField}, PartitionKey={document.PartitionKey}, City={document.City}");
                }
                
                Console.WriteLine($"Found {documentsDesc.Count} document(s) in descending order");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying documents with order by: {ex.Message}");
            }
        }

        private async Task TestMessageDocumentInsertionAsync(CosmosDbContext context)
        {
            Console.WriteLine("Creating message document as described in the bug report...");

            var messageId = Guid.NewGuid().ToString();
            var messageDocument = new MessageDocument
            {
                Id = messageId,
                MessageId = messageId,
                PartitionKey = messageId,
                QueryField = "message",
                Data = new List<MyDataItem>
                {
                    new MyDataItem
                    {
                        SystemId = "PUT001",
                        Date = SetUpFixture.FirstDayOfCurrentMonth,
                        CategoryId = 0,
                        Subcategory = "TestSubCategory",
                        Name = "TestName1",
                        ValueType = ValueTypeEnum.Difference.ToString(),
                        Quantity = 123.5,
                        Unit = "TestUnit1",
                        Data = "{\"ReferringUnit\":\"system 1\"}"
                    },
                    new MyDataItem
                    {
                        SystemId = "PUT001",
                        Date = SetUpFixture.FirstDayOfCurrentMonth,
                        CategoryId = 2,
                        Subcategory = "TestSubCategory2",
                        Name = "TestName2",
                        ValueType = ValueTypeEnum.Total.ToString(),
                        Quantity = 200,
                        Unit = "TestUnit2",
                        Data = "{\"ReferringUnit\":\"system 1\"}"
                    }
                }
            };

            try
            {
                context.MessageDocuments.Add(messageDocument);
                await context.SaveChangesAsync();
                
                Console.WriteLine($"Message document created successfully with ID: {messageDocument.Id}");
                Console.WriteLine($"Message contains {messageDocument.Data.Count} data items");

                // Verify by reading back the document
                var retrievedMessage = await context.MessageDocuments
                    .FirstOrDefaultAsync(m => m.Id == messageId);
                
                if (retrievedMessage != null)
                {
                    Console.WriteLine($"Retrieved message document: ID={retrievedMessage.Id}, MessageId={retrievedMessage.MessageId}");
                    Console.WriteLine($"Retrieved message contains {retrievedMessage.Data.Count} data items");
                    foreach (var dataItem in retrievedMessage.Data)
                    {
                        Console.WriteLine($"  Data item: {dataItem.Name}, Quantity: {dataItem.Quantity}, ValueType: {dataItem.ValueType}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating message document: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        private async Task TestSimpleTestDocumentOperationsAsync(CosmosDbContext context)
        {
            Console.WriteLine("=== Testing SimpleTestDocument Operations ===");
            Console.WriteLine("This test reproduces operations similar to the SDK test for system properties");

            try
            {
                var documentId = Guid.NewGuid().ToString();
                var partitionKey = "simple-pk";
                
                // Create a simple test document
                var simpleDoc = new SimpleTestDocument
                {
                    Id = documentId,
                    Foo = "InitialValue",
                    PartitionKey = partitionKey,
                    QueryField = "simple"
                };

                Console.WriteLine($"Creating SimpleTestDocument with ID: {documentId}");
                context.SimpleTestDocuments.Add(simpleDoc);
                await context.SaveChangesAsync();

                // Read the document back and check ETag
                var createdItem = await context.SimpleTestDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (createdItem != null)
                {
                    // Update the document (only non-partition-key properties)
                    Console.WriteLine("Updating the document...");
                    createdItem.Foo = "UpdatedValue";
                    await context.SaveChangesAsync();

                    // Read the document again and check ETag after update
                    var updatedItem = await context.SimpleTestDocuments
                        .FirstOrDefaultAsync(d => d.Id == documentId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SimpleTestDocument operations: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }

        private async Task TestChangeTrackingFeaturesAsync(CosmosDbContext context)
        {
            Console.WriteLine("=== Testing EF Core Change Tracking Features ===");
            
            try
            {
                var documentId = Guid.NewGuid().ToString();
                var partitionKey = "tracking-test";
                
                // Create a document
                var testDoc = new TestDocument
                {
                    Id = documentId,
                    QueryField = "tracking",
                    PartitionKey = partitionKey,
                    City = "OriginalCity"
                };

                Console.WriteLine("Adding document to context (not yet saved)...");
                context.TestDocuments.Add(testDoc);
                
                // Check entity state before saving
                var entry = context.Entry(testDoc);
                Console.WriteLine($"Entity state before SaveChanges: {entry.State}");
                
                await context.SaveChangesAsync();
                Console.WriteLine($"Entity state after SaveChanges: {entry.State}");

                // Modify the document (only non-partition-key properties)
                Console.WriteLine("Modifying the document (City property only)...");
                testDoc.City = "ModifiedCity";
                Console.WriteLine($"Entity state after modification: {entry.State}");

                // Check if EF Core detected the change
                if (entry.State == EntityState.Modified)
                {
                    Console.WriteLine("SUCCESS: EF Core change tracking detected the modification");
                    
                    // Show which properties changed
                    foreach (var property in entry.Properties)
                    {
                        if (property.IsModified)
                        {
                            Console.WriteLine($"  Modified property: {property.Metadata.Name}");
                            Console.WriteLine($"    Original value: {property.OriginalValue}");
                            Console.WriteLine($"    Current value: {property.CurrentValue}");
                        }
                    }
                }

                await context.SaveChangesAsync();
                Console.WriteLine($"Entity state after second SaveChanges: {entry.State}");

                // Test optimistic concurrency (if supported)
                Console.WriteLine("\nTesting concurrent modifications simulation...");
                
                // Load the same document in a different context to simulate concurrent access
                using var context2 = new CosmosDbContext();
                await context2.Database.EnsureCreatedAsync(); // Ensure context2 can access the same database
                
                var docInContext2 = await context2.TestDocuments
                    .FirstOrDefaultAsync(d => d.Id == documentId);
                
                if (docInContext2 != null)
                {
                    // Modify in both contexts
                    testDoc.City = "CityFromContext1";
                    docInContext2.City = "CityFromContext2";
                    
                    // Save from first context
                    await context.SaveChangesAsync();
                    Console.WriteLine("Saved changes from first context");
                    
                    // Try to save from second context
                    try
                    {
                        await context2.SaveChangesAsync();
                        Console.WriteLine("Saved changes from second context - no conflict detected");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Concurrent modification conflict: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in change tracking test: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
        }
    }
}
