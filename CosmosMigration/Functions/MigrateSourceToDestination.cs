using System.Diagnostics;
using System.Net;
using CRUD.Helper;
using CRUD.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Management.KeyVault.Fluent;
using Microsoft.Extensions.Logging;

namespace CRUD
{
    public class MigrateSourceToDestination
    {
        private readonly ILogger _logger;
        private readonly CosmosDbClient _srcClient, _destClient;
        private readonly TrackTimeCosmosClient _trackerTimeClient;
        private readonly TrackItemCosmosClient _trackItemClient;
        private readonly BulkOperations _bulkOperations;

        public MigrateSourceToDestination(ILoggerFactory loggerFactory,
            SourceCosmosClient srcContainer,
            DestinationCosmosClient destContainer,
            TrackItemCosmosClient trackItemContainer,
            TrackTimeCosmosClient trackerTimeContainer,
            BulkOperations bulkOperations
            )
        {
            _logger = loggerFactory.CreateLogger<MigrateSourceToDestination>();
            _srcClient = srcContainer;
            _destClient = destContainer;
            _trackItemClient = trackItemContainer;
            _trackerTimeClient = trackerTimeContainer;
            _bulkOperations = bulkOperations;
        }

        [Function("MigrateSourceToDestination")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            int totalRecords = 0;
            int successRecords = 0;
            var list = new List<ToDoItem>();

            var t = new Stopwatch();
            t.Start();


            //taking the backuped item to the list
            Console.WriteLine("Checking for the backup item(s).");
            var trackedItem = await _trackItemClient.getAllItemsAsync<ToDoItem>();
            if (trackedItem.Count > 0)
            {
                Console.WriteLine($"Found {trackedItem.Count} item(s) from {_trackItemClient._container.Id} container.");
            }
            else { Console.WriteLine($"No item found from {_trackItemClient._container.Id} container."); }



            //Checking the last updated time
            Console.WriteLine("Checking the last updated time.");
            var lastTime = await _trackerTimeClient.getLastUpdatedTimeAsync();
            Console.WriteLine(lastTime);



            //taking the source item to the list
            var query = ($"SELECT * FROM c WHERE c._ts > {lastTime} ORDER BY c._ts ASC");
            var srcData = await _srcClient.getItemsByQueryAsync<ToDoItem>(query);
            if (srcData.Count > 0)
            {
                Console.WriteLine($"Found {srcData.Count} item(s) from {_srcClient._container.Id} container.");

                //remove the duplicate data from the backup container
                trackedItem = trackedItem.Where(p1 => !srcData.Any(p2 => p2.id == p1.id)).ToList();
            }
            else { Console.WriteLine($"No item found from {_srcClient._container.Id} container.."); }



            //Addin both to the list
            if (trackedItem.Count > 0)
                list.AddRange(trackedItem);
            if (srcData.Count > 0)
                list.AddRange(srcData);

            totalRecords = list.Count;
            Console.WriteLine($"Total item(s) count - {totalRecords}.");



            if (totalRecords > 0)
            {
                var maxTime = list.LastOrDefault();

                try
                {
                    Console.WriteLine($"Start migrating {totalRecords} item(s) to destination.");

                    //Moving item to the destination container.
                    var failedRecords = await _bulkOperations.BulkExecuteWithRetryAsync(_destClient._container, list, EnableTrack: true);
                    successRecords = _bulkOperations.SuccessRecords;

                    //Moving the failed item to the track item container.
                    if (failedRecords.Count == 0)
                    {
                        await _trackItemClient.DeleteAllItemAsync();
                    }
                    else
                    {
                        Console.WriteLine($"Failed records found: - Item(s) count {failedRecords.Count}");
                        Console.WriteLine($"Start inserting to the {_trackItemClient._container.Id} container.");
                        await _bulkOperations.BulkExecuteWithRetryAsync(_trackItemClient._container, failedRecords);
                    }

                    //updating the last item time
                    if (maxTime != null)
                    {
                        Console.WriteLine($"updating the last item time - {maxTime._ts}.");
                        await _trackerTimeClient.updateLastUpdatedTimeAsync(maxTime._ts.ToString());
                    }

                }
                catch (CosmosException ex)
                {
                    // Handle Cosmos DB exception
                    Console.WriteLine($"CosmosException: - {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle general exception
                    Console.WriteLine($"Exception: - {ex.Message}");
                }
            }

            t.Stop();
            var timeElapsed = t.Elapsed;

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString($"{successRecords} of {totalRecords} items migrated in {timeElapsed}");
            return response;
        }
    }
}
