// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

#if ANALYZER
namespace Trestel.SqlQueryAnalyzer.Design
#else
namespace Trestel.Database.Design
#endif
{
    /// <summary>
    /// Contains enum of supported database types.
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// The SQL server
        /// </summary>
        SqlServer
    }
}
