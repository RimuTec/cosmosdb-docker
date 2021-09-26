using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using NUnit.Framework;

namespace CosmosDbTest
{
   /// <summary>
   /// Most source code for the CosmosDb tests from the sample .NET Core app that is shipped with the emulator.
   /// Adjusted naming and coding conventions. [Manfred, 19sep2021]
   /// </summary>
   [TestFixture]
   public class CosmosDbTests
   {
      [OneTimeTearDown]
      public void OneTimeTearDown()
      {
         //Dispose of CosmosClient
         _cosmosClient.Dispose(); // Move this to teardown of fixture
      }

      [Test]
      public void ReadsDevelopmentSettingsCorrectly()
      {
         Assert.AreEqual("https://demo-database.local:8081", EndpointUri);
         Assert.AreEqual("C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==", PrimaryKey);
      }

      [Test]
      public async Task CosmosDemo()
      {
         await GetStartedDemoAsync().ConfigureAwait(false);
      }

      /// <summary>
      /// Entry point to call methods that operate on Azure Cosmos DB resources in this sample
      /// </summary>
      private async Task GetStartedDemoAsync()
      {
         // Create a new instance of the Cosmos Client
         _cosmosClient = new CosmosClient(EndpointUri, PrimaryKey, new CosmosClientOptions() { ApplicationName = "CosmosDBDotnetQuickstart" });
         await CreateDatabaseAsync().ConfigureAwait(false);
         await CreateContainerAsync().ConfigureAwait(false);
         await ScaleContainerAsync().ConfigureAwait(false);
         await AddItemsToContainerAsync().ConfigureAwait(false);
         await QueryItemsAsync().ConfigureAwait(false);
         await ReplaceFamilyItemAsync().ConfigureAwait(false);
         await DeleteFamilyItemAsync().ConfigureAwait(false);
         await DeleteDatabaseAndCleanupAsync().ConfigureAwait(false);
      }

      /// <summary>
      /// Create the database if it does not exist
      /// </summary>
      private async Task CreateDatabaseAsync()
      {
         // Create a new database
         _database = await _cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId).ConfigureAwait(false);
         Console.WriteLine("Created Database: {0}\n", _database.Id);
      }

      /// <summary>
      /// Delete the database and dispose of the Cosmos Client instance
      /// </summary>
      private async Task DeleteDatabaseAndCleanupAsync()
      {
         _ = await _database.DeleteAsync().ConfigureAwait(false);
         // Also valid: await this.cosmosClient.Databases["FamilyDatabase"].DeleteAsync();

         Console.WriteLine("Deleted Database: {0}\n", DatabaseId);
      }

      /// <summary>
      /// Create the container if it does not exist.
      /// Specifiy "/LastName" as the partition key since we're storing family information, to ensure good distribution of requests and storage.
      /// </summary>
      private async Task CreateContainerAsync()
      {
         // Create a new container
         _container = await _database.CreateContainerIfNotExistsAsync(ContainerId, "/LastName", 400).ConfigureAwait(false);
         Console.WriteLine("Created Container: {0}\n", _container.Id);
      }

      /// <summary>
      /// Scale the throughput provisioned on an existing Container.
      /// You can scale the throughput (RU/s) of your container up and down to meet the needs of the workload. Learn more: https://aka.ms/cosmos-request-units
      /// </summary>
      private async Task ScaleContainerAsync()
      {
         // Read the current throughput
         int? throughput = await _container.ReadThroughputAsync().ConfigureAwait(false);
         if (throughput.HasValue)
         {
            Console.WriteLine("Current provisioned throughput : {0}\n", throughput.Value);
            int newThroughput = throughput.Value + 100;
            // Update throughput
            _ = await _container.ReplaceThroughputAsync(newThroughput).ConfigureAwait(false);
            Console.WriteLine("New provisioned throughput : {0}\n", newThroughput);
         }
      }

      /// <summary>
      /// Add Family items to the container
      /// </summary>
      private async Task AddItemsToContainerAsync()
      {
         // Create a family object for the Andersen family
         Family andersenFamily = new()
         {
            Id = "Andersen.1",
            LastName = "Andersen",
            Parents = new Parent[]
            {
               new Parent { FirstName = "Thomas" },
               new Parent { FirstName = "Mary Kay" }
            },
            Children = new Child[]
            {
               new Child
               {
                  FirstName = "Henriette Thaulow",
                  Gender = "female",
                  Grade = 5,
                  Pets = new Pet[]
                  {
                        new Pet { GivenName = "Fluffy" }
                  }
               }
            },
            Address = new Address { State = "WA", County = "King", City = "Seattle" },
            IsRegistered = false
         };

         try
         {
            // Read the item to see if it exists.  
            ItemResponse<Family> andersenFamilyResponse = await _container.ReadItemAsync<Family>(andersenFamily.Id, new PartitionKey(andersenFamily.LastName)).ConfigureAwait(false);
            Console.WriteLine("Item in database with id: {0} already exists\n", andersenFamilyResponse.Resource.Id);
         }
         catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
         {
            // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item, which is "Andersen"
            ItemResponse<Family> andersenFamilyResponse = await _container.CreateItemAsync<Family>(andersenFamily, new PartitionKey(andersenFamily.LastName)).ConfigureAwait(false);

            // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", andersenFamilyResponse.Resource.Id, andersenFamilyResponse.RequestCharge);
         }

         // Create a family object for the Wakefield family
         Family wakefieldFamily = new()
         {
            Id = "Wakefield.7",
            LastName = "Wakefield",
            Parents = new Parent[]
            {
               new Parent { FamilyName = "Wakefield", FirstName = "Robin" },
               new Parent { FamilyName = "Miller", FirstName = "Ben" }
            },
            Children = new Child[]
            {
               new Child
               {
                  FamilyName = "Merriam",
                  FirstName = "Jesse",
                  Gender = "female",
                  Grade = 8,
                  Pets = new Pet[]
                  {
                        new Pet { GivenName = "Goofy" },
                        new Pet { GivenName = "Shadow" }
                  }
               },
               new Child
               {
                  FamilyName = "Miller",
                  FirstName = "Lisa",
                  Gender = "female",
                  Grade = 1
               }
            },
            Address = new Address { State = "NY", County = "Manhattan", City = "NY" },
            IsRegistered = true
         };

         try
         {
            // Read the item to see if it exists
            ItemResponse<Family> wakefieldFamilyResponse = await _container.ReadItemAsync<Family>(wakefieldFamily.Id, new PartitionKey(wakefieldFamily.LastName)).ConfigureAwait(false);
            Console.WriteLine("Item in database with id: {0} already exists\n", wakefieldFamilyResponse.Resource.Id);
         }
         catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
         {
            // Create an item in the container representing the Wakefield family. Note we provide the value of the partition key for this item, which is "Wakefield"
            ItemResponse<Family> wakefieldFamilyResponse = await _container.CreateItemAsync<Family>(wakefieldFamily, new PartitionKey(wakefieldFamily.LastName)).ConfigureAwait(false);

            // Note that after creating the item, we can access the body of the item with the Resource property off the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
            Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", wakefieldFamilyResponse.Resource.Id, wakefieldFamilyResponse.RequestCharge);
         }
      }
      // </AddItemsToContainerAsync>

      // <QueryItemsAsync>
      /// <summary>
      /// Run a query (using Azure Cosmos DB SQL syntax) against the container
      /// Including the partition key value of lastName in the WHERE filter results in a more efficient query
      /// </summary>
      private async Task QueryItemsAsync()
      {
         const string sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";

         Console.WriteLine("Running query: {0}\n", sqlQueryText);

         QueryDefinition queryDefinition = new(sqlQueryText);
         FeedIterator<Family> queryResultSetIterator = _container.GetItemQueryIterator<Family>(queryDefinition);

         List<Family> families = new();

         while (queryResultSetIterator.HasMoreResults)
         {
            FeedResponse<Family> currentResultSet = await queryResultSetIterator.ReadNextAsync().ConfigureAwait(false);
            foreach (Family family in currentResultSet)
            {
               families.Add(family);
               Console.WriteLine("\tRead {0}\n", family);
            }
         }
      }
      // </QueryItemsAsync>

      // <ReplaceFamilyItemAsync>
      /// <summary>
      /// Replace an item in the container
      /// </summary>
      private async Task ReplaceFamilyItemAsync()
      {
         ItemResponse<Family> wakefieldFamilyResponse = await _container.ReadItemAsync<Family>("Wakefield.7", new PartitionKey("Wakefield")).ConfigureAwait(false);
         var itemBody = wakefieldFamilyResponse.Resource;

         // update registration status from false to true
         itemBody.IsRegistered = true;
         // update grade of child
         itemBody.Children[0].Grade = 6;

         // replace the item with the updated content
         wakefieldFamilyResponse = await _container.ReplaceItemAsync<Family>(itemBody, itemBody.Id, new PartitionKey(itemBody.LastName)).ConfigureAwait(false);
         Console.WriteLine("Updated Family [{0},{1}].\n \tBody is now: {2}\n", itemBody.LastName, itemBody.Id, wakefieldFamilyResponse.Resource);
      }
      // </ReplaceFamilyItemAsync>

      // <DeleteFamilyItemAsync>
      /// <summary>
      /// Delete an item in the container
      /// </summary>
      private async Task DeleteFamilyItemAsync()
      {
         const string partitionKeyValue = "Wakefield";
         const string familyId = "Wakefield.7";

         // Delete an item. Note we must provide the partition key value and id of the item to delete
         _ = await _container.DeleteItemAsync<Family>(familyId, new PartitionKey(partitionKeyValue)).ConfigureAwait(false);
         Console.WriteLine("Deleted Family [{0},{1}]\n", partitionKeyValue, familyId);
      }
      // </DeleteFamilyItemAsync>

      private CosmosClient _cosmosClient;
      private static readonly string EndpointUri = TestSuite.Configuration["CosmosDb:EndPointUri"];
      private static readonly string PrimaryKey = TestSuite.Configuration["CosmosDb:PrimaryKey"];
      private Database _database;
      private readonly string DatabaseId = "db";
      private readonly string ContainerId = "items";
      private Container _container;
   }

   public class Family
   {
      [JsonProperty(PropertyName = "id")]
      public string Id { get; set; }
      public string LastName { get; set; }
      public Parent[] Parents { get; set; }
      public Child[] Children { get; set; }
      public Address Address { get; set; }
      public bool IsRegistered { get; set; }
      public override string ToString()
      {
         return JsonConvert.SerializeObject(this);
      }
   }

   public class Parent
   {
      public string FamilyName { get; set; }
      public string FirstName { get; set; }
   }

   public class Child
   {
      public string FamilyName { get; set; }
      public string FirstName { get; set; }
      public string Gender { get; set; }
      public int Grade { get; set; }
      public Pet[] Pets { get; set; }
   }

   public class Pet
   {
      public string GivenName { get; set; }
   }

   public class Address
   {
      public string State { get; set; }
      public string County { get; set; }
      public string City { get; set; }
   }
}
