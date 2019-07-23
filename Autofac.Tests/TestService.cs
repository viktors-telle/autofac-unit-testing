namespace Autofac.Tests
{
    public class TestService : ITestService
    {
        private readonly IDependencyTestService _dependencyTestService;

        public TestService(IDependencyTestService dependencyTestService)
        {
            _dependencyTestService = dependencyTestService;
        }
    }

    public interface ITestService
    {
    }
}