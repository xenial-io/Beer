using System.Threading.Tasks;
using static Xenial.Delicious.Beer.Tests.Json.PokeJsonFacts;
using static Xenial.Tasty;

namespace Xenial.Delicious.Beer.Tests
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            PokeJsonTests();

            return await Run(args);
        }
    }
}
