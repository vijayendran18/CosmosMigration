using CRUD.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Azure;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Document = Microsoft.Azure.Documents.Document;

namespace CRUD.Helper
{
    // <BulkOperationsHelper>
    public class BulkOperations
    {

        public List<ToDoItem> FailedItems { get; set; }
        public int SuccessRecords { get; set; }

        private readonly TrackTimeCosmosClient _tracker;

        public BulkOperations(TrackTimeCosmosClient tracker)
        {
            FailedItems = new List<ToDoItem>();
            SuccessRecords = 0;
            _tracker = tracker;
        }

        public async Task<List<ToDoItem>> BulkExecuteWithRetryAsync(Container container, IEnumerable<ToDoItem> items, int batchSize = 100, int maxRetries = 3, int retryDelayMs = 1000, bool EnableTrack = false)
        {
            FailedItems = new List<ToDoItem>();
            SuccessRecords = 0;

            var batches = SplitIntoBatches(items, batchSize);
            var proccessing = 0;

            foreach (var batch in batches)
            {

                Console.WriteLine($"Processing {proccessing} to {proccessing + batch.Count} records");
                proccessing += batch.Count;

                int retries = 0;
                var tasks = new List<Task<ItemResponse<ToDoItem>>>();

                while (retries < maxRetries)
                {
                    ItemResponse<ToDoItem>[] results;

                    try
                    {
                        var lastItem = batch.Last();

                        tasks = batch.Select(item => container.UpsertItemAsync(item)).ToList();
                        results = await Task.WhenAll(tasks);

                        foreach (var response in results)
                        {
                            if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.OK)
                            {
                                FailedItems.Add(response.Resource);
                            }
                        }

                        if (FailedItems.Count == 0)
                        {
                            SuccessRecords += batch.Count;
                            Console.WriteLine("success");

                            //update the max batch time
                            if (EnableTrack)
                                await _tracker.updateLastUpdatedTimeAsync(lastItem._ts.ToString());
                        }
                        else
                        {
                            Console.WriteLine($"Upsert failed - items count {FailedItems.Count}.");
                        }
                        break;

                    }
                    catch (Exception ex)
                    {
                        if (retries == 0) { Console.WriteLine($"Exception throw - {ex.Message}"); }

                        retries++;

                        if (retries < maxRetries)
                        {
                            // Retry with exponential backoff delay
                            int delayMs = retryDelayMs * retries;
                            Console.WriteLine($"Retry Wait Ms : {delayMs}");
                            await Task.Delay(delayMs);
                            Console.WriteLine($"Retry attempt : {retries}");
                        }
                        else
                        {
                            Console.WriteLine($"Max retries reached.");
                            FailedItems.AddRange(batch);
                        }
                    }
                }

            }

            return FailedItems;

        }
        private IEnumerable<List<ToDoItem>> SplitIntoBatches<ToDoItem>(IEnumerable<ToDoItem> items, int batchSize)
        {
            return items
                .Select((item, index) => new { Item = item, Index = index })
                .GroupBy(x => x.Index / batchSize)
                .Select(g => g.Select(x => x.Item).ToList());
        }

    }

}
