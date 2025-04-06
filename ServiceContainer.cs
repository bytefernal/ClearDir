using System;
using System.Collections.Generic;

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
        /// Resolves an instance of the specified service type.
        /// </summary>
        public TService Resolve<TService>() where TService : class
        {
            if (_singletons.TryGetValue(typeof(TService), out var singleton))
            {
                return (TService)singleton;
            }

            if (_isInScope && _scopedInstances.TryGetValue(typeof(TService), out var scoped))
            {
                return (TService)scoped;
            }

            if (_registrations.TryGetValue(typeof(TService), out var factory))
            {
                var instance = factory();

                // Store the instance in scoped dictionary if in scope
                if (_isInScope)
                {
                    _scopedInstances[typeof(TService)] = instance;
                }

                return (TService)instance;
            }

            throw new InvalidOperationException($"Service of type {typeof(TService)} is not registered.");
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
