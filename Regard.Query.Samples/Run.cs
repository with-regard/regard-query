using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Regard.Query.Api;
using Regard.Query.Sql;

namespace Regard.Query.Samples
{
    public static class Run
    {
        [STAThread]
        public static void Main()
        {
            // We need to use asynchronous stuff and there's no way to make an entry point async in C# (though there really should be), so run as a task
            var runIt = Task.Run(async () => 
            {
                Console.WriteLine(@"builder.AllEvents().Only(""EventType"", ""DoSomething"").CountUniqueValues(""SessionId"").BrokenDownBy(""Day"");");

                var connection = new SqlConnection("");
                var builder = new SqlQueryBuilder(connection, 1);
                IRegardQuery result = builder.AllEvents();
                result = result.Only("EventType", "DoSomething").CountUniqueValues("SessionId", "NumSessions").BrokenDownBy("Day", "Day");

                Console.WriteLine("");
                Console.WriteLine(((SqlQuery)result).GenerateQuery());
                Console.WriteLine("");
                Console.WriteLine("Substitutions:");
                foreach (var sub in ((SqlQuery) result).GenerateSubstitutions())
                {
                    Console.WriteLine("  {0} = {1}", sub.Name, sub.Value);
                }

                Console.WriteLine("Press enter to run the query...");
                Console.ReadLine();

                await connection.OpenAsync();
                var queryResult = await result.RunQuery();

                for (var nextLine = await queryResult.FetchNext(); nextLine != null; nextLine = await queryResult.FetchNext())
                {
                    Console.WriteLine("=== NEW LINE: {0} events", nextLine.EventCount);

                    foreach (var column in nextLine.Columns)
                    {
                        Console.WriteLine("  {0} = {1}", column.Name, column.Value);
                    }
                }

                Console.ReadLine();
            });

            runIt.Wait();
        }
    }
}
