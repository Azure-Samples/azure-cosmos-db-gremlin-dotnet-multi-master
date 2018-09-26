//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosDB.Gremlin.DotNet.MultiMaster
{
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            Program.RunScenariosAsync().GetAwaiter().GetResult();
        }

        static async Task RunScenariosAsync()
        {
            MultiMasterScenario scenario = new MultiMasterScenario();
            await scenario.InitializeAsync();
            await scenario.RunBasicAsync();
            await scenario.RunLWWAsync();
        }
    }
}
