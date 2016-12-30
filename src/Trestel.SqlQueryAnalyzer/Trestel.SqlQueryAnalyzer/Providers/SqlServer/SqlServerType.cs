// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Providers.SqlServer
{
    /// <summary>
    /// SQL Server defined types.
    /// </summary>
    // TODO generate from database
    public enum SqlServerType
    {
        /// <summary>
        /// The unknown/invalid type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The image type
        /// </summary>
        Image = 34,

        /// <summary>
        /// The text type
        /// </summary>
        Text = 35,

        /// <summary>
        /// The unique identifier type
        /// </summary>
        UniqueIdentifier = 36,

        /// <summary>
        /// The date type
        /// </summary>
        Date = 40,

        /// <summary>
        /// The time type
        /// </summary>
        Time = 41,

        /// <summary>
        /// The datetime2 type
        /// </summary>
        DateTime2 = 42,

        /// <summary>
        /// The datetime offset type
        /// </summary>
        DateTimeOffset = 43,

        /// <summary>
        /// The tiny int type
        /// </summary>
        TinyInt = 48,

        /// <summary>
        /// The small int type
        /// </summary>
        SmallInt = 52,

        /// <summary>
        /// The int type
        /// </summary>
        Int = 56,

        /// <summary>
        /// The small datetime type
        /// </summary>
        SmallDateTime = 58,

        /// <summary>
        /// The real type
        /// </summary>
        Real = 59,

        /// <summary>
        /// The money type
        /// </summary>
        Money = 60,

        /// <summary>
        /// The datetime type
        /// </summary>
        DateTime = 61,

        /// <summary>
        /// The float type
        /// </summary>
        Float = 62,

        /// <summary>
        /// The SQL variant type
        /// </summary>
        SqlVariant = 98,

        /// <summary>
        /// The ntext type
        /// </summary>
        NText = 99,

        /// <summary>
        /// The bit type
        /// </summary>
        Bit = 104,

        /// <summary>
        /// The decimal type
        /// </summary>
        Decimal = 106,

        /// <summary>
        /// The numeric type
        /// </summary>
        Numeric = 108,

        /// <summary>
        /// The small money type
        /// </summary>
        SmallMoney = 122,

        /// <summary>
        /// The big int type
        /// </summary>
        BigInt = 127,

        /// <summary>
        /// The variable binary type
        /// </summary>
        VarBinary = 165,

        /// <summary>
        /// The variable character type
        /// </summary>
        VarChar = 167,

        /// <summary>
        /// The binary type
        /// </summary>
        Binary = 173,

        /// <summary>
        /// The character type
        /// </summary>
        Char = 175,

        /// <summary>
        /// The timestamp type
        /// </summary>
        Timestamp = 189,

        /// <summary>
        /// The variable ncharacter type
        /// </summary>
        NVarChar = 231,

        /// <summary>
        /// The ncharacter type
        /// </summary>
        NChar = 239,

        /// <summary>
        /// The udt type
        /// </summary>
        UDT = 240,

        /// <summary>
        /// The XML type
        /// </summary>
        Xml = 241
    }

#pragma warning disable SA1649 // File name must match first type name
                              /// <summary>
                              /// Extension methods for <see cref="SqlServerType"/>.
                              /// </summary>
    internal static class SqlServerTypeExtensions
#pragma warning restore SA1649 // File name must match first type name
    {
        /// <summary>
        /// Gets the type of the equivalent CLR type.
        /// Source: https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
        /// </summary>
        /// <param name="sqlServerType">Type of the SQL server.</param>
        /// <param name="isNullable">if set to <c>true</c> [is nullable].</param>
        /// <returns>Runtime CLR type of corresponding SQL Server type.</returns>
        public static Type GetEquivalentCLRType(this SqlServerType sqlServerType, bool isNullable)
        {
            // TODO: change return type INamedType
            switch (sqlServerType)
            {
                case SqlServerType.UniqueIdentifier:
                    return isNullable ? typeof(Guid?) : typeof(Guid);
                case SqlServerType.Image:
                case SqlServerType.VarBinary:
                case SqlServerType.Binary:
                case SqlServerType.Timestamp:
                    return typeof(byte[]);
                case SqlServerType.Text:
                case SqlServerType.NText:
                case SqlServerType.VarChar:
                case SqlServerType.Char:
                case SqlServerType.NVarChar:
                case SqlServerType.NChar:
                case SqlServerType.Xml:
                    return typeof(string);
                case SqlServerType.Date:
                case SqlServerType.DateTime2:
                case SqlServerType.SmallDateTime:
                case SqlServerType.DateTime:
                    return isNullable ? typeof(DateTime?) : typeof(DateTime);
                case SqlServerType.Time:
                    return isNullable ? typeof(TimeSpan?) : typeof(TimeSpan);
                case SqlServerType.DateTimeOffset:
                    return isNullable ? typeof(DateTimeOffset?) : typeof(DateTimeOffset);
                case SqlServerType.TinyInt:
                    return isNullable ? typeof(byte?) : typeof(byte);
                case SqlServerType.SmallInt:
                    return isNullable ? typeof(short?) : typeof(short);
                case SqlServerType.Int:
                    return isNullable ? typeof(int?) : typeof(int);
                case SqlServerType.Real:
                    return isNullable ? typeof(float?) : typeof(float);
                case SqlServerType.Money:
                case SqlServerType.Decimal:
                case SqlServerType.Numeric:
                case SqlServerType.SmallMoney:
                    return isNullable ? typeof(decimal?) : typeof(decimal);
                case SqlServerType.Float:
                    return isNullable ? typeof(double?) : typeof(double);
                case SqlServerType.SqlVariant:
                case SqlServerType.UDT:
                    return typeof(object);
                case SqlServerType.Bit:
                    return isNullable ? typeof(bool?) : typeof(bool);
                case SqlServerType.BigInt:
                    return isNullable ? typeof(long?) : typeof(long);
                default:
                    return typeof(void);
            }
        }
    }
}
