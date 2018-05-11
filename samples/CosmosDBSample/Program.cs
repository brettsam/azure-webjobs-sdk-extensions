using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace CosmosDBSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IHost host = new HostBuilder()
                .AddCosmosDB()
                .UseConsoleLifetime()
                .Build();

            using (host)
            {
                await host.RunAsync();
            }
        }
    }
}
