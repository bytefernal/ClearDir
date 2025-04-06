using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ClearDir
{
    /// <summary>
    /// Specifies the lifecycle of a registered service.
    /// </summary>
    public enum InstanceType
    {
        /// <summary>
        /// A single shared instance will be used for the lifetime of the application.
        /// </summary>
        Singleton,

        /// <summary>
        /// A new instance will be created each time the service is resolved.
        /// </summary>
        Transient,

        /// <summary>
        /// A single instance will be created per scope.
        /// </summary>
        Scoped
    }

    /// <summary>
    /// A simple dependency injection container for registering and resolving services.
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, Func<object>> _registrations = new();
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Dictionary<Type, object> _scopedInstances = new();
        private bool _isInScope = false;

        /// <summary>
        /// Registers a service type with its factory method and lifecycle.
        /// </summary>
        public void Register<TService>(Func<TService> factory, InstanceType instanceType = InstanceType.Singleton) where TService : class
        {
            _registrations[typeof(TService)] = factory;

            if (instanceType == InstanceType.Singleton)
            {
                _singletons[typeof(TService)] = factory();
            }
        }

        /// <summary>
        /// Registers a service type for automatic constructor injection.
        /// </summary>
        public void Register<TService>(InstanceType instanceType = InstanceType.Singleton) where TService : class
        {
            var serviceType = typeof(TService);
            var constructor = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                         .SingleOrDefault(); // Ensure there is only one constructor

            if (constructor == null)
            {
                throw new InvalidOperationException($"Service of type {serviceType} must have exactly one public constructor.");
            }

            _registrations[serviceType] = () =>
            {
                var parameters = constructor.GetParameters()
                                            .Select(param => Resolve(param.ParameterType))
                                            .ToArray();

                return Activator.CreateInstance(serviceType, parameters);
            };

            if (instanceType == InstanceType.Singleton)
            {
                _singletons[serviceType] = _registrations[serviceType]();
            }
        }

        /// <summary>
        /// Resolves an instance of the specified service type.
        /// </summary>
        public TService Resolve<TService>() where TService : class
        {
            return (TService)Resolve(typeof(TService));
        }

        /// <summary>
        /// Resolves an instance of the specified service type.
        /// </summary>
        public object Resolve(Type serviceType)
        {
            if (_singletons.TryGetValue(serviceType, out var singleton))
            {
                return singleton;
            }

            if (_isInScope && _scopedInstances.TryGetValue(serviceType, out var scoped))
            {
                return scoped;
            }

            if (_registrations.TryGetValue(serviceType, out var factory))
            {
                var instance = factory();

                if (_isInScope)
                {
                    _scopedInstances[serviceType] = instance;
                }

                return instance;
            }

            throw new InvalidOperationException($"Service of type {serviceType} is not registered.");
        }

        /// <summary>
        /// Starts a new scope, allowing scoped services to be resolved.
        /// </summary>
        public void BeginScope()
        {
            if (_isInScope)
            {
                throw new InvalidOperationException("A scope is already active. You must call EndScope before starting a new one.");
            }

            _isInScope = true;
        }

        /// <summary>
        /// Ends the current scope, disposing all scoped services.
        /// </summary>
        public void EndScope()
        {
            if (!_isInScope)
            {
                throw new InvalidOperationException("There is no active scope to end.");
            }

            _scopedInstances.Clear();
            _isInScope = false;
        }
    }
}
