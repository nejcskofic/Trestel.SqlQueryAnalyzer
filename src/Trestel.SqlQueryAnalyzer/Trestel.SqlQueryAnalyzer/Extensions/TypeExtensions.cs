using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Extensions
{
    internal static class TypeExtensions
    {
        #region ConvertFromRuntimeType
        /// <summary>
        /// Converts the type of from runtime.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="compilation">The compilation.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
        public static ITypeSymbol ConvertFromRuntimeType(this Type type, Compilation compilation)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            return ConvertFromRuntimeTypeInternal(type, compilation);
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
        #endregion

        #region IsPrimitiveType
        /// <summary>
        /// Determines whether [is c sharp primitive type] [the specified type].
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
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

            // TODO byte array
            return false;
        }
        #endregion

        #region CanAssign
        /// <summary>
        /// Determines whether this instance can assign the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="target">The target.</param>
        /// <param name="compilation">The compilation.</param>
        /// <returns></returns>
        public static bool CanAssign(this ITypeSymbol source, ITypeSymbol target, Compilation compilation)
        {
            var conversionStatus = compilation.ClassifyConversion(source, target);
            return conversionStatus.Exists && !conversionStatus.IsExplicit;
        }
        #endregion
    }
}
