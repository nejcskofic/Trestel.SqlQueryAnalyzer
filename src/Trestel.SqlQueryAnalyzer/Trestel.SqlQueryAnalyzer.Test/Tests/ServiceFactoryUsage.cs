// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using NUnit.Framework;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;

namespace Tests
{
    [TestFixture]
    public class ServiceFactoryUsage
    {
        [Test]
        public void EmptyServiceFactory()
        {
            var factory = new ServiceFactory();

            Assert.IsNull(factory.GetQueryValidationProvider("<something>", DatabaseType.SqlServer));
        }

        [Test]
        public void EmptyConnectionString()
        {
            var factory = new ServiceFactory();

            Assert.Catch<ArgumentException>(() => factory.GetQueryValidationProvider("", DatabaseType.SqlServer));
        }

        [Test]
        public void RegisterAndRetrieveQueryValidationProvider()
        {
            var factory = new ServiceFactory();
            var validationProvider = new MockupValidationProvider();
            factory.RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connectionString) => validationProvider);

            Assert.AreSame(validationProvider, factory.GetQueryValidationProvider("<something>", DatabaseType.SqlServer));
        }

        [Test]
        public void CacheQueryValidationProvider()
        {
            var factory = new ServiceFactory();
            factory.RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connectionString) => new MockupValidationProvider());

            var retrievedProvider = factory.GetQueryValidationProvider("<something>", DatabaseType.SqlServer);
            Assert.AreSame(retrievedProvider, factory.GetQueryValidationProvider("<something>", DatabaseType.SqlServer));
            Assert.AreNotSame(retrievedProvider, factory.GetQueryValidationProvider("<something other>", DatabaseType.SqlServer));
            Assert.AreSame(retrievedProvider, factory.GetQueryValidationProvider("<something>", DatabaseType.SqlServer));
        }
    }
}
