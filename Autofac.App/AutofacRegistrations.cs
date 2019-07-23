using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac.Core;
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Activators.ProvidedInstance;
using Autofac.Core.Activators.Reflection;
using Autofac.Core.Lifetime;

namespace Autofac.App
{
    public class AutofacRegistrations
    {
        public AutofacRegistrations(IComponentRegistry componentRegistry)
        {
            ComponentRegistry = componentRegistry;
        }

        public IComponentRegistry ComponentRegistry { get; }


        public IEnumerable<ParameterInfo> GetRegistrationConstructorParameters(IComponentRegistration componentRegistration)
        {
            if (!(componentRegistration.Activator is ReflectionActivator activator))
            {
                return Enumerable.Empty<ParameterInfo>();
            }              

            var limitType = activator.LimitType;
            return activator.ConstructorFinder.FindConstructors(limitType).SelectMany(ctor => ctor.GetParameters());
        }

        public Type GetConcreteType(IComponentRegistration r)
        {
            switch (r.Activator)
            {
                case ReflectionActivator reflectionActivator:
                    return reflectionActivator.LimitType;
                case DelegateActivator delegateActivator:
                    return delegateActivator.LimitType;
                case ProvidedInstanceActivator providedInstanceActivator:
                    return providedInstanceActivator.LimitType;
                default:
                    throw new InvalidOperationException(r.Activator.GetType() + " is not a known component registration type");
            }
        }

        public Lifetime GetLifetime(IComponentRegistration componentRegistration)
        {
            switch (componentRegistration.Ownership)
            {
                case InstanceOwnership.OwnedByLifetimeScope when componentRegistration.Sharing == InstanceSharing.Shared && componentRegistration.Lifetime is RootScopeLifetime:
                    return Lifetime.SingleInstance;
                case InstanceOwnership.OwnedByLifetimeScope when componentRegistration.Sharing == InstanceSharing.Shared && componentRegistration.Lifetime is CurrentScopeLifetime:
                    return Lifetime.InstancePerLifetimeScope;
                case InstanceOwnership.OwnedByLifetimeScope when componentRegistration.Sharing == InstanceSharing.None && componentRegistration.Lifetime is CurrentScopeLifetime:
                    return Lifetime.Transient;
                case InstanceOwnership.ExternallyOwned when componentRegistration.Sharing == InstanceSharing.None && componentRegistration.Lifetime is CurrentScopeLifetime:
                    return Lifetime.ExternallyOwned;
                case InstanceOwnership.ExternallyOwned when componentRegistration.Sharing == InstanceSharing.Shared && componentRegistration.Lifetime is CurrentScopeLifetime:
                    return Lifetime.SingleInstanceExternallyOwned;
                default:
                    throw new InvalidOperationException(string.Format("Unknown registration type for {3} Ownership: {0}, Sharing: {1}, Lifetime type: {2}", componentRegistration.Ownership, componentRegistration.Sharing,
                        componentRegistration.Lifetime.GetType().Name, GetConcreteType(componentRegistration)));
            }
        }
    }
}