using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MrHotkeys.Reflection.Emit.Templating.Extensions
{
    public static class TypeBuilderExtensions
    {
        public static MethodBuilder DefineMethodAndOverride(this TypeBuilder typeBuilder, string name,
            int genericParameterCount, BindingFlags bindingAttr, params Type[] parameterTypes)
        {
            if (typeBuilder is null)
                throw new ArgumentNullException(nameof(typeBuilder));
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (parameterTypes is null)
                throw new ArgumentNullException(nameof(parameterTypes));

            var baseMethod = typeBuilder
                .BaseType
                .GetMethod(name, genericParameterCount, bindingAttr, parameterTypes);

            if (baseMethod.IsFinal)
                throw new InvalidOperationException($"Cannot override sealed method {name}!");
            if (!baseMethod.IsVirtual)
                throw new InvalidOperationException($"Cannot override non-virtual method {name}!");

            var overMethodBuilder = typeBuilder.DefineMethod(
                name: baseMethod.Name,
                attributes: baseMethod.Attributes & ~MethodAttributes.Abstract,
                callingConvention: baseMethod.CallingConvention,
                returnType: baseMethod.ReturnType,
                parameterTypes: baseMethod
                    .GetParameters()
                    .Select(p => p.ParameterType)
                    .ToArray()
            );

            typeBuilder.DefineMethodOverride(overMethodBuilder, baseMethod);

            return overMethodBuilder;
        }

        public static PropertyBuilder DefineAutoProperty(this TypeBuilder typeBuilder, string name,
            Type type, MethodAttributes setterAttributes, MethodAttributes getterAttributes)
        {
            if (typeBuilder is null)
                throw new ArgumentNullException(nameof(typeBuilder));
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            var backingFieldBuilder = DefineAutoPropertyBackingField(typeBuilder, name, type);

            var propertyBuilder = typeBuilder.DefineProperty(
                name: name,
                attributes: PropertyAttributes.None,
                returnType: type,
                parameterTypes: Type.EmptyTypes
            );

            var getterBuilder = DefineAutoPropertyGetter(typeBuilder, name, type, setterAttributes, backingFieldBuilder);
            propertyBuilder.SetGetMethod(getterBuilder);

            var setterBuilder = DefineAutoPropertySetter(typeBuilder, name, type, getterAttributes, backingFieldBuilder);
            propertyBuilder.SetSetMethod(setterBuilder);

            return propertyBuilder;
        }

        public static FieldBuilder DefineAutoPropertyBackingField(this TypeBuilder typeBuilder, string propertyName, Type type) =>
            DefineAutoPropertyBackingField(typeBuilder, propertyName, type, FieldAttributes.Private);

        public static FieldBuilder DefineAutoPropertyBackingField(this TypeBuilder typeBuilder, string propertyName, Type type, FieldAttributes attributes)
        {
            if (typeBuilder is null)
                throw new ArgumentNullException(nameof(typeBuilder));
            if (propertyName is null)
                throw new ArgumentNullException(nameof(propertyName));
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            return typeBuilder.DefineField(
                fieldName: $"<{propertyName}>k__BackingField",
                type: type,
                attributes: attributes
            );
        }

        public static MethodBuilder DefineAutoPropertyGetter(this TypeBuilder typeBuilder, string propertyName,
            Type type, MethodAttributes attributes, FieldBuilder backingFieldBuilder)
        {
            if (typeBuilder is null)
                throw new ArgumentNullException(nameof(typeBuilder));
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (backingFieldBuilder is null)
                throw new ArgumentNullException(nameof(backingFieldBuilder));

            var getterBuilder = typeBuilder.DefineMethod(
                name: $"get_{propertyName}",
                attributes: attributes,
                returnType: type,
                parameterTypes: Type.EmptyTypes);

            var il = getterBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, backingFieldBuilder);
            il.Emit(OpCodes.Ret);

            return getterBuilder;
        }

        public static MethodBuilder DefineAutoPropertySetter(this TypeBuilder typeBuilder, string propertyName,
            Type type, MethodAttributes attributes, FieldBuilder backingFieldBuilder)
        {
            if (typeBuilder is null)
                throw new ArgumentNullException(nameof(typeBuilder));
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            if (backingFieldBuilder is null)
                throw new ArgumentNullException(nameof(backingFieldBuilder));

            var setterBuilder = typeBuilder.DefineMethod(
                name: $"set_{propertyName}",
                attributes: attributes,
                returnType: null,
                parameterTypes: new[] { type });

            var il = setterBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, backingFieldBuilder);
            il.Emit(OpCodes.Ret);

            return setterBuilder;
        }
    }
}