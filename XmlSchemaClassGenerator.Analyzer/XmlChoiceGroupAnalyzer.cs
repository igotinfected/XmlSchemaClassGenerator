using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace XmlSchemaClassGenerator.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlChoiceGroupAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "XCGA001";

    private const string AttributeFullName = "XmlSchemaClassGenerator.Attributes.XmlChoiceGroupAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Conflicting XmlChoiceGroup arms",
        messageFormat: "Properties '{0}' and '{1}' belong to different arms of the same xs:choice group (groupId={2}, arms {3} and {4}) and must not both be set",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Setting properties from different arms of the same xs:choice group produces invalid XML. Only one arm of a choice group may be set at a time.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeObjectInitializer, SyntaxKind.ObjectInitializerExpression);
        context.RegisterSyntaxNodeAction(AnalyzeBlockAssignments, SyntaxKind.Block);
    }

    private static void AnalyzeObjectInitializer(SyntaxNodeAnalysisContext context)
    {
        var initializer = (InitializerExpressionSyntax)context.Node;

        // Collect all assigned properties with their choice group metadata.
        var assignments = new List<(string PropertyName, int GroupId, int ArmId, Location Location)>();

        foreach (var expression in initializer.Expressions)
        {
            if (!(expression is AssignmentExpressionSyntax assignment))
                continue;

            if (!(assignment.Left is IdentifierNameSyntax identifier))
                continue;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken);
            if (!(symbolInfo.Symbol is IPropertySymbol property))
                continue;

            if (TryGetChoiceGroupInfo(property, context.Compilation, out var groupId, out var armId))
            {
                assignments.Add((property.Name, groupId, armId, assignment.GetLocation()));
            }
        }

        ReportConflicts(context, assignments);
    }

    private static void AnalyzeBlockAssignments(SyntaxNodeAnalysisContext context)
    {
        var block = (BlockSyntax)context.Node;

        // Group straight-line assignments by receiver identity.
        // We reset tracking when we hit any non-assignment statement or a statement
        // that isn't a simple `receiver.Property = value;`.
        var receiverGroups = new Dictionary<ISymbol, List<(string PropertyName, int GroupId, int ArmId, Location Location)>>(SymbolEqualityComparer.Default);

        foreach (var statement in block.Statements)
        {
            // Only consider expression statements containing simple assignments.
            if (!(statement is ExpressionStatementSyntax exprStatement))
            {
                // Non-assignment statement breaks all sequences.
                ReportAndClear(context, receiverGroups);
                continue;
            }

            if (!(exprStatement.Expression is AssignmentExpressionSyntax assignment))
            {
                ReportAndClear(context, receiverGroups);
                continue;
            }

            if (assignment.Kind() != SyntaxKind.SimpleAssignmentExpression)
            {
                ReportAndClear(context, receiverGroups);
                continue;
            }

            // Must be `receiver.Property = ...`
            if (!(assignment.Left is MemberAccessExpressionSyntax memberAccess))
            {
                ReportAndClear(context, receiverGroups);
                continue;
            }

            var propertySymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);
            if (!(propertySymbolInfo.Symbol is IPropertySymbol property))
            {
                ReportAndClear(context, receiverGroups);
                continue;
            }

            // Identify the receiver symbol (the variable/parameter being assigned to).
            var receiverSymbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess.Expression, context.CancellationToken);
            var receiverSymbol = receiverSymbolInfo.Symbol;
            if (receiverSymbol == null)
            {
                continue;
            }

            if (TryGetChoiceGroupInfo(property, context.Compilation, out var groupId, out var armId))
            {
                if (!receiverGroups.TryGetValue(receiverSymbol, out var list))
                {
                    list = new List<(string, int, int, Location)>();
                    receiverGroups[receiverSymbol] = list;
                }

                list.Add((property.Name, groupId, armId, assignment.GetLocation()));
            }
        }

        // Report anything remaining at end of block.
        ReportAndClear(context, receiverGroups);
    }

    private static void ReportAndClear(
        SyntaxNodeAnalysisContext context,
        Dictionary<ISymbol, List<(string PropertyName, int GroupId, int ArmId, Location Location)>> receiverGroups)
    {
        foreach (var kvp in receiverGroups)
        {
            ReportConflicts(context, kvp.Value);
        }

        receiverGroups.Clear();
    }

    private static void ReportConflicts(
        SyntaxNodeAnalysisContext context,
        List<(string PropertyName, int GroupId, int ArmId, Location Location)> assignments)
    {
        if (assignments.Count < 2)
            return;

        // Group by GroupId, then check for conflicting ArmIds.
        var byGroup = new Dictionary<int, List<(string PropertyName, int ArmId, Location Location)>>();
        foreach (var (propertyName, groupId, armId, location) in assignments)
        {
            if (!byGroup.TryGetValue(groupId, out var list))
            {
                list = new List<(string, int, Location)>();
                byGroup[groupId] = list;
            }

            list.Add((propertyName, armId, location));
        }

        foreach (var kvp in byGroup)
        {
            var groupId = kvp.Key;
            var props = kvp.Value;

            // Find distinct arms.
            var distinctArms = new Dictionary<int, (string PropertyName, Location Location)>();
            foreach (var (propertyName, armId, location) in props)
            {
                if (!distinctArms.ContainsKey(armId))
                {
                    distinctArms[armId] = (propertyName, location);
                }
            }

            if (distinctArms.Count < 2)
                continue;

            // Report a diagnostic for each pair of conflicting arms.
            // Pick the first two distinct arms and report on the second one's location.
            var arms = distinctArms.ToList();
            for (var i = 1; i < arms.Count; i++)
            {
                var first = arms[0];
                var conflicting = arms[i];

                var diagnostic = Diagnostic.Create(
                    Rule,
                    conflicting.Value.Location,
                    additionalLocations: new[] { first.Value.Location },
                    conflicting.Value.PropertyName,
                    first.Value.PropertyName,
                    groupId,
                    conflicting.Key,
                    first.Key);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool TryGetChoiceGroupInfo(
        IPropertySymbol property,
        Compilation compilation,
        out int groupId,
        out int armId)
    {
        groupId = 0;
        armId = 0;

        var attributeType = compilation.GetTypeByMetadataName(AttributeFullName);
        if (attributeType == null)
            return false;

        foreach (var attr in property.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType))
                continue;

            var args = attr.ConstructorArguments;
            if (args.Length == 2 &&
                args[0].Value is int g &&
                args[1].Value is int a)
            {
                groupId = g;
                armId = a;
                return true;
            }
        }

        return false;
    }
}
