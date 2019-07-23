using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autofac.Core;

namespace Autofac.App
{
    public class DependencyInspector
    {
        private readonly IContainer _container;

        public DependencyInspector(IContainer container)
        {
            _container = container;
        }

        public void CheckCanResolveAllRegisteredDependencies(AutofacRegistrations autofacRegistrations)
        {
            var distinctTypes = autofacRegistrations.ComponentRegistry.Registrations
                .SelectMany(r => r.Services.OfType<TypedService>().Select(s => s.ServiceType)
                    .Union(GetGenericFactoryTypes(r)))
                .Distinct();

            var failingTypes = new List<string>();
            foreach (var distinctType in distinctTypes)
            {
                try
                {
                    _container.Resolve(distinctType);
                }
                catch (DependencyResolutionException e)
                {
                    failingTypes.Add(e.ToString());
                }
            }

            if (failingTypes.Any())
            {
                throw new Exception("The following types are not resolvable: " +
                                    $"{failingTypes.Aggregate((a, b) => a + Environment.NewLine + b)}");
            }

            IEnumerable<Type> GetGenericFactoryTypes(IComponentRegistration componentRegistration)
            {
                return from ctorParameter in autofacRegistrations.GetRegistrationConstructorParameters(componentRegistration)
                    where ctorParameter.ParameterType.FullName.StartsWith("System.Func")
                    select ctorParameter.ParameterType.GetGenericArguments()[0];
            }
        }

        public void CheckServicesShouldOnlyHaveDependenciesWithLesserLifetime(AutofacRegistrations data)
        {
            var exceptions = new List<string>();
            foreach (var registration in data.ComponentRegistry.Registrations)
            {
                var registrationLifetime = data.GetLifetime(registration);

                foreach (var ctorParameter in data.GetRegistrationConstructorParameters(registration))
                {
                    var typedService = new TypedService(ctorParameter.ParameterType);

                    if (ContainsMicrosoftInNamespace(typedService.ServiceType)) continue;

                    if (ContainsSystemInNamespace(typedService.ServiceType)) continue;

                    // If the parameter is not registered with autofac, ignore
                    if (!data.ComponentRegistry.TryGetRegistration(typedService, out var parameterRegistration)) continue;

                    var parameterLifetime = data.GetLifetime(parameterRegistration);

                    if (parameterLifetime >= registrationLifetime) continue;

                    var typeName = data.GetConcreteType(registration).ToString();
                    var parameterType = ctorParameter.ParameterType.ToString();

                    var error = $"{typeName} ({registrationLifetime}) => {parameterType} ({parameterLifetime})";
                    exceptions.Add(error);
                }
            }

            if (exceptions.Any())
            {
                throw new Exception("The following components should not depend on with greater lifetimes: " +
                                    $"{exceptions.Aggregate((a, b) => a + Environment.NewLine + b)}");
            }

            bool ContainsMicrosoftInNamespace(Type type) => type.Assembly.FullName.StartsWith("Microsoft", StringComparison.InvariantCultureIgnoreCase);
            bool ContainsSystemInNamespace(Type type) => type.Assembly.FullName.StartsWith("System", StringComparison.InvariantCultureIgnoreCase);
        }

        public void CheckDuplicatedRegistrations(AutofacRegistrations data)
        {
            var sb = new StringBuilder();
            var duplicatedComponents = data.ComponentRegistry.Registrations
                .GroupBy(registration => registration.Activator.LimitType)
                .Where(g => g.Count() > 1)
                .Select(y => new { Type = y.Key, Count = y.Count() })
                .ToList();

            if (!duplicatedComponents.Any()) return;
            sb.AppendLine();
            foreach (var duplicatedComponent in duplicatedComponents)
            {
                sb.Append($"Component: {duplicatedComponent.Type} ");
                sb.Append($"Count: {duplicatedComponent.Count}");
                sb.AppendLine();
            }
            throw new Exception($"The following components have duplicated registrations:{sb}");
        }
    }
}