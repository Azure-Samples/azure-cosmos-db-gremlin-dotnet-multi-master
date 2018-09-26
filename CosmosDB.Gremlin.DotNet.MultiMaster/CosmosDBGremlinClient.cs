//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosDB.Gremlin.DotNet.MultiMaster
{
    using global::Gremlin.Net.Driver;

    public sealed class CosmosDBGremlinClient
    {
        public CosmosDBGremlinClient(GremlinClient client, string regionName, string endpoint)
        {
            this.GremlinClient = client;
            this.RegionName = regionName;
            this.Endpoint = endpoint;
        }

        public string Endpoint { get; private set; }
        public GremlinClient GremlinClient { get; private set; }
        public string RegionName { get; private set; }
    }
}