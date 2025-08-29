namespace SSMSMint.Shared.SqlObjAtPosition;

public abstract class SqlObject
{
    public abstract string ContextServerName { get; }
    public abstract string ObjName { get; }
    public abstract string GetParamsString();
}

public class ServerSqlObject(string contextServerName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}";
}

public class DatabaseSqlObject(string contextServerName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}";
}

public class StoredProcedureSqlObject(string contextServerName, string contextDatabaseName, string schemaName, string objName, string number) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;
    public string ContextDatabaseName => contextDatabaseName;
    public string SchemaName => schemaName;
    public string Number => number;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ContextDatabaseName)} - {contextDatabaseName}; {nameof(SchemaName)} - {schemaName}; {nameof(ObjName)} - {objName}; {nameof(Number)} - {number}";
}

public class NamedTableReferenceSqlObject(string contextServerName, string contextDatabaseName, string schemaName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;
    public string ContextDatabaseName => contextDatabaseName;
    public string SchemaName => schemaName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}; {nameof(ContextDatabaseName)} - {contextDatabaseName}; {nameof(SchemaName)} - {schemaName}";
}

public class TableFunctionSqlObject(string contextServerName, string contextDatabaseName, string schemaName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;
    public string ContextDatabaseName => contextDatabaseName;
    public string SchemaName => schemaName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}; {nameof(ContextDatabaseName)} - {contextDatabaseName}; {nameof(SchemaName)} - {schemaName}";
}

public class SchemaSqlObject(string contextServerName, string contextDatabaseName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;
    public string ContextDatabaseName => contextDatabaseName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}; {nameof(ContextDatabaseName)} - {contextDatabaseName}";
}

public class UserDefinedDataTypeSqlObject(string contextServerName, string contextDatabaseName, string schemaName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;
    public string ContextDatabaseName => contextDatabaseName;
    public string SchemaName => schemaName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}; {nameof(ContextDatabaseName)} - {contextDatabaseName}; {nameof(SchemaName)} - {schemaName}";
}

public class ScalarFunctionSqlObject(string contextServerName, string contextDatabaseName, string schemaName, string objName) : SqlObject
{
    public override string ContextServerName => contextServerName;
    public override string ObjName => objName;
    public string ContextDatabaseName => contextDatabaseName;
    public string SchemaName => schemaName;

    public override string GetParamsString() => $"{nameof(ContextServerName)} - {contextServerName}; {nameof(ObjName)} - {objName}; {nameof(ContextDatabaseName)} - {contextDatabaseName}; {nameof(SchemaName)} - {schemaName}";
}
