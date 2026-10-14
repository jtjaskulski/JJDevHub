using System.Reflection;
using System.Reflection.Emit;
using JJDevHub.Domain;

namespace JJDevHub.Domain.Tests.Fixtures
{

    public static class EventFixtureAssembly
    {
        public static Assembly Create(params (string TypeName, string? EventName)[] events)
        {
            var assemblyName = new AssemblyName("EventFixtures-" + Guid.NewGuid().ToString("N"));
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Events");

            foreach ((string typeName, string? eventName) in events)
            {
                DefineEvent(module, typeName, eventName);
            }

            return assembly;
        }

        private static void DefineEvent(ModuleBuilder module, string typeName, string? eventName)
        {
            TypeBuilder type = module.DefineType(
                typeName,
                TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
            type.AddInterfaceImplementation(typeof(IDomainEvent));
            DefineProperty(type, nameof(IDomainEvent.EventId), typeof(Guid));
            DefineProperty(type, nameof(IDomainEvent.DateOccurred), typeof(DateTimeOffset));
            ApplyName(type, eventName);

            if (type.CreateType() is null)
            {
                throw new InvalidOperationException($"Could not create fixture type '{typeName}'.");
            }
        }

        private static void ApplyName(TypeBuilder type, string? eventName)
        {
            if (eventName is null)
            {
                return;
            }

            ConstructorInfo? constructor = typeof(EventNameAttribute).GetConstructor([typeof(string)]);
            if (constructor is null)
            {
                throw new InvalidOperationException("EventNameAttribute constructor is missing.");
            }

            type.SetCustomAttribute(new CustomAttributeBuilder(constructor, [eventName]));
        }

        private static void DefineProperty(TypeBuilder type, string name, Type propertyType)
        {
            FieldBuilder field = type.DefineField("_" + name, propertyType, FieldAttributes.Private);
            PropertyBuilder property = type.DefineProperty(name, PropertyAttributes.None, propertyType, null);
            MethodBuilder getter = type.DefineMethod(
                "get_" + name,
                MethodAttributes.Public
                    | MethodAttributes.Virtual
                    | MethodAttributes.SpecialName
                    | MethodAttributes.HideBySig,
                propertyType,
                Type.EmptyTypes);
            ILGenerator il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            property.SetGetMethod(getter);

            MethodInfo? interfaceGetter = typeof(IDomainEvent).GetProperty(name)?.GetGetMethod();
            if (interfaceGetter is null)
            {
                throw new InvalidOperationException($"IDomainEvent.{name} getter is missing.");
            }

            type.DefineMethodOverride(getter, interfaceGetter);
        }
    }
}
