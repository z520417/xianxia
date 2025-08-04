using System;
using System.Collections.Generic;
using UnityEngine;

namespace XianXiaGame.Core
{
    /// <summary>
    /// 服务生命周期类型
    /// </summary>
    public enum ServiceLifetime
    {
        Singleton,  // 单例 - 整个应用程序生命周期内只有一个实例
        Transient,  // 瞬态 - 每次请求都创建新实例
        Scoped      // 作用域 - 在特定作用域内是单例（暂未实现）
    }

    /// <summary>
    /// 服务描述器
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
        public Func<IServiceContainer, object> Factory { get; set; }
        public object Instance { get; set; }

        public ServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            ImplementationType = implementationType;
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, Func<IServiceContainer, object> factory, ServiceLifetime lifetime)
        {
            ServiceType = serviceType;
            Factory = factory;
            Lifetime = lifetime;
        }

        public ServiceDescriptor(Type serviceType, object instance)
        {
            ServiceType = serviceType;
            Instance = instance;
            Lifetime = ServiceLifetime.Singleton;
        }
    }

    /// <summary>
    /// 服务容器接口
    /// </summary>
    public interface IServiceContainer
    {
        // 注册服务
        IServiceContainer RegisterSingleton<TService, TImplementation>()
            where TImplementation : class, TService;

        IServiceContainer RegisterSingleton<TService>(TService instance)
            where TService : class;

        IServiceContainer RegisterSingleton<TService>(Func<IServiceContainer, TService> factory)
            where TService : class;

        IServiceContainer RegisterTransient<TService, TImplementation>()
            where TImplementation : class, TService;

        IServiceContainer RegisterTransient<TService>(Func<IServiceContainer, TService> factory)
            where TService : class;

        // 获取服务
        TService GetService<TService>() where TService : class;
        object GetService(Type serviceType);
        TService GetRequiredService<TService>() where TService : class;
        object GetRequiredService(Type serviceType);

        // 检查服务
        bool IsRegistered<TService>();
        bool IsRegistered(Type serviceType);

        // 生命周期管理
        void Dispose();
    }

    /// <summary>
    /// 简单的依赖注入容器实现
    /// </summary>
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        private readonly Dictionary<Type, ServiceDescriptor> m_Services = new Dictionary<Type, ServiceDescriptor>();
        private readonly Dictionary<Type, object> m_SingletonInstances = new Dictionary<Type, object>();
        private readonly object m_Lock = new object();
        private bool m_IsDisposed = false;

        public IServiceContainer RegisterSingleton<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return RegisterSingleton(typeof(TService), typeof(TImplementation));
        }

        public IServiceContainer RegisterSingleton<TService>(TService instance)
            where TService : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            lock (m_Lock)
            {
                var descriptor = new ServiceDescriptor(typeof(TService), instance);
                m_Services[typeof(TService)] = descriptor;
                m_SingletonInstances[typeof(TService)] = instance;
            }

            GameLog.Debug($"注册单例实例: {typeof(TService).Name}", "ServiceContainer");
            return this;
        }

        public IServiceContainer RegisterSingleton<TService>(Func<IServiceContainer, TService> factory)
            where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (m_Lock)
            {
                var descriptor = new ServiceDescriptor(typeof(TService), 
                    container => factory(container), ServiceLifetime.Singleton);
                m_Services[typeof(TService)] = descriptor;
            }

            GameLog.Debug($"注册单例工厂: {typeof(TService).Name}", "ServiceContainer");
            return this;
        }

        public IServiceContainer RegisterTransient<TService, TImplementation>()
            where TImplementation : class, TService
        {
            return RegisterTransient(typeof(TService), typeof(TImplementation));
        }

        public IServiceContainer RegisterTransient<TService>(Func<IServiceContainer, TService> factory)
            where TService : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (m_Lock)
            {
                var descriptor = new ServiceDescriptor(typeof(TService), 
                    container => factory(container), ServiceLifetime.Transient);
                m_Services[typeof(TService)] = descriptor;
            }

            GameLog.Debug($"注册瞬态工厂: {typeof(TService).Name}", "ServiceContainer");
            return this;
        }

        public TService GetService<TService>() where TService : class
        {
            return GetService(typeof(TService)) as TService;
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return GetServiceInternal(serviceType);
            }
            catch (Exception e)
            {
                GameLog.Error($"获取服务失败 {serviceType.Name}: {e.Message}", "ServiceContainer");
                return null;
            }
        }

        public TService GetRequiredService<TService>() where TService : class
        {
            var service = GetService<TService>();
            if (service == null)
            {
                throw new InvalidOperationException($"未找到必需的服务: {typeof(TService).Name}");
            }
            return service;
        }

        public object GetRequiredService(Type serviceType)
        {
            var service = GetService(serviceType);
            if (service == null)
            {
                throw new InvalidOperationException($"未找到必需的服务: {serviceType.Name}");
            }
            return service;
        }

        public bool IsRegistered<TService>()
        {
            return IsRegistered(typeof(TService));
        }

        public bool IsRegistered(Type serviceType)
        {
            lock (m_Lock)
            {
                return m_Services.ContainsKey(serviceType);
            }
        }

        private IServiceContainer RegisterSingleton(Type serviceType, Type implementationType)
        {
            lock (m_Lock)
            {
                var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Singleton);
                m_Services[serviceType] = descriptor;
            }

            GameLog.Debug($"注册单例: {serviceType.Name} -> {implementationType.Name}", "ServiceContainer");
            return this;
        }

        private IServiceContainer RegisterTransient(Type serviceType, Type implementationType)
        {
            lock (m_Lock)
            {
                var descriptor = new ServiceDescriptor(serviceType, implementationType, ServiceLifetime.Transient);
                m_Services[serviceType] = descriptor;
            }

            GameLog.Debug($"注册瞬态: {serviceType.Name} -> {implementationType.Name}", "ServiceContainer");
            return this;
        }

        private object GetServiceInternal(Type serviceType)
        {
            if (m_IsDisposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));

            lock (m_Lock)
            {
                if (!m_Services.TryGetValue(serviceType, out var descriptor))
                {
                    return null;
                }

                // 如果是单例且已创建实例，直接返回
                if (descriptor.Lifetime == ServiceLifetime.Singleton && 
                    m_SingletonInstances.TryGetValue(serviceType, out var existingInstance))
                {
                    return existingInstance;
                }

                object instance = CreateInstance(descriptor);

                // 缓存单例实例
                if (descriptor.Lifetime == ServiceLifetime.Singleton && instance != null)
                {
                    m_SingletonInstances[serviceType] = instance;
                }

                return instance;
            }
        }

        private object CreateInstance(ServiceDescriptor descriptor)
        {
            try
            {
                // 如果有预设实例，直接返回
                if (descriptor.Instance != null)
                    return descriptor.Instance;

                // 如果有工厂方法，使用工厂创建
                if (descriptor.Factory != null)
                    return descriptor.Factory(this);

                // 使用反射创建实例
                if (descriptor.ImplementationType != null)
                    return CreateInstanceWithDependencies(descriptor.ImplementationType);

                return null;
            }
            catch (Exception e)
            {
                GameLog.Error($"创建服务实例失败 {descriptor.ServiceType.Name}: {e.Message}", "ServiceContainer");
                throw;
            }
        }

        private object CreateInstanceWithDependencies(Type type)
        {
            // 简单的依赖注入实现
            var constructors = type.GetConstructors();
            
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                var args = new object[parameters.Length];
                bool canResolve = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    var arg = GetServiceInternal(paramType);
                    
                    if (arg == null)
                    {
                        canResolve = false;
                        break;
                    }
                    
                    args[i] = arg;
                }

                if (canResolve)
                {
                    return Activator.CreateInstance(type, args);
                }
            }

            // 如果无法解析依赖，尝试无参构造函数
            try
            {
                return Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                GameLog.Error($"无法创建类型实例 {type.Name}: {e.Message}", "ServiceContainer");
                throw;
            }
        }

        public void Dispose()
        {
            if (m_IsDisposed) return;

            lock (m_Lock)
            {
                // 销毁所有单例实例
                foreach (var instance in m_SingletonInstances.Values)
                {
                    if (instance is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception e)
                        {
                            GameLog.Error($"销毁服务实例时出错: {e.Message}", "ServiceContainer");
                        }
                    }
                }

                m_SingletonInstances.Clear();
                m_Services.Clear();
                m_IsDisposed = true;
            }

            GameLog.Info("服务容器已销毁", "ServiceContainer");
        }
    }

    /// <summary>
    /// 全局服务定位器
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceContainer s_Container;
        private static readonly object s_Lock = new object();

        public static IServiceContainer Current
        {
            get
            {
                if (s_Container == null)
                {
                    lock (s_Lock)
                    {
                        if (s_Container == null)
                        {
                            s_Container = new ServiceContainer();
                        }
                    }
                }
                return s_Container;
            }
        }

        public static void SetContainer(IServiceContainer container)
        {
            lock (s_Lock)
            {
                s_Container?.Dispose();
                s_Container = container;
            }
        }

        public static TService GetService<TService>() where TService : class
        {
            return Current.GetService<TService>();
        }

        public static TService GetRequiredService<TService>() where TService : class
        {
            return Current.GetRequiredService<TService>();
        }

        public static void Reset()
        {
            lock (s_Lock)
            {
                s_Container?.Dispose();
                s_Container = null;
            }
        }
    }
}