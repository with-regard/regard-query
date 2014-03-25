using System;
using Regard.Query.Api;
using Regard.Query.Sql;

namespace Regard.Query.Samples
{
    public static class Run
    {
        [STAThread]
        public static void Main()
        {
            Console.WriteLine(@"builder.AllEvents().Only(""EventType"", ""DoSomething"").CountUniqueValues(""SessionId"").BrokenDownBy(""Day"");");

            var builder = new SqlQueryBuilder();
            IRegardQuery result = builder.AllEvents();
            result = result.Only("EventType", "DoSomething").CountUniqueValues("SessionId").BrokenDownBy("Day");

            Console.WriteLine("");
            Console.WriteLine(((SqlQuery)result).GenerateQuery());
            Console.WriteLine("");
            Console.WriteLine("Substitutions:");
            foreach (var sub in ((SqlQuery) result).GenerateSubstitutions())
            {
                Console.WriteLine("  {0}", sub);
            }

            Console.ReadLine();
        }
    }
}
