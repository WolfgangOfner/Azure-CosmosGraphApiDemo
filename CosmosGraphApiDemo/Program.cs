using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Graphs;
using Newtonsoft.Json;

namespace CosmosGraphApiDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            const string endpoint = "YourURI";
            const string authKey = "YourKey";

            using (var client = new DocumentClient(new Uri(endpoint), authKey,
                new ConnectionPolicy { ConnectionMode = ConnectionMode.Direct, ConnectionProtocol = Protocol.Tcp }))
            {
                RunAsync(client).Wait();
            }
        }

        public static async Task RunAsync(DocumentClient client)
        {
            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = "wolfganggraphdb" });

            DocumentCollection graph = await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri("wolfganggraphdb"),
                new DocumentCollection { Id = "People" },
                new RequestOptions { OfferThroughput = 1000 });


            var gremlinQueries = new Dictionary<string, string>
            {
                // delete all entries from the db
                {"Cleanup", "g.V().drop()"},
                // Add nodes
                {
                    "AddNode 1",
                    "g.addV('person').property('id', 'Wolfgang').property('firstName', 'Wolfgang').property('age', 28)"
                },
                {
                    "AddNode 2",
                    "g.addV('person').property('id', 'John').property('firstName', 'John').property('lastName', 'Andersen').property('age', 33)"
                },
                {
                    "AddNode 3",
                    "g.addV('person').property('id', 'Sue').property('firstName', 'Sue').property('lastName', 'Doe')"
                },
                {
                    "AddNode 4",
                    "g.addV('person').property('id', 'Anna').property('firstName', 'Anna').property('lastName', 'Smith')"
                },
                // Add edges
                {"AddEdge 1", "g.V('Wolfgang').addE('knows').to(g.V('Sue'))"},
                {"AddEdge 2", "g.V('Wolfgang').addE('knows').to(g.V('Anna'))"},
                {"AddEdge 3", "g.V('Anna').addE('knows').to(g.V('John'))"}
            };

            foreach (var gremlinQuery in gremlinQueries)
            {
                var query = client.CreateGremlinQuery<dynamic>(graph, gremlinQuery.Value);

                while (query.HasMoreResults)
                {
                    foreach (var result in await query.ExecuteNextAsync())
                    {
                        // add values
                        JsonConvert.SerializeObject(result);
                    }
                }
            }

            // get people Wolfgang knows
            var queryToFindFriends = client.CreateGremlinQuery<dynamic>
                (graph, "g.V('Wolfgang').outE('knows').inV().hasLabel('person')");

            while (queryToFindFriends.HasMoreResults)
            {
                foreach (var result in await queryToFindFriends.ExecuteNextAsync())
                {
                    Console.WriteLine($"{JsonConvert.SerializeObject(result)}");
                }
            }
        }
    }
}