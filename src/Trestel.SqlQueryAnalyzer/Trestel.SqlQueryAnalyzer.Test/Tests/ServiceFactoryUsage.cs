// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Threading;
using Moq;
using NUnit.Framework;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;

namespace Tests
{
    [TestFixture]
    public class ServiceFactoryUsage
    {
        [Test]
        public void NoValidationProvider()
        {
            var factory = ServiceFactory.New().Build();

            Assert.IsNull(factory.GetQueryValidationProvider(new ConnectionStringData("<something>", DatabaseType.SqlServer)));
        }

        [Test]
        public void EmptyConnectionString()
        {
            var factory = ServiceFactory.New().Build();

            Assert.Catch<ArgumentException>(() => factory.GetQueryValidationProvider(new ConnectionStringData("", DatabaseType.SqlServer)));
        }

        [Test]
        public void RegisterAndRetrieveQueryValidationProvider()
        {
            var validationProvider = new Mock<IQueryValidationProvider>().Object;
            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connectionString) => validationProvider)
                .Build();

            Assert.AreSame(validationProvider, factory.GetQueryValidationProvider(new ConnectionStringData("<something>", DatabaseType.SqlServer)));
        }

        [Test]
        public void CacheQueryValidationProvider()
        {
            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connectionString) => new Mock<IQueryValidationProvider>().Object)
                .Build();
            var connectionData = new ConnectionStringData("<something>", DatabaseType.SqlServer);
            var otherConnectionData = new ConnectionStringData("<something other>", DatabaseType.SqlServer);

            var retrievedProvider = factory.GetQueryValidationProvider(connectionData);
            Assert.AreSame(retrievedProvider, factory.GetQueryValidationProvider(connectionData));
            Assert.AreNotSame(retrievedProvider, factory.GetQueryValidationProvider(otherConnectionData));
            Assert.AreSame(retrievedProvider, factory.GetQueryValidationProvider(connectionData));
        }

        [Test]
        public void NoCallSiteAnalyzer()
        {
            var factory = ServiceFactory.New().Build();
            var context = new CallSiteContext(null, null, default(CancellationToken));

            var analyzer = factory.GetCallSiteAnalyzer(context);

            Assert.IsNull(analyzer);
        }

        [Test]
        public void DefaultCallSiteAnalyzer()
        {
            var mockAnalyzer = new Mock<ICallSiteAnalyzer>();
            mockAnalyzer.Setup(x => x.CanAnalyzeCallSite(It.IsAny<CallSiteContext>())).Returns(true);
            var factory = ServiceFactory
                .New()
                .RegisterCallSiteAnalyzerInstance(mockAnalyzer.Object)
                .Build();
            var context = new CallSiteContext(null, null, default(CancellationToken));

            var analyzer = factory.GetCallSiteAnalyzer(context);

            Assert.IsNotNull(analyzer);
        }

        [Test]
        public void NoMatchingAnalyzer()
        {
            var mockAnalyzer = new Mock<ICallSiteAnalyzer>();
            mockAnalyzer.Setup(x => x.CanAnalyzeCallSite(It.IsAny<CallSiteContext>())).Returns(false);
            var factory = ServiceFactory
                .New()
                .RegisterCallSiteAnalyzerInstance(mockAnalyzer.Object)
                .Build();
            var context = new CallSiteContext(null, null, default(CancellationToken));

            var analyzer = factory.GetCallSiteAnalyzer(context);

            Assert.IsNull(analyzer);
        }

        [Test]
        public void MatchingByDefaultAnalyzer()
        {
            var falseMockAnalyzer = new Mock<ICallSiteAnalyzer>();
            falseMockAnalyzer.Setup(x => x.CanAnalyzeCallSite(It.IsAny<CallSiteContext>())).Returns(false);
            var trueMockAnalyzer = new Mock<ICallSiteAnalyzer>();
            trueMockAnalyzer.Setup(x => x.CanAnalyzeCallSite(It.IsAny<CallSiteContext>())).Returns(true);
            var trueAnalyzer = trueMockAnalyzer.Object;
            var factory = ServiceFactory
                .New()
                .RegisterCallSiteAnalyzerInstance(falseMockAnalyzer.Object)
                .RegisterCallSiteAnalyzerInstance(trueAnalyzer)
                .Build();
            var context = new CallSiteContext(null, null, default(CancellationToken));

            var analyzer = factory.GetCallSiteAnalyzer(context);

            Assert.AreSame(trueAnalyzer, analyzer);
        }
    }
}
