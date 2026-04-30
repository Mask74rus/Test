using BlazorAppTest.Configurator.AppBootstrapper;

namespace BlazorAppTest
{
    public class Program
    {
        public static void Main(string[] args)
            => new ModuleBootstrapper().Run(args);
    }
}
