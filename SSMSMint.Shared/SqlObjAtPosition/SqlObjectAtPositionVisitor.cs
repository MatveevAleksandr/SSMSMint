using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace SSMSMint.Shared.SqlObjAtPosition
{
    public class SqlObjectAtPositionVisitor(int line, int column, string defaultServer, string defaultDatabase) : TSqlFragmentVisitor
    {
        public SqlObject SqlObjectUnderCursor { get; private set; }
        private const string DefaultSchema = "dbo";
        private string _lastUseDatabase; // Обход дерева идет сверху вниз. Поэтому когда уткнемся в какой то объект, то тут будет храниться последняя операция USE с базой внутри которой будет исполнен скрипт

        public override void Visit(SchemaObjectName fragment)
        {
            if (!IsCaretInsideFragment(fragment))
                return;

            var contextServerName = fragment.ServerIdentifier?.Value ?? defaultServer;
            var contextDatabaseName = fragment.DatabaseIdentifier?.Value ?? _lastUseDatabase ?? defaultDatabase;

            if (IsCaretInsideFragment(fragment.ServerIdentifier))
            {
                SqlObjectUnderCursor = new ServerSqlObject(contextServerName, fragment.ServerIdentifier.Value);
            }

            if (IsCaretInsideFragment(fragment.DatabaseIdentifier))
            {
                SqlObjectUnderCursor = new DatabaseSqlObject(contextServerName, fragment.DatabaseIdentifier.Value);
            }

            if (IsCaretInsideFragment(fragment.SchemaIdentifier))
            {
                SqlObjectUnderCursor = new SchemaSqlObject(contextServerName, contextDatabaseName, fragment.SchemaIdentifier.Value);
            }

            base.Visit(fragment);
        }

        // Stored Procedure
        public override void Visit(ProcedureReferenceName fragment)
        {
            if (IsCaretInsideFragment(fragment.ProcedureReference.Name.BaseIdentifier))
                SetOnStoredProcedureSqlObject(fragment.ProcedureReference);

            base.Visit(fragment);
        }

        // Table View Synonim
        public override void Visit(NamedTableReference fragment)
        {
            if (IsCaretInsideFragment(fragment.SchemaObject.BaseIdentifier))
                SetOnNamedTableReferenceSqlObject(fragment.SchemaObject);

            base.Visit(fragment);
        }

        // Table function
        public override void Visit(SchemaObjectFunctionTableReference fragment)
        {
            if (!IsCaretInsideFragment(fragment.SchemaObject.BaseIdentifier))
                return;

            var contextServerName = fragment.SchemaObject.ServerIdentifier?.Value ?? defaultServer;
            var contextDatabaseName = fragment.SchemaObject.DatabaseIdentifier?.Value ?? _lastUseDatabase ?? defaultDatabase;
            var contextSchemaName = fragment.SchemaObject.SchemaIdentifier?.Value ?? DefaultSchema;

            SqlObjectUnderCursor = new TableFunctionSqlObject(contextServerName, contextDatabaseName, contextSchemaName, fragment.SchemaObject.BaseIdentifier.Value);

            base.Visit(fragment);
        }

        // Use [DataBase]
        public override void Visit(UseStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.DatabaseName))
            {
                SqlObjectUnderCursor = new DatabaseSqlObject(defaultServer, fragment.DatabaseName.Value);
            }

            _lastUseDatabase = fragment.DatabaseName.Value;

            base.Visit(fragment);
        }

        // Scalar Function
        public override void Visit(FunctionCall fragment)
        {
            if (!IsCaretInsideFragment(fragment))
                return;

            string contextServerName = defaultServer;
            string contextDatabaseName = _lastUseDatabase ?? defaultDatabase;
            string contextSchemaName = DefaultSchema;

            // Тупо, но иначе не нашел
            if (fragment.CallTarget != null && fragment.CallTarget is MultiPartIdentifierCallTarget callTarget)
            {
                var identifiers = callTarget.MultiPartIdentifier.Identifiers;

                if (identifiers.Count > 3)
                {
                    return;
                }

                if (identifiers.Count == 3)
                {
                    contextServerName = identifiers[0].Value;
                    contextDatabaseName = identifiers[1].Value;
                    contextSchemaName = identifiers[2].Value;

                    if (IsCaretInsideFragment(identifiers[0]))
                    {
                        SqlObjectUnderCursor = new ServerSqlObject(contextServerName, contextServerName);
                        return;
                    }
                    if (IsCaretInsideFragment(identifiers[1]))
                    {
                        SqlObjectUnderCursor = new DatabaseSqlObject(contextServerName, contextDatabaseName);
                        return;
                    }
                    if (IsCaretInsideFragment(identifiers[2]))
                    {
                        SqlObjectUnderCursor = new SchemaSqlObject(contextServerName, contextDatabaseName, contextSchemaName);
                        return;
                    }
                }
                else if (identifiers.Count == 2)
                {
                    contextDatabaseName = identifiers[0].Value;
                    contextSchemaName = identifiers[1].Value;

                    if (IsCaretInsideFragment(identifiers[0]))
                    {
                        SqlObjectUnderCursor = new DatabaseSqlObject(contextServerName, contextDatabaseName);
                        return;
                    }
                    if (IsCaretInsideFragment(identifiers[1]))
                    {
                        SqlObjectUnderCursor = new SchemaSqlObject(contextServerName, contextDatabaseName, contextSchemaName);
                        return;
                    }
                }
                else if (identifiers.Count == 1)
                {
                    contextSchemaName = identifiers[0].Value;

                    if (IsCaretInsideFragment(identifiers[0]))
                    {
                        SqlObjectUnderCursor = new SchemaSqlObject(contextServerName, contextDatabaseName, contextSchemaName);
                        return;
                    }
                }
            }

            if (IsCaretInsideFragment(fragment.FunctionName))
            {
                SqlObjectUnderCursor = new ScalarFunctionSqlObject(contextServerName, contextDatabaseName, contextSchemaName, fragment.FunctionName.Value);
            }

            base.Visit(fragment);
        }

        // User defined data types
        public override void Visit(UserDataTypeReference fragment)
        {
            if (!IsCaretInsideFragment(fragment.Name.BaseIdentifier))
                return;

            var contextServerName = fragment.Name.ServerIdentifier?.Value ?? defaultServer;
            var contextDatabaseName = fragment.Name.DatabaseIdentifier?.Value ?? _lastUseDatabase ?? defaultDatabase;
            var contextSchemaName = fragment.Name.SchemaIdentifier?.Value ?? DefaultSchema;

            SqlObjectUnderCursor = new UserDefinedDataTypeSqlObject(contextServerName, contextDatabaseName, contextSchemaName, fragment.Name.BaseIdentifier.Value);

            base.Visit(fragment);
        }

        public override void Visit(AlterProcedureStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.ProcedureReference.Name.BaseIdentifier))
                SetOnStoredProcedureSqlObject(fragment.ProcedureReference);

            base.Visit(fragment);
        }
        public override void Visit(CreateProcedureStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.ProcedureReference.Name.BaseIdentifier))
                SetOnStoredProcedureSqlObject(fragment.ProcedureReference);

            base.Visit(fragment);
        }
        public override void Visit(CreateOrAlterProcedureStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.ProcedureReference.Name.BaseIdentifier))
                SetOnStoredProcedureSqlObject(fragment.ProcedureReference);

            base.Visit(fragment);
        }

        public override void Visit(CreateOrAlterViewStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.SchemaObjectName.BaseIdentifier))
                SetOnNamedTableReferenceSqlObject(fragment.SchemaObjectName);

            base.Visit(fragment);
        }

        public override void Visit(CreateViewStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.SchemaObjectName.BaseIdentifier))
                SetOnNamedTableReferenceSqlObject(fragment.SchemaObjectName);

            base.Visit(fragment);
        }

        public override void Visit(AlterViewStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.SchemaObjectName.BaseIdentifier))
                SetOnNamedTableReferenceSqlObject(fragment.SchemaObjectName);

            base.Visit(fragment);
        }

        public override void Visit(CreateTableStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.SchemaObjectName.BaseIdentifier))
                SetOnNamedTableReferenceSqlObject(fragment.SchemaObjectName);

            base.Visit(fragment);
        }

        public override void Visit(AlterTableStatement fragment)
        {
            if (IsCaretInsideFragment(fragment.SchemaObjectName.BaseIdentifier))
                SetOnNamedTableReferenceSqlObject(fragment.SchemaObjectName);

            base.Visit(fragment);
        }

        private void SetOnStoredProcedureSqlObject(ProcedureReference procedureReference)
        {
            var contextServerName = procedureReference.Name.ServerIdentifier?.Value ?? defaultServer;
            var contextDatabaseName = procedureReference.Name.DatabaseIdentifier?.Value ?? _lastUseDatabase ?? defaultDatabase;
            var contextSchemaName = procedureReference.Name.SchemaIdentifier?.Value ?? DefaultSchema;

            SqlObjectUnderCursor = new StoredProcedureSqlObject
                (
                    contextServerName,
                    contextDatabaseName,
                    contextSchemaName,
                    procedureReference.Name.BaseIdentifier.Value,
                    procedureReference.Number?.Value
                );
        }

        private void SetOnNamedTableReferenceSqlObject(SchemaObjectName schemaObjectName)
        {
            var contextServerName = schemaObjectName.ServerIdentifier?.Value ?? defaultServer;
            var contextDatabaseName = schemaObjectName.DatabaseIdentifier?.Value ?? _lastUseDatabase ?? defaultDatabase;
            var contextSchemaName = schemaObjectName.SchemaIdentifier?.Value ?? DefaultSchema;

            SqlObjectUnderCursor = new NamedTableReferenceSqlObject(contextServerName, contextDatabaseName, contextSchemaName, schemaObjectName.BaseIdentifier.Value);
        }

        private bool IsCaretInsideFragment(TSqlFragment fragment)
        {
            if (fragment == null)
            {
                return false;
            }

            return line == fragment.StartLine &&
                   column >= fragment.StartColumn &&
                   column <= fragment.StartColumn + fragment.FragmentLength - 1;
        }
    }
}
