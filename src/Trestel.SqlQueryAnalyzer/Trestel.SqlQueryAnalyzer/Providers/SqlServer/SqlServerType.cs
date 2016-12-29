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
        Unknown = 0,

        Image = 34,

        Text = 35,

        UniqueIdentifier = 36,

        Date = 40,

        Time = 41,

        DateTime2 = 42,

        DateTimeOffset = 43,

        TinyInt = 48,

        SmallInt = 52,

        Int = 56,

        SmallDateTime = 58,

        Real = 59,

        Money = 60,

        DateTime = 61,

        Float = 62,

        SqlVariant = 98,

        NText = 99,

        Bit = 104,

        Decimal = 106,

        Numeric = 108,

        SmallMoney = 122,

        BigInt = 127,

        VarBinary = 165,

        VarChar = 167,

        Binary = 173,

        Char = 175,

        Timestamp = 189,

        NVarChar = 231,

        NChar = 239,

        UDT = 240,

        Xml = 241
    }

    internal static class SqlServerTypeExtensions
    {
        /// <summary>
        /// Gets the type of the equivalent CLR type.
        /// Source: https://msdn.microsoft.com/en-us/library/cc716729(v=vs.110).aspx
        /// </summary>
        /// <param name="sqlServerType">Type of the SQL server.</param>
        /// <param name="isNullable">if set to <c>true</c> [is nullable].</param>
        /// <returns></returns>
        // TODO: change return type INamedType
        public static Type GetEquivalentCLRType(this SqlServerType sqlServerType, bool isNullable)
        {
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
