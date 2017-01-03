// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Threading.Tasks;
using NUnit.Framework;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure.Models;
using Trestel.SqlQueryAnalyzer.Services;

namespace Tests.Tests
{
    [TestFixture]
    public class CachingServiceUsage
    {
        [Test]
        public void UncachedAndCachedValidationResult()
        {
            var service = new CachingService();
            var connectionData = new ConnectionStringData("<test>", DatabaseType.SqlServer);

            var result = service.GetOrAddValidationResult(connectionData, "<query>", () => ValidationResult.Failure());
            Assert.NotNull(result);
            Assert.AreSame(result, service.GetOrAddValidationResult(connectionData, "<query>", () => ValidationResult.Failure()));
        }

        [Test]
        public async Task UncachedAndCachedValidationResultForAsync()
        {
            var service = new CachingService();
            var connectionData = new ConnectionStringData("<test>", DatabaseType.SqlServer);

            var result = await service.GetOrAddValidationResultAsync(connectionData, "<query>", () => Task.FromResult(ValidationResult.Failure()));
            Assert.NotNull(result);
            Assert.AreSame(result, await service.GetOrAddValidationResultAsync(connectionData, "<query>", () => Task.FromResult(ValidationResult.Failure())));
        }
    }
}
