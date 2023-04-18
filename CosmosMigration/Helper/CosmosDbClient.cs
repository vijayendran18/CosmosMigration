using Azure;
using Azure.Core;
using Azure.Identity;
using CRUD.Models;
using Microsoft.Azure.Cosmos;
using Document = Microsoft.Azure.Documents.Document;
//using Microsoft.Azure.Management.ContainerInstance.Fluent.Models;
using System;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;

namespace CRUD.Helper
{
    public class CosmosDbClient
    {
        public CosmosClient _cosmosClient { get; set; }
        public Database _database { get; set; }
        public Container _container { get; set; }
        public CosmosDbClient(CosmosConfiguration CosmosConfiguration)
        {
            var s = Initialize().IsCompletedSuccessfully;

            async Task<CosmosDbClient> Initialize()
            {

                try
                {
                    CosmosClientOptions cosmosClientOptions = new CosmosClientOptions()
                    {
                        //ConnectionMode = ConnectionMode.Direct,
                        AllowBulkExecution = true,
                        ////MaxRequestsPerTcpConnection = 1000,
                        ////GatewayModeMaxConnectionLimit = 1024,
                        //MaxRetryAttemptsOnRateLimitedRequests = 10,
                        //MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                        ////RequestTimeout = TimeSpan.FromSeconds(10),
                    };

                    //----------------Primary/secondary keys	
                    _cosmosClient = new CosmosClient(CosmosConfiguration.Endpoint, CosmosConfiguration.Key, cosmosClientOptions);
                    _database = _cosmosClient.CreateDatabaseIfNotExistsAsync(CosmosConfiguration.Database).Result;

                    if (CosmosConfiguration.ThroughPut > 0)
                    {
                        _container = _database.DefineContainer(CosmosConfiguration.Container, "/id")
                                            .WithIndexingPolicy()
                                                .WithIndexingMode(IndexingMode.Consistent)
                                                .WithIncludedPaths()
                                                    .Attach()
                                                .WithExcludedPaths()
                                                    .Path("/*")
                                                    .Attach()
                                            .Attach()
                                        .CreateIfNotExistsAsync(CosmosConfiguration.ThroughPut).Result;
                    }
                    else
                    {
                        _container = _database.CreateContainerIfNotExistsAsync(new ContainerProperties()
                        {
                            Id = CosmosConfiguration.Container,
                            PartitionKeyPath = "/id"
                        }).Result;
                    }

                }
                catch (CosmosException ex)
                {
                    // Handle Cosmos DB exception
                    Console.WriteLine($"CosmosException: {ex.StatusCode} - {ex.Message}");
                    throw;
                }
                catch (Exception ex)
                {
                    // Handle general exception
                    Console.WriteLine($"Exception: {ex.Message}");
                    throw;
                }

                return this;

            }

        }

        public async Task<List<T>> getAllItemsAsync<T>(int maxItemCount = 1000)
        {
            Console.WriteLine($"Container [{_container.Id}] - getAllItemsAsync");

            var list = new List<T>();

            try
            {
                var query = new QueryDefinition($"SELECT * FROM c ORDER BY c._ts ASC");
                var queryRequestOptions = new QueryRequestOptions()
                {
                    MaxItemCount = maxItemCount, // Maximum number of items to retrieve per query
                };

                var queryIterator = _container.GetItemQueryIterator<T>(query, requestOptions: queryRequestOptions);

                int totalRecords = 0;
                while (queryIterator.HasMoreResults)
                {
                    var queryResult = await queryIterator.ReadNextAsync();
                    totalRecords += queryResult.Count;
                    Console.WriteLine($"Loaded {totalRecords} item(s)");
                    list.AddRange(queryResult.ToList());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in getAllItemsAsync - {ex.Message}");
            }

            return list;

        }

        public async Task<List<T>> getItemsByQueryAsync<T>(string query, int maxItemCount = 1000)
        {
            Console.WriteLine($"Container [{_container.Id}] - getAllItemsAsync");

            var list = new List<T>();

            try
            {
                var queryDef = new QueryDefinition(query);
                var queryRequestOptions = new QueryRequestOptions()
                {
                    MaxItemCount = maxItemCount, // Maximum number of items to retrieve per query
                };

                var queryIterator = _container.GetItemQueryIterator<T>(queryDef, requestOptions: queryRequestOptions);

                int totalRecords = 0;
                while (queryIterator.HasMoreResults)
                {
                    var queryResult = await queryIterator.ReadNextAsync();
                    totalRecords += queryResult.Count;
                    Console.WriteLine($"Loaded {totalRecords} item(s)");
                    list.AddRange(queryResult.ToList());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in getItemsByQueryAsync - {ex.Message}");
            }

            return list;

        }

        /// <summary>
        /// Delete all documents
        /// </summary>
        /// <typeparam name="DocumentModel"></typeparam>
        /// <returns></returns>
        public async Task DeleteAllItemAsync(int maxRetry = 3, int retryDelayMs = 1000)
        {
            Console.WriteLine($"Container [{_container.Id}] - DeleteAllItemAsync");
            int retries = 0;

            while (retries < maxRetry)
            {
                try
                {
                    var list = new List<Document>();
                    string query = "SELECT * FROM c";
                    FeedIterator<Document> queryIterator = _container.GetItemQueryIterator<Document>(query);
                    while (queryIterator.HasMoreResults)
                    {
                        var queryResult = await queryIterator.ReadNextAsync();
                        list.AddRange(queryResult.ToList());
                    }

                    if (list.Count > 0)
                    {
                        var tasks = list.Select(item => _container.DeleteItemAsync<Document>(item.Id, new PartitionKey(item.Id)));
                        await Task.WhenAll(tasks);
                    }

                    Console.WriteLine($"{list.Count} records deleted from {_container.Id} container");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in delete {_container.Id} items : {ex.Message}");
                    retries++;
                    if (retries < maxRetry)
                    {
                        int delayMs = retries * retryDelayMs;
                        Console.WriteLine($"Retry Wait Ms : {delayMs}");
                        await Task.Delay(delayMs);
                        Console.WriteLine($"Retry attempt : {retries}");
                    }
                    else
                    {
                        Console.WriteLine($"Max retries reached.");
                    }
                }
            }
        }


    }


    public class SourceCosmosClient : CosmosDbClient
    {
        public SourceCosmosClient(CosmosConfiguration CosmosConfiguration) : base(CosmosConfiguration) { }
    }

    public class DestinationCosmosClient : CosmosDbClient
    {
        public DestinationCosmosClient(CosmosConfiguration CosmosConfiguration) : base(CosmosConfiguration) { }
    }

    public class TrackTimeCosmosClient : CosmosDbClient
    {
        public TrackTimeCosmosClient(CosmosConfiguration CosmosConfiguration) : base(CosmosConfiguration) { }

        public async Task<string> getLastUpdatedTimeAsync()
        {
            string query = "SELECT TOP 1 * FROM c ORDER BY c._ts DESC";

            try
            {
                // Execute the query and retrieve the last inserted record
                FeedIterator<TrackerData> iterator = _container.GetItemQueryIterator<TrackerData>(query);
                if (iterator.HasMoreResults)
                {
                    var results = await iterator.ReadNextAsync();
                    if (results.Count > 0)
                    {
                        TrackerData lastInsertedRecord = results.FirstOrDefault();
                        return lastInsertedRecord.TimeString;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Exception: - {ex.Message}"); }
            return "0";
        }

        public async Task updateLastUpdatedTimeAsync(string timeStamp)
        {
            try
            {
                // Execute the query and retrieve the last inserted record
                var item = new TrackerData() { id = Guid.NewGuid().ToString(), TimeString = timeStamp };
                await _container.CreateItemAsync<TrackerData>(item);
            }
            catch (Exception ex) { Console.WriteLine($"Exception: - {ex.Message}"); }
        }

    }

    public class TrackItemCosmosClient : CosmosDbClient
    {
        public TrackItemCosmosClient(CosmosConfiguration CosmosConfiguration) : base(CosmosConfiguration) { }


    }


}
