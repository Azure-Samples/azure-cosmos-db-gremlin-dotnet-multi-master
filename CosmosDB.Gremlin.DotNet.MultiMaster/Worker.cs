//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------
namespace CosmosDB.Gremlin.DotNet.MultiMaster
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Gremlin.Net.Driver;

    internal sealed class Worker
    {
        private CosmosDBGremlinClient client;
        private readonly string regionName;


        public Worker(CosmosDBGremlinClient client, string databaseName, string collectionName, string regionName)
        {
            this.client = client;
            this.regionName = regionName;
        }

        public async Task RunLoopAsync(int documentsToInsert)
        {
            int iterationCount = 0;

            List<long> latency = new List<long>();
            while (iterationCount++ < documentsToInsert)
            {
                long startTick = Environment.TickCount;

                await this.client.GremlinClient.SubmitAsync<dynamic>($"g.addV().property('id', '{Guid.NewGuid().ToString()}')");

                long endTick = Environment.TickCount;

                latency.Add(endTick - startTick);
            }

            latency.Sort();
            int p50Index = (latency.Count / 2);

            Console.WriteLine("Inserted {2} vertices at {0} with p50 {1} ms",
                regionName,
                latency[p50Index],
                documentsToInsert);
        }

        public async Task ReadAllAsync(int expectedNumberOfDocuments)
        {
            while (true)
            {
                dynamic results = await this.client.GremlinClient.SubmitAsync<dynamic>($"g.V()");
                int totalItemRead = ((List<object>)results).Count;

                if (totalItemRead < expectedNumberOfDocuments)
                {
                    Console.WriteLine("Total item read {0} from {1} is less than {2}, retrying reads",
                        totalItemRead,
                        this.client.Endpoint,
                        expectedNumberOfDocuments);

                    await Task.Delay(1000);
                    continue;
                }
                else
                {
                    Console.WriteLine("Read {0} items from {1}", totalItemRead, this.client.Endpoint);
                    break;
                }
            }
        }

        public async Task DeleteAllAsync()
        {
            dynamic results = await this.client.GremlinClient.SubmitAsync<dynamic>($"g.V().drop()");
            int totalItemRead = ((List<object>)results).Count;

            Console.WriteLine("Deleted all documents from region {0}", this.client.Endpoint);
        }
    }
}