using SSMSMint.Core.Models;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.Linq;

namespace SSMSMint.Core.Visitors;

public class TextMarkerVisitor : TSqlFragmentVisitor
{
    private readonly HashSet<string> _declaredVars = new(); // Тут храним задекларированные переменные
    private readonly Dictionary<string, List<TextSpan>> _notUsedVars = new(); // Тут храним задекларированные, но неиспользуемые переменные
    private readonly Dictionary<string, List<TextSpan>> _notDeclaredVars = new(); // Тут храним использованные, но незадекларированные переменные


    public IReadOnlyList<TextSpan> NotUsedVars => _notUsedVars.Values.SelectMany(list => list).ToList();
    public IReadOnlyList<TextSpan> NotDeclaredVars => _notDeclaredVars.Values.SelectMany(list => list).ToList();

    public override void Visit(DeclareCursorStatement fragment) => SaveDeclaredVar(fragment.Name.Value, fragment.Name);

    public override void Visit(DeclareTableVariableStatement fragment) => SaveDeclaredVar(fragment.Body.VariableName.Value, fragment.Body.VariableName);

    public override void Visit(DeclareVariableElement fragment) => SaveDeclaredVar(fragment.VariableName.Value, fragment.VariableName);


    public override void Visit(OpenCursorStatement fragment) => ProcessFoundVar(fragment.Cursor.Name.Value, fragment);

    public override void Visit(VariableReference fragment) => ProcessFoundVar(fragment.Name, fragment);

    private void SaveDeclaredVar(string varName, TSqlFragment fragment)
    {
        varName = varName.ToLower();
        var sp = new TextPoint(fragment.StartLine - 1, fragment.StartColumn - 1);
        var ep = new TextPoint(fragment.StartLine - 1, fragment.StartColumn + fragment.FragmentLength - 1);
        var markerObject = new TextSpan(sp, ep);

        if (!_notUsedVars.ContainsKey(varName))
            _notUsedVars.Add(varName, new());

        _notUsedVars[varName].Add(markerObject);
        _declaredVars.Add(varName);
    }

    private void ProcessFoundVar(string varName, TSqlFragment fragment)
    {
        varName = varName.ToLower();

        _notUsedVars.Remove(varName);

        if (!_declaredVars.Contains(varName))
        {
            var sp = new TextPoint(fragment.StartLine - 1, fragment.StartColumn - 1);
            var ep = new TextPoint(fragment.StartLine - 1, fragment.StartColumn + fragment.FragmentLength - 1);
            var markerObject = new TextSpan(sp, ep);

            if (!_notDeclaredVars.ContainsKey(varName))
                _notDeclaredVars.Add(varName, new());

            _notDeclaredVars[varName].Add(markerObject);
        }
    }

    // Тут пытаемся обойти следующий кейс
    // Exec procedure @var = 1, @var2 = @intVar
    // @var \ @var2 в данном случае является VariableReference, но не является переменной которую нам надо учитывать в поиске.
    // Поэтому используем ExplicitVisit, сами управляем обходом детей. В паре @var = ... берем только правую часть и к ней применяем текущий visitor, чтобы в Visit(VariableReference) пошла только правая часть
    public override void ExplicitVisit(ExecuteParameter fragment)
    {
        fragment.ParameterValue.Accept(this);
    }
}
