using CRUD.Helper;
using CRUD.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


IConfiguration _config = null;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((hostContext, config) =>
    {

        config.AddJsonFile("local.settings.json", optional: true);
        //config.AddEnvironmentVariables(prefix: "PREFIX_");
        config.AddCommandLine(args);
        _config = config.Build();

    })
    .ConfigureServices(Services =>
    {
        //var serviceProvider = Services.BuildServiceProvider();

        //Source container 
        SourceCosmosConfiguration sourceCosmosConfiguration = _config.GetSection("SourceCosmosConfiguration").Get<SourceCosmosConfiguration>();
        Services.AddSingleton<SourceCosmosConfiguration>(sourceCosmosConfiguration);
        Services.AddSingleton(new SourceCosmosClient(sourceCosmosConfiguration));

        //Dest container
        DestinationCosmosConfiguration destCosmosConfiguration = _config.GetSection("DestCosmosConfiguration").Get<DestinationCosmosConfiguration>();
        Services.AddSingleton<DestinationCosmosConfiguration>(destCosmosConfiguration);
        Services.AddSingleton(new DestinationCosmosClient(destCosmosConfiguration));

        //Track time container
        CosmosConfiguration trackCosmosConfiguration = _config.GetSection("TrackCosmosConfiguration").Get<CosmosConfiguration>();
        var trackTimeClient = new TrackTimeCosmosClient(trackCosmosConfiguration);
        Services.AddSingleton(trackTimeClient);

        //Track Item container
        CosmosConfiguration trackItemCosmosConfiguration = _config.GetSection("TrackItemCosmosConfiguration").Get<CosmosConfiguration>();
        Services.AddSingleton(new TrackItemCosmosClient(trackItemCosmosConfiguration));

        Services.AddSingleton(new BulkOperations(trackTimeClient));

    })
    .Build();


host.Run();
