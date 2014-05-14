// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Query;
using Microsoft.Framework.Logging;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Microsoft.Data.Entity.Relational
{
    public partial class RelationalDataStore
    {
        private class AsyncQueryModelVisitor : AsyncEntityQueryModelVisitor
        {
            public AsyncQueryModelVisitor(IModel model)
                : base(model)
            {
            }

            protected override ExpressionTreeVisitor CreateQueryingExpressionTreeVisitor(
                IQuerySource querySource, bool isolateSubqueries = false)
            {
                return new RelationalQueryingExpressionTreeVisitor(_model, isolateSubqueries);
            }

            protected override ExpressionTreeVisitor CreateProjectionExpressionTreeVisitor(
                IQuerySource querySource, bool isolateSubqueries = false)
            {
                return new RelationalProjectionSubQueryExpressionTreeVisitor(_model, isolateSubqueries);
            }

            private static readonly MethodInfo _entityScanMethodInfo
                = typeof(AsyncQueryModelVisitor)
                    .GetTypeInfo().GetDeclaredMethod("EntityScan");

            [UsedImplicitly]
            private static IAsyncEnumerable<TEntity> EntityScan<TEntity>(QueryContext queryContext)
            {
                var entityType = queryContext.Model.GetEntityType(typeof(TEntity));

                var sql = new StringBuilder();

                sql.Append("SELECT ")
                    .AppendJoin(entityType.Properties.Select(p => p.StorageName))
                    .AppendLine()
                    .Append("FROM ")
                    .AppendLine(entityType.StorageName);

                return new Enumerable<TEntity>((RelationalQueryContext)queryContext, sql.ToString(), entityType);
            }

            private class RelationalQueryingExpressionTreeVisitor : QueryingExpressionTreeVisitor
            {
                public RelationalQueryingExpressionTreeVisitor(IModel model, bool isolateSubqueries)
                    : base(model, isolateSubqueries)
                {
                }

                protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
                {
                    var queryModelVisitor = new AsyncQueryModelVisitor(_model) { _isRootVisitor = _isolateSubqueries };

                    queryModelVisitor.VisitQueryModel(expression.QueryModel);

                    return queryModelVisitor._expression;
                }

                protected override Expression VisitEntityQueryable(Type elementType)
                {
                    return Expression.Call(
                        _entityScanMethodInfo.MakeGenericMethod(elementType),
                        _queryContextParameter);
                }
            }

            private class RelationalProjectionSubQueryExpressionTreeVisitor : RelationalQueryingExpressionTreeVisitor
            {
                public RelationalProjectionSubQueryExpressionTreeVisitor(IModel model, bool isolateSubqueries)
                    : base(model, isolateSubqueries)
                {
                }

                protected override Expression VisitSubQueryExpression(SubQueryExpression expression)
                {
                    return VisitProjectionSubQuery(expression, new AsyncQueryModelVisitor(_model));
                }
            }

            private sealed class Enumerable<T> : IAsyncEnumerable<T>
            {
                private readonly RelationalQueryContext _queryContext;
                private readonly string _sql;
                private readonly IEntityType _entityType;

                public Enumerable(RelationalQueryContext queryContext, string sql, IEntityType entityType)
                {
                    _queryContext = queryContext;
                    _sql = sql;
                    _entityType = entityType;
                }

                public IAsyncEnumerator<T> GetEnumerator()
                {
                    return new Enumerator<T>(_queryContext, _sql, _entityType);
                }
            }

            private sealed class Enumerator<T> : IAsyncEnumerator<T>
            {
                private readonly RelationalQueryContext _queryContext;
                private readonly string _sql;
                private readonly IEntityType _entityType;

                private RelationalConnection _connection;
                private DbCommand _command;
                private DbDataReader _reader;

                public Enumerator(RelationalQueryContext queryContext, string sql, IEntityType entityType)
                {
                    _queryContext = queryContext;
                    _sql = sql;
                    _entityType = entityType;
                }

                public Task<bool> MoveNext(CancellationToken cancellationToken)
                {
                    return _reader == null
                        ? InitializeAndReadAsync(cancellationToken)
                        : _reader.ReadAsync(cancellationToken);
                }

                private async Task<bool> InitializeAndReadAsync(
                    CancellationToken cancellationToken = default(CancellationToken))
                {
                    Contract.Assert(_connection == null);

                    var connection = _queryContext.Connection;
                    await connection.OpenAsync(cancellationToken);
                    _connection = connection;

                    _command = _connection.DbConnection.CreateCommand();
                    _command.CommandText = _sql;

                    _queryContext.Logger.WriteSql(_sql);

                    _reader = await _command.ExecuteReaderAsync(cancellationToken);

                    return await _reader.ReadAsync(cancellationToken);
                }

                public T Current
                {
                    get
                    {
                        if (_reader == null)
                        {
                            return default(T);
                        }

                        return (T)_queryContext.StateManager
                            .GetOrMaterializeEntry(
                                _entityType, _queryContext.ValueReaderFactory.Create(_reader)).Entity;
                    }
                }

                public void Dispose()
                {
                    if (_reader != null)
                    {
                        _reader.Dispose();
                    }

                    if (_command != null)
                    {
                        _command.Dispose();
                    }

                    if (_connection != null)
                    {
                        _connection.Close();
                    }
                }
            }
        }
    }
}
