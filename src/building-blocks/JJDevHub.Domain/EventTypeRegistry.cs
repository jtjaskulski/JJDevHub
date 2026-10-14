using System.Reflection;

namespace JJDevHub.Domain
{

    public sealed class EventTypeRegistry
    {
        private readonly Dictionary<string, Type> _typesByName;
        private readonly Dictionary<Type, string> _namesByType;

        private EventTypeRegistry(
            Dictionary<string, Type> typesByName,
            Dictionary<Type, string> namesByType)
        {
            _typesByName = typesByName;
            _namesByType = namesByType;
        }

        public static EventTypeRegistry FromAssemblies(Type marker, params Assembly[] assemblies)
        {
            var typesByName = new Dictionary<string, Type>();
            var namesByType = new Dictionary<Type, string>();

            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    Register(marker, type, typesByName, namesByType);
                }
            }

            return new EventTypeRegistry(typesByName, namesByType);
        }

        public string NameOf(Type type)
        {
            if (_namesByType.TryGetValue(type, out var name))
            {
                return name;
            }

            throw new InvalidOperationException($"Type '{type}' is not a registered event.");
        }

        public Type? TypeOf(string name)
        {
            if (_typesByName.TryGetValue(name, out var type))
            {
                return type;
            }

            return null;
        }

        private static void Register(
            Type marker,
            Type type,
            Dictionary<string, Type> typesByName,
            Dictionary<Type, string> namesByType)
        {
            if (!type.IsClass || type.IsAbstract || !marker.IsAssignableFrom(type))
            {
                return;
            }

            EventNameAttribute? attribute = type.GetCustomAttribute<EventNameAttribute>(inherit: false);
            if (attribute is null)
            {
                throw new InvalidOperationException($"Type '{type}' is missing EventNameAttribute.");
            }

            if (typesByName.TryGetValue(attribute.Name, out var existing))
            {
                throw new InvalidOperationException(
                    $"Event name '{attribute.Name}' is used by '{existing}' and '{type}'.");
            }

            typesByName.Add(attribute.Name, type);
            namesByType.Add(type, attribute.Name);
        }
    }
}
