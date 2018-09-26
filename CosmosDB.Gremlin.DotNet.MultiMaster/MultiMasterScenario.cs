//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace CosmosDB.Gremlin.DotNet.MultiMaster
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading.Tasks;
    using global::Gremlin.Net.Driver;
    using global::Gremlin.Net.Structure.IO.GraphSON;

    internal sealed class MultiMasterScenario
    {
        IList<Worker> workers;
        ConflictWorker conflictWorker;

        public MultiMasterScenario()
        {
            string gremlinEndpoint = ConfigurationManager.AppSettings["GremlinEndpoint"];
            int gremlinServerPort = int.Parse(ConfigurationManager.AppSettings["GremlinServerPort"]);
            string accountKey = ConfigurationManager.AppSettings["AuthorizationKey"];

            string[] regions = ConfigurationManager.AppSettings["regions"].Split(new string[] { "," },
                StringSplitOptions.RemoveEmptyEntries);

            string database = ConfigurationManager.AppSettings["databaseName"];
            string lwwCollectionName = ConfigurationManager.AppSettings["graphName"];

            this.workers = new List<Worker>();
            this.conflictWorker = new ConflictWorker(database, lwwCollectionName);

            foreach (string region in regions)
            {

                string regionalGremlinEndpint = gremlinEndpoint.Replace(".gremlin.cosmosdb.azure.com", $"-{region.ToLower().Replace(" ", string.Empty)}.gremlin.cosmosdb.azure.com");

                var regionalGremlinServer = new GremlinServer(regionalGremlinEndpint, gremlinServerPort, enableSsl: true,
                                                    username: "/dbs/" + database + "/colls/" + lwwCollectionName,
                                                    password: accountKey);

                var regionalGremlinClient = new GremlinClient(regionalGremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType);
                CosmosDBGremlinClient cosmosDbGremlinCleint = new CosmosDBGremlinClient(regionalGremlinClient, region, regionalGremlinEndpint);

                workers.Add(new Worker(cosmosDbGremlinCleint, database, lwwCollectionName, region));

                conflictWorker.AddGremlinClient(cosmosDbGremlinCleint);
            }
        }

        public async Task InitializeAsync()
        {
            await this.conflictWorker.InitializeAsync();
            Console.WriteLine("Initialized collections.");
        }

        public async Task RunBasicAsync()
        {
            Console.WriteLine("\n####################################################");
            Console.WriteLine("Basic Active-Active");
            Console.WriteLine("####################################################");

            Console.WriteLine("1) Starting insert loops across multiple regions ...");

            IList<Task> basicTask = new List<Task>();

            int documentsToInsertPerWorker = 20;

            foreach (Worker worker in this.workers)
            {
                basicTask.Add(worker.RunLoopAsync(documentsToInsertPerWorker));
            }

            await Task.WhenAll(basicTask);

            basicTask.Clear();

            Console.WriteLine("2) Reading from every region ...");

            int expectedDocuments = this.workers.Count * documentsToInsertPerWorker;
            foreach (Worker worker in this.workers)
            {
                basicTask.Add(worker.ReadAllAsync(expectedDocuments));
            }

            await Task.WhenAll(basicTask);

            basicTask.Clear();

            Console.WriteLine("3) Deleting all the documents ...");
            await this.workers[0].DeleteAllAsync();

            Console.WriteLine("####################################################");
        }

        public async Task RunLWWAsync()
        {
            Console.WriteLine("\n####################################################");
            Console.WriteLine("LWW Conflict Resolution");
            Console.WriteLine("####################################################");

            await this.conflictWorker.RunLWWConflictAsync();
            Console.WriteLine("####################################################");
        }
    }
}
