using System;
using System.Threading.Tasks;

namespace Regard.Query.StressTest.Cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                await StressTest.RunStressTest(new TestOptions(), TimeSpan.FromSeconds(60));
            }).Wait();

            Console.ReadKey();
        }
    }
}
