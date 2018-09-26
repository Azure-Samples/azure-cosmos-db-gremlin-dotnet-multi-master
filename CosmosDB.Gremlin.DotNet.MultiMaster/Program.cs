//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace CosmosDB.Gremlin.DotNet.MultiMaster
{
    using System;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            Program.RunScenariosAsync().GetAwaiter().GetResult();
            Console.WriteLine("\nSample finished execution successfully. Please press any key to continue ....");
            Console.ReadLine();
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
