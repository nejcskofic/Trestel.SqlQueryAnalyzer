// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Tests
{
    [TestFixture]
    public class DatabaseHintAttributeUsage : SqlQueryDiagnosticVerifier
    {
        [Test]
        public void UseOnMethod()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=1"")]

namespace TestNamespace
{
    [DatabaseHint(@""Data Source=2"")]
    class Program
    {
        [DatabaseHint(@""Data Source=3"")]
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));
            var mockupFactory = new Mock<Func<string, IQueryValidationProvider>>();
            mockupFactory.Setup(x => x("Data Source=3")).Returns(mockupValidationProvider.Object);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, mockupFactory.Object)
                .Build();

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            mockupFactory.Verify(x => x("Data Source=3"), Times.Once, "Factory should be called for provider instance.");
        }

        [Test]
        public void UseOnClass()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=1"")]

namespace TestNamespace
{
    [DatabaseHint(@""Data Source=2"")]
    class Program
    {
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));
            var mockupFactory = new Mock<Func<string, IQueryValidationProvider>>();
            mockupFactory.Setup(x => x("Data Source=2")).Returns(mockupValidationProvider.Object);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, mockupFactory.Object)
                .Build();

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            mockupFactory.Verify(x => x("Data Source=2"), Times.Once, "Factory should be called for provider instance.");
        }

        [Test]
        public void UseOnAssembly()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=1"")]

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));
            var mockupFactory = new Mock<Func<string, IQueryValidationProvider>>();
            mockupFactory.Setup(x => x("Data Source=1")).Returns(mockupValidationProvider.Object);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, mockupFactory.Object)
                .Build();

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            mockupFactory.Verify(x => x("Data Source=1"), Times.Once, "Factory should be called for provider instance.");
        }

        [Test]
        public void NoAttribute()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));
            var mockupFactory = new Mock<Func<string, IQueryValidationProvider>>();
            mockupFactory.Setup(x => x(It.IsAny<string>())).Returns(mockupValidationProvider.Object);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, mockupFactory.Object)
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingDatabaseHintAttributeDiagnosticId,
                Message = "Analysis cannot continue because there is no 'Trestel.Database.Design.DatabaseHintAttribute' attribute applied to method, class or assembly.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 32) }
            };

            VerifyCSharpDiagnostic(factory, test, expected);

            // verify that validation provider was called
            mockupFactory.Verify(x => x("Data Source=1"), Times.Never, "Factory should not be called if attribute is not present.");
        }

        [Test]
        public void UseOnMethodWithExplicitDatabaseType()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

namespace TestNamespace
{
    class Program
    {
        [DatabaseHint(@""Data Source=3"", DatabaseType.SqlServer)]
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));
            var mockupFactory = new Mock<Func<string, IQueryValidationProvider>>();
            mockupFactory.Setup(x => x("Data Source=3")).Returns(mockupValidationProvider.Object);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, mockupFactory.Object)
                .Build();

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            mockupFactory.Verify(x => x("Data Source=3"), Times.Once, "Factory should be called for provider instance.");
        }
    }
}
