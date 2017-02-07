// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Generic;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Extensions;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Trestel.SqlQueryAnalyzer.CallSiteAnalyzers
{
    /// <summary>
    /// Contains common logic for call site analyzers.
    /// </summary>
    /// <seealso cref="Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis.ICallSiteAnalyzer" />
    public abstract class BaseCallSiteAnalyzer : ICallSiteAnalyzer
    {
        /// <summary>
        /// Determines whether this instance can analyze call site given the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        ///   <c>true</c> if this instance can analyze call site; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool CanAnalyzeCallSite(CallSiteContext context);

        /// <summary>
        /// Analyzes the call site.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Result of call site normalization
        /// </returns>
        public abstract Result<NormalizedCallSite> AnalyzeCallSite(CallSiteContext context);

        /// <summary>
        /// Verifies the call site.
        /// </summary>
        /// <param name="callSiteData">The call site data.</param>
        /// <param name="queryData">The query data.</param>
        /// <param name="context">The context.</param>
        public void VerifyCallSite(NormalizedCallSite callSiteData, ValidatedQuery queryData, CallSiteVerificationContext context)
        {
            if (callSiteData.CheckParameters)
            {
                CheckParameterMapping(callSiteData, queryData, context);
            }

            if (callSiteData.CheckResult)
            {
                CheckResultMapping(callSiteData, queryData, context);
            }
        }

        /// <summary>
        /// Checks the parameter mapping.
        /// </summary>
        /// <param name="callSite">The call site.</param>
        /// <param name="validatedQuery">The validated query.</param>
        /// <param name="context">The context.</param>
        protected virtual void CheckParameterMapping(NormalizedCallSite callSite, ValidatedQuery validatedQuery, CallSiteVerificationContext context)
        {
            var unusedParameters = new List<Parameter>(callSite.InputParameters);
            var missingParameters = new List<ParameterInfo>();

            for (int i = 0; i < validatedQuery.Parameters.Length; i++)
            {
                var queryParameter = validatedQuery.Parameters[i];
                bool found = false;
                for (int j = 0; j < callSite.InputParameters.Length; j++)
                {
                    var parameter = callSite.InputParameters[j];
                    if (queryParameter.ParameterName == parameter.ParameterName)
                    {
                        found = true;
                        unusedParameters.Remove(parameter);

                        var queryParameterType = queryParameter.ParameterType.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        if (queryParameterType != null && !parameter.ParameterType.CanAssign(queryParameterType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateParameterTypeMismatchDiagnostic(context.CallSiteNode.GetLocation(), queryParameter.ParameterName, queryParameterType, parameter.ParameterType));
                        }

                        break;
                    }
                }

                if (!found)
                {
                    missingParameters.Add(queryParameter);
                }
            }

            // report diagnostic
            if (unusedParameters.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedParameterDiagnostic(context.CallSiteNode.GetLocation(), unusedParameters));
            }

            if (missingParameters.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingParameterDiagnostic(context.CallSiteNode.GetLocation(), missingParameters, context.SemanticModel.Compilation));
            }
        }

        /// <summary>
        /// Checks the result mapping.
        /// </summary>
        /// <param name="callSite">The call site.</param>
        /// <param name="validatedQuery">The validated query.</param>
        /// <param name="context">The context.</param>
        protected virtual void CheckResultMapping(NormalizedCallSite callSite, ValidatedQuery validatedQuery, CallSiteVerificationContext context)
        {
            // special case if we are directly mapping to primitive type
            if (callSite.ExpectedFields.Length == 1 && callSite.ExpectedFields[0].IsAnonymous)
            {
                var fieldType = callSite.ExpectedFields[0].FieldType;
                if (validatedQuery.OutputColumns.Length != 1)
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateExpectedSingleColumnInQueryResultDiagnostic(context.CallSiteNode.GetLocation(), fieldType));
                    return;
                }

                var columnType = validatedQuery.OutputColumns[0].Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                if (columnType != null && !columnType.CanAssign(fieldType, context.SemanticModel.Compilation))
                {
                    context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateTypeMismatchDiagnostic(context.CallSiteNode.GetLocation(), columnType, fieldType));
                }

                return;
            }

            var unusedColumns = new List<ColumnInfo>(validatedQuery.OutputColumns);
            var unusedFields = new List<ResultField>();

            for (int i = 0; i < callSite.ExpectedFields.Length; i++)
            {
                var field = callSite.ExpectedFields[i];
                bool found = false;
                for (int j = 0; j < validatedQuery.OutputColumns.Length; j++)
                {
                    var column = validatedQuery.OutputColumns[j];
                    if (field.FieldName == column.Name)
                    {
                        found = true;
                        unusedColumns.Remove(column);

                        var columnType = column.Type.ConvertFromRuntimeType(context.SemanticModel.Compilation);
                        if (columnType != null && !columnType.CanAssign(field.FieldType, context.SemanticModel.Compilation))
                        {
                            context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreatePropertyTypeMismatchDiagnostic(context.CallSiteNode.GetLocation(), column.Name, columnType, field.FieldType));
                        }

                        break;
                    }
                }

                if (!found)
                {
                    unusedFields.Add(field);
                }
            }

            // report diagnostic
            if (unusedFields.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateMissingColumnsInQueryResultDiagnostic(context.CallSiteNode.GetLocation(), unusedFields));
            }

            if (unusedColumns.Count > 0)
            {
                context.ReportDiagnostic(SqlQueryAnalyzerDiagnostic.CreateUnusedColumnsInQueryResultDiagnostic(context.CallSiteNode.GetLocation(), unusedColumns));
            }
        }
    }
}
