//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosDB.Gremlin.DotNet.MultiMaster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using global::Gremlin.Net.Driver;
    using System.Configuration;

    internal sealed class ConflictWorker
    {
        private readonly IList<CosmosDBGremlinClient> gremlinClients;
        private readonly string databaseName;
        private readonly string lwwCollectionName;

        public ConflictWorker(string databaseName, string lwwCollectionName)
        {
            this.gremlinClients = new List<CosmosDBGremlinClient>();

            this.databaseName = databaseName;
            this.lwwCollectionName = lwwCollectionName;
        }

        public void AddGremlinClient(CosmosDBGremlinClient client)
        {
            this.gremlinClients.Add(client);
        }

        public async Task InitializeAsync()
        {
            // This assumes that a graph has already been created, as specified in the app.config. 
            await Task.FromResult(true);
        }


        public async Task RunLWWConflictAsync()
        {
            Console.WriteLine("\r\nInsert Conflict\r\n");
            await this.RunAddVertexConflictOnLWWAsync();
        }

        public async Task RunAddVertexConflictOnLWWAsync()
        {
            do
            {
                Console.WriteLine("1) Performing conflicting insert across {0} regions on {1}", this.gremlinClients.Count, this.lwwCollectionName);

                IList<Task<dynamic>> insertTask = new List<Task<dynamic>>();

                int index = 0;
                string id = Guid.NewGuid().ToString();
                foreach (CosmosDBGremlinClient client in this.gremlinClients)
                {
                    insertTask.Add(this.TryInsertVertex(client, index++, id));
                }

                dynamic[] conflictDocuments = (await Task.WhenAll(insertTask)).Where(document => document != null).ToArray();

                if (conflictDocuments.Length > 1)
                {
                    Console.WriteLine("2) Caused {0} insert conflicts, verifying conflict resolution", conflictDocuments.Length);

                    await this.ValidateLWWAsync(id, this.gremlinClients, conflictDocuments);

                    break;
                }
                else
                {
                    Console.WriteLine("Retrying insert to induce conflicts");
                }
            } while (true);
        }


        private async Task<dynamic> TryInsertVertex(CosmosDBGremlinClient client, int index, string id)
        {
            try
            {
                return await client.GremlinClient.SubmitAsync<dynamic>($"g.withStrategies(ProjectionStrategy.build().IncludeSystemProperties('{ConfigurationManager.AppSettings["conflict_resolver_property"]}').create()).addV().property('id', '{id}').property('regionId', {index}).property('regionEndpoint', '{client.Endpoint}')");
            }
            catch (Exception ex)
            {
                if(ex.ToString().Contains("ConflictException"))
                {
                    return null;
                }
                throw;
            }
        }
        

        private async Task ValidateLWWAsync(string id, IList<CosmosDBGremlinClient> clients, dynamic[] conflictDocument, bool hasDeleteConflict = false)
        {
            foreach (CosmosDBGremlinClient client in clients)
            {
                await this.ValidateLWWAsync(id, client, conflictDocument, hasDeleteConflict);
            }
        }

        private async Task ValidateLWWAsync(string id, CosmosDBGremlinClient client, dynamic[] conflictDocument, bool hasDeleteConflict)
        {
            dynamic winnerDocument = null;
            long winnerTs = -1;

            foreach (dynamic vertex in conflictDocument)
            {
                Dictionary<string, object> props = (Dictionary<string, object>)((Dictionary<string, object>)vertex[0])["properties"];
                List<object> tsObject= ((IEnumerable<object>)props[ConfigurationManager.AppSettings["conflict_resolver_property"]]).ToList();

                long ts = (long)((Dictionary<string, object>)tsObject[0])["value"];

                if (winnerDocument == null ||
                    winnerTs <= ts)
                {
                    winnerDocument = vertex;
                    winnerTs = ts;
                }
            }

            Console.WriteLine("Vertex with ts {0} should be the winner",
                winnerTs);

            while (true)
            {
                try
                {
                    long existsingTs = (long)(await client.GremlinClient.SubmitAsync<dynamic>($"g.withStrategies(ProjectionStrategy.build().IncludeSystemProperties('{ConfigurationManager.AppSettings["conflict_resolver_property"]}').create()).V('{id}').values('{ConfigurationManager.AppSettings["conflict_resolver_property"]}')")).First();

                    if (existsingTs == winnerTs)
                    {
                        Console.WriteLine("Winner document with ts region {0} found at {1}",
                            existsingTs,
                            client.Endpoint);
                        break;
                    }
                    else
                    {
                        this.TraceError("Winning document version from with ts {0} is not found @ {1}, retrying...",
                            winnerTs,
                            client.Endpoint);
                        await Task.Delay(500);
                    }
                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("Not Found"))
                    {
                        this.TraceError("Winner document wit ts {0} is not found @ {1}, retrying...",
                            winnerTs,
                            client.Endpoint);
                        await Task.Delay(500);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private void TraceError(string format, params object[] values)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(format, values);
            Console.ResetColor();
        }
    }
}
