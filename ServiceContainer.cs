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
        /// Registers a service with a factory method and lifecycle.
        /// </summary>
        public void Register<TService>(Func<TService> factory, InstanceType instanceType = InstanceType.Singleton) where TService : class
        {
            RegisterInternal(typeof(TService), () => factory(), instanceType);
        }

        /// <summary>
        /// Registers a service with automatic constructor injection.
        /// </summary>
        public void Register<TService>(InstanceType instanceType = InstanceType.Singleton) where TService : class
        {
            RegisterInternal(typeof(TService), () => CreateInstance(typeof(TService)), instanceType);
        }

        /// <summary>
        /// Registers an interface with an implementation.
        /// </summary>
        public void Register<TService, TImplementation>(InstanceType instanceType = InstanceType.Singleton)
            where TService : class
            where TImplementation : class, TService
        {
            RegisterInternal(typeof(TService), () => CreateInstance(typeof(TImplementation)), instanceType);
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

            throw new InvalidOperationException($"Service of type {serviceType.FullName} is not registered.");
        }

        /// <summary>
        /// Starts a new scope, allowing scoped services to be resolved.
        /// </summary>
        public void BeginScope()
        {
            if (_isInScope)
            {
                throw new InvalidOperationException("A scope is already active. Call EndScope before starting a new one.");
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

        /// <summary>
        /// Internal method to handle registration logic.
        /// </summary>
        private void RegisterInternal(Type serviceType, Func<object> factory, InstanceType instanceType)
        {
            _registrations[serviceType] = factory;

            if (instanceType == InstanceType.Singleton)
            {
                _singletons[serviceType] = factory();
            }
        }

        /// <summary>
        /// Creates an instance of the specified type by resolving constructor dependencies.
        /// </summary>
        private object CreateInstance(Type serviceType)
        {
            EnsureValidType(serviceType);

            var constructor = GetConstructor(serviceType);
            var parameters = constructor.GetParameters()
                                        .Select(param => Resolve(param.ParameterType))
                                        .ToArray();

            var instance = Activator.CreateInstance(serviceType, parameters);

            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create an instance of {serviceType.FullName}. Ensure all dependencies are registered.");
            }

            return instance;
        }

        /// <summary>
        /// Ensures the provided type is valid for automatic registration.
        /// </summary>
        private void EnsureValidType(Type serviceType)
        {
            if (serviceType.IsAbstract || serviceType.IsInterface)
            {
                throw new InvalidOperationException($"Cannot register abstract classes or interfaces: {serviceType.FullName}.");
            }
        }

        /// <summary>
        /// Selects the most suitable constructor for the given type.
        /// </summary>
        private ConstructorInfo GetConstructor(Type serviceType)
        {
            var constructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (!constructors.Any())
            {
                throw new InvalidOperationException($"Type {serviceType.FullName} must have at least one public constructor.");
            }

            // Select the constructor with the most resolvable parameters
            return constructors.OrderByDescending(c => c.GetParameters().Length).First();
        }
    }
}
