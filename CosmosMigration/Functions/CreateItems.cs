using System.Diagnostics;
using System.Net;
using CRUD.Helper;
using CRUD.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CRUD
{
    public class CreateItems
    {
        private readonly ILogger _logger;
        private readonly CosmosDbClient _srcClient;
        private readonly BulkOperations _bulkOperations;

        public CreateItems(ILoggerFactory loggerFactory, SourceCosmosClient srcClient, BulkOperations bulkOperations)
        {
            _logger = loggerFactory.CreateLogger<CreateItems>();
            _srcClient = srcClient;
            _bulkOperations = bulkOperations;
        }

        [Function("CreateItems")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {

            List<ToDoItem> items = new List<ToDoItem>();
            int counts = 100000;

            for (int i = 0; i < counts; i++)
            {
                items.Add(new ToDoItem() { id = Guid.NewGuid().ToString(), creationTime = DateTime.Now });
            }

            var t = new Stopwatch();
            t.Start();

            // <BulkImport>
            var data = await _bulkOperations.BulkExecuteWithRetryAsync(_srcClient._container, items);

            t.Stop();


            var elapsed = t.Elapsed;

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString($"{counts} items created in {elapsed}");
            return response;
        }




    }
}
