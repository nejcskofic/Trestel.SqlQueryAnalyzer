// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Trestel.SqlQueryAnalyzer.Extensions
{
    /// <summary>
    /// Contains helper methods for working with Type and ITypeSymbol classes.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Converts the type of from runtime.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="compilation">The compilation.</param>
        /// <returns>Corresponding <see cref="ITypeSymbol"/> for given <see cref="Type"/>.</returns>
        /// <exception cref="System.ArgumentNullException">Type of compilation is null.</exception>
        public static ITypeSymbol ConvertFromRuntimeType(this Type type, Compilation compilation)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            return ConvertFromRuntimeTypeInternal(type, compilation);
        }

        /// <summary>
        /// Determines whether given type symbol is basic type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>True if given type symbol represents basic type.</returns>
        public static bool IsBasicType(this ITypeSymbol type)
        {
            if (type == null) return false;

            INamedTypeSymbol namedType = null;
            if (type.TypeKind == TypeKind.Array)
            {
                var arrayType = type as IArrayTypeSymbol;
                return arrayType.ElementType.SpecialType == SpecialType.System_Byte;
            }

            namedType = type as INamedTypeSymbol;
            if (namedType == null) return false;

            if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                namedType = namedType.TypeArguments[0] as INamedTypeSymbol;
                if (namedType == null) return false;
            }

            switch (namedType.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_Char:
                case SpecialType.System_DateTime:
                case SpecialType.System_Decimal:
                case SpecialType.System_Double:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_Object:
                case SpecialType.System_SByte:
                case SpecialType.System_Single:
                case SpecialType.System_String:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                    return true;
            }

            if (namedType.ContainingNamespace.Name == "System")
            {
                switch (namedType.Name)
                {
                    case "Guid":
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If type symbol derives from <see cref="IEnumerable{T}"/> and type T is bound, return bound type T.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Containing type from <see cref="IEnumerable{T}"/></returns>
        public static ITypeSymbol TryGetUnderlyingTypeFromIEnumerableT(this ITypeSymbol type)
        {
            if (type == null) return null;

            if (type.TypeKind == TypeKind.Array)
            {
                return ((IArrayTypeSymbol)type).ElementType;
            }

            for (int i = 0; i < type.AllInterfaces.Length; i++)
            {
                var f = type.AllInterfaces[i];
                if (f.IsGenericType &&
                    f.TypeParameters.Length == 1 &&
                    f.ConstructedFrom != null &&
                    f.ConstructedFrom.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    return f.TypeArguments[0];
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether this instance can assign the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="compilation">The compilation.</param>
        /// <returns>True, if target symbol can be assigned to source (without explicit cast).</returns>
        public static bool CanAssign(this ITypeSymbol source, ITypeSymbol target, Compilation compilation)
        {
            var conversionStatus = compilation.ClassifyConversion(source, target);
            return conversionStatus.Exists && !conversionStatus.IsExplicit;
        }

        /// <summary>
        /// Get the properties with public setter.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>Enumerable of properties with public setter.</returns>
        public static IEnumerable<IPropertySymbol> GetPropertiesWithPublicSetter(this INamedTypeSymbol symbol)
        {
            if (symbol == null) yield break;

            var members = symbol.GetMembers();
            for (int i = 0; i < members.Length; i++)
            {
                var m = members[i];
                if (m.Kind != SymbolKind.Property) continue;
                var prop = (IPropertySymbol)m;

                if (prop.IsStatic || prop.IsIndexer || prop.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (prop.SetMethod == null || prop.SetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                yield return prop;
            }
        }

        /// <summary>
        /// Gets the properties with public getter.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>Enumerable of properties with public getter</returns>
        public static IEnumerable<IPropertySymbol> GetPropertiesWithPublicGetter(this INamedTypeSymbol symbol)
        {
            if (symbol == null) yield break;

            var members = symbol.GetMembers();
            for (int i = 0; i < members.Length; i++)
            {
                var m = members[i];
                if (m.Kind != SymbolKind.Property) continue;
                var prop = (IPropertySymbol)m;

                if (prop.IsStatic || prop.IsIndexer || prop.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                if (prop.GetMethod == null || prop.GetMethod.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }

                yield return prop;
            }
        }

        private static ITypeSymbol ConvertFromRuntimeTypeInternal(Type type, Compilation compilation)
        {
            if (type.IsArray)
            {
                return ConvertFromArrayTypeInternal(type, compilation);
            }

            // TODO: compilation.GetSpecialType faster for known types?
            var symbol = compilation.GetTypeByMetadataName(type.Namespace + "." + type.Name);
            if (symbol == null || !type.IsGenericType)
            {
                return symbol;
            }

            if (type.ContainsGenericParameters)
            {
                // unbound generic, nothing to do
                // TODO can we handle that?
                return null;
            }

            var genArgs = type.GetGenericArguments();
            ITypeSymbol[] symbGenArgs = new ITypeSymbol[genArgs.Length];
            for (int i = 0; i < genArgs.Length; i++)
            {
                symbGenArgs[i] = ConvertFromRuntimeTypeInternal(genArgs[i], compilation);
                if (symbGenArgs[i] == null)
                {
                    // unresolved?
                    return null;
                }
            }

            return symbol.Construct(symbGenArgs);
        }

        private static ITypeSymbol ConvertFromArrayTypeInternal(Type arrayType, Compilation compilation)
        {
            var rank = arrayType.GetArrayRank();
            var elementType = arrayType.GetElementType();

            var innerType = ConvertFromRuntimeTypeInternal(elementType, compilation);
            if (innerType == null) return null;

            return compilation.CreateArrayTypeSymbol(innerType, rank);
        }
    }
}
