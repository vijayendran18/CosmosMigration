using System.Net;
using CRUD.Helper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CRUD.Functions
{
    public class ClearDocuments
    {
        private readonly ILogger _logger;
        private readonly CosmosDbClient _srcClient, _destClient, _trackerTimeClient, _trackItemClient;
        public ClearDocuments(
            ILoggerFactory loggerFactory,
            SourceCosmosClient srcContainer,
            DestinationCosmosClient destContainer,
            TrackItemCosmosClient trackItemContainer,
            TrackTimeCosmosClient trackerTimeContainer
            )
        {
            _logger = loggerFactory.CreateLogger<ClearDocuments>();
            _srcClient = srcContainer;
            _destClient = destContainer;
            _trackItemClient = trackItemContainer;
            _trackerTimeClient = trackerTimeContainer;
        }

        [Function("ClearDocuments")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(_srcClient.DeleteAllItemAsync());
            tasks.Add(_destClient.DeleteAllItemAsync());
            tasks.Add(_trackItemClient.DeleteAllItemAsync());
            tasks.Add(_trackerTimeClient.DeleteAllItemAsync());

            await Task.WhenAll(tasks);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString("Cleared!");
            return response;
        }
    }
}
