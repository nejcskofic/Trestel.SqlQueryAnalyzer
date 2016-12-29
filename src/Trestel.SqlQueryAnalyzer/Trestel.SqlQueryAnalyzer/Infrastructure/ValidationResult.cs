using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    public sealed class ValidationResult
    {
        private readonly bool _isSuccess;
        private readonly ValidatedQuery _validatedQuery;
        private readonly ImmutableArray<string> _errors;

        private ValidationResult(ValidatedQuery validatedQuery)
        {
            _isSuccess = true;
            _validatedQuery = validatedQuery;
            _errors = ImmutableArray.Create<string>();
        }

        private ValidationResult(IEnumerable<string> errors)
        {
            _isSuccess = false;
            _validatedQuery = null;
            _errors = ImmutableArray.CreateRange(errors);
        }

        public static ValidationResult Success(ValidatedQuery validatedQuery)
        {
            if (validatedQuery == null) throw new ArgumentNullException(nameof(validatedQuery));
            return new ValidationResult(validatedQuery);
        }

        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            if (errors == null) errors = Enumerable.Empty<string>();
            return new ValidationResult(errors);
        }

        public bool IsSuccess
        {
            get
            {
                return _isSuccess;
            }
        }

        public ValidatedQuery ValidatedQuery
        {
            get
            {
                return _validatedQuery;
            }
        }

        public ImmutableArray<string> Errors
        {
            get
            {
                return _errors;
            }
        }
    }

    public sealed class ValidatedQuery
    {
        private readonly ImmutableArray<ColumnInfo> _outputColumns;

        private ValidatedQuery(IEnumerable<ColumnInfo> outputColumns)
        {
            _outputColumns = ImmutableArray.CreateRange(outputColumns);
        }

        public IReadOnlyList<ColumnInfo> OutputColumns
        {
            get
            {
                return _outputColumns;
            }
        }

        public class Builder
        {
            private readonly List<ColumnInfo> _outputColumns;

            public Builder()
            {
                _outputColumns = new List<ColumnInfo>();
            }

            public Builder AddOutputColumn(string name, Type type)
            {
                if (String.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));
                if (type == null) throw new ArgumentNullException(nameof(type));

                _outputColumns.Add(new ColumnInfo(_outputColumns.Count, name, type));
                return this;
            }

            public ValidatedQuery Build()
            {
                return new ValidatedQuery(_outputColumns);
            }
        }
    }

    public sealed class ColumnInfo
    {
        private readonly int _ordinal;
        private readonly string _name;
        private readonly Type _type;

        public int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public Type Type
        {
            get
            {
                return _type;
            }
        }

        public ColumnInfo(int ordinal, string name, Type type)
        {
            _ordinal = ordinal;
            _name = name;
            _type = type;
        }
    }
}
