# Multi master conflict generation and resolution samples for Gremlin API in .NET

## Getting Started

### Prerequisites

The sample assumes that 
1. You have a Cosmos DB Graph Account created with Mult-master enabled.  
2. You have database and a grph created. 
3. The Graph has "Last Writer Wins" as Conflict Resolution Mode, with '/_ts' as Conflict Resolver Property


### Installation

The only dependency is the [Gremlin.Net driver](http://tinkerpop.apache.org/docs/3.3.0/reference/#gremlin-DotNet), which you can install with the following instructions:

- Using .NET CLI:

    ```
    dotnet add package Gremlin.Net
    ```

- Using Powershell Package Manager:

    ```
    Install-Package Gremlin.Net
    ```

- For *.NET CORE* use the `nuget` [command-line utility](https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools):

    ```
    nuget install Gremlin.Net
    ```
