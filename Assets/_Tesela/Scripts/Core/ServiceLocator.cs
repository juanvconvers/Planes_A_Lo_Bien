using System;
using System.Collections.Generic;

namespace Tesela.Core
{
    /// <summary>Registro mínimo de servicios. La raíz de composición (TeselaApp) registra todo en el arranque; los tests lo reinician.</summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            Services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static T Get<T>() where T : class
        {
            if (Services.TryGetValue(typeof(T), out object s)) return (T)s;
            throw new InvalidOperationException("Servicio no registrado: " + typeof(T).Name);
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out object s))
            {
                service = (T)s;
                return true;
            }
            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class => Services.ContainsKey(typeof(T));

        public static void Reset() => Services.Clear();
    }
}
