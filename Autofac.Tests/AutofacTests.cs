using Autofac.App;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Autofac.Tests
{

    public class AutofacTests
    {
        private readonly IContainer _container;
        private readonly AutofacRegistrations _autofacRegistrations;

        public AutofacTests()
        {
            var collection = new ServiceCollection();
            collection.AddHttpClient();

            var builder = new ContainerBuilder();
            builder.RegisterType<TestService>().As<ITestService>().InstancePerLifetimeScope();
            builder.RegisterType<DependencyTestService>().As<IDependencyTestService>().SingleInstance();
            builder.Populate(collection);
            _container = builder.Build();

            _autofacRegistrations = new AutofacRegistrations(_container.ComponentRegistry);
        }

        [Fact]
        public void Services_Should_Only_Have_Dependencies_With_Lesser_Lifetime()
        {
            var ex = Record.Exception(() =>
                new DependencyInspector(_container).CheckServicesShouldOnlyHaveDependenciesWithLesserLifetime(
                    _autofacRegistrations));

            Assert.Null(ex);
        }

        [Fact]
        public void All_Dependencies_Should_Be_Resolvable()
        {
            var ex = Record.Exception(() =>
                new DependencyInspector(_container).CheckCanResolveAllRegisteredDependencies(_autofacRegistrations));

            Assert.Null(ex);
        }


        [Fact]
        public void Should_Not_Be_Duplicated_Registrations()
        {
            var ex = Record.Exception(() =>
                new DependencyInspector(_container).CheckDuplicatedRegistrations(_autofacRegistrations));

            Assert.Null(ex);
        }
    }
}
