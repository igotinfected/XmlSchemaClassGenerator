using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace XmlSchemaClassGenerator.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class XmlChoiceGroupAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "XCGA001";
    public const string MissingRequiredChoiceGroupDiagnosticId = "XCGA002";

    private const string AttributeShortName = "XmlChoiceGroupAttribute";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Conflicting XmlChoiceGroup arms",
        messageFormat: "Properties '{0}' and '{1}' belong to different arms of the same xs:choice group (groupId={2}, arms {3} and {4}) and must not both be set",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Setting properties from different arms of the same xs:choice group produces invalid XML. Only one arm of a choice group may be set at a time.");

    private static readonly DiagnosticDescriptor MissingRequiredChoiceGroupRule = new(
        MissingRequiredChoiceGroupDiagnosticId,
        title: "Missing required XmlChoiceGroup arm",
        messageFormat: "Object initializer for '{0}' must set a property from required xs:choice group (groupId={1})",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Object initializers for types with required xs:choice groups must set at least one property from each required choice group.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule, MissingRequiredChoiceGroupRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        if (context.Operation is not IObjectCreationOperation objectCreation ||
            objectCreation.Initializer == null ||
            objectCreation.Type == null)
        {
            return;
        }

        var requiredGroupIds = GetRequiredChoiceGroupIds(objectCreation.Type);
        if (requiredGroupIds.Count == 0)
        {
            return;
        }

        var assignedGroupIds = GetAssignedChoiceGroupIds(objectCreation.Initializer);

        foreach (var requiredGroupId in requiredGroupIds)
        {
            if (assignedGroupIds.Contains(requiredGroupId))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                MissingRequiredChoiceGroupRule,
                objectCreation.Syntax.GetLocation(),
                objectCreation.Type.Name,
                requiredGroupId));
        }
    }

    private static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        foreach (var operationBlock in context.OperationBlocks)
        {
            // Walk up to the root operation (Parent == null).
            // ControlFlowGraph.Create requires a root operation — the operation blocks
            // provided by RegisterOperationBlockAction may be nested (e.g., IBlockOperation
            // as a child of IMethodBodyOperation).
            var root = operationBlock;
            while (root.Parent != null)
                root = root.Parent;

            ControlFlowGraph? cfg;
            try
            {
                cfg = root switch
                {
                    IMethodBodyOperation methodBody => ControlFlowGraph.Create(methodBody),
                    IConstructorBodyOperation ctorBody => ControlFlowGraph.Create(ctorBody),
                    IBlockOperation body => ControlFlowGraph.Create(body),
                    IFieldInitializerOperation fieldInit => ControlFlowGraph.Create(fieldInit),
                    IPropertyInitializerOperation propInit => ControlFlowGraph.Create(propInit),
                    _ => null
                };
            }
            catch (ArgumentException)
            {
                // Unsupported operation type for CFG creation.
                continue;
            }

            if (cfg == null)
                continue;

            AnalyzeControlFlowGraph(context, cfg);
        }
    }

    /// <summary>
    /// Runs a forward dataflow analysis over the CFG to detect choice group conflicts.
    ///
    /// State: for each tracked receiver, for each choice group, which arms have been assigned
    /// (with the property name and syntax location of the first assignment to that arm).
    ///
    /// Transfer: when we see <c>receiver.Property = value</c> and the property has
    /// <c>[XmlChoiceGroup(groupId, armId)]</c>, record the arm.
    ///
    /// Merge at join points: union the arm sets from all predecessors (must-analysis:
    /// an arm is in the merged state only if it appears on ALL incoming paths).
    ///
    /// Report: when a block's exit state has two distinct arm IDs for the same group
    /// on the same receiver.
    /// </summary>
    private static void AnalyzeControlFlowGraph(OperationBlockAnalysisContext context, ControlFlowGraph cfg)
    {
        var blocks = cfg.Blocks;
        if (blocks.Length == 0) return;

        // Per-block entry state. null means "not yet visited".
        var blockEntryState = new FlowState?[blocks.Length];

        // Track flow captures: captureId -> receiver key.
        // In the CFG, object initializers are lowered: `new Foo { P = v }` becomes
        //   #0 = new Foo()
        //   #0.P = v
        // We need to map the capture back to the eventual local it's assigned to,
        // or track it as its own receiver.
        var captureToReceiver = new Dictionary<CaptureId, ReceiverKey>();

        // Worklist algorithm (forward).
        var worklist = new Queue<int>();
        blockEntryState[0] = FlowState.Empty;
        worklist.Enqueue(0);

        // Track which diagnostics we've already reported to avoid duplicates.
        var reportedDiagnostics = new HashSet<string>();

        while (worklist.Count > 0)
        {
            var ordinal = worklist.Dequeue();
            var block = blocks[ordinal];

            if (!block.IsReachable) continue;

            var state = blockEntryState[ordinal]?.Clone() ?? FlowState.Empty;

            // Process operations in this block.
            foreach (var operation in block.Operations)
            {
                ProcessOperation(operation, state, captureToReceiver);
            }

            // Process branch value if present.
            if (block.BranchValue != null)
            {
                ProcessOperation(block.BranchValue, state, captureToReceiver);
            }

            // Report conflicts in the current block's exit state.
            ReportConflicts(context, state, reportedDiagnostics);

            // Propagate to successors.
            PropagateToSuccessor(block.FallThroughSuccessor, state, blockEntryState, worklist);
            PropagateToSuccessor(block.ConditionalSuccessor, state, blockEntryState, worklist);
        }
    }

    private static void PropagateToSuccessor(
        ControlFlowBranch? branch,
        FlowState currentState,
        FlowState?[] blockEntryState,
        Queue<int> worklist)
    {
        if (branch?.Destination == null) return;

        var destOrdinal = branch.Destination.Ordinal;
        var existingState = blockEntryState[destOrdinal];

        if (existingState == null)
        {
            // First time visiting this block — take current state as-is.
            blockEntryState[destOrdinal] = currentState.Clone();
            worklist.Enqueue(destOrdinal);
        }
        else
        {
            // Merge: union of arm assignments (may-analysis: if ANY path sets an arm, track it).
            var merged = existingState.MergeWith(currentState);
            if (!merged.Equals(existingState))
            {
                blockEntryState[destOrdinal] = merged;
                worklist.Enqueue(destOrdinal);
            }
        }
    }

    private static void ProcessOperation(
        IOperation operation,
        FlowState state,
        Dictionary<CaptureId, ReceiverKey> captureToReceiver)
    {
        // Walk nested operations (the CFG flattens most things, but some nesting remains).
        foreach (var child in operation.ChildOperations)
        {
            ProcessOperation(child, state, captureToReceiver);
        }

        switch (operation)
        {
            // Track flow captures: #N = <expression>
            // If the captured value is a local/parameter/field reference or another capture,
            // propagate the receiver mapping.
            case IFlowCaptureOperation capture:
                var capturedReceiver = GetReceiverKey(capture.Value, captureToReceiver);
                if (capturedReceiver != null)
                {
                    captureToReceiver[capture.Id] = capturedReceiver;
                }
                break;

            // Track simple assignments: receiver.Property = value
            case ISimpleAssignmentOperation assignment:
                if (assignment.Target is IPropertyReferenceOperation propRef)
                {
                    var property = propRef.Property;
                    var choiceGroupInfos = GetChoiceGroupInfos(property).ToList();
                    if (choiceGroupInfos.Count > 0)
                    {
                        var receiver = GetReceiverKey(propRef.Instance, captureToReceiver);
                        if (receiver != null)
                        {
                            foreach (var choiceGroupInfo in choiceGroupInfos)
                            {
                                state.RecordAssignment(
                                    receiver,
                                    choiceGroupInfo.GroupId,
                                    choiceGroupInfo.ArmId,
                                    property.Name,
                                    assignment.Syntax.GetLocation());
                            }
                        }
                    }
                }
                // Track local = captureRef: links a flow capture (used by object
                // initializer assignments) to the local variable (used by subsequent
                // assignments). Transfer any existing state from the capture receiver
                // to the local receiver so they're unified.
                else if (assignment.Target is ILocalReferenceOperation or IParameterReferenceOperation)
                {
                    var localKey = GetReceiverKey(assignment.Target, captureToReceiver);
                    var valueKey = GetReceiverKeyDirect(assignment.Value);
                    if (localKey != null && valueKey != null)
                    {
                        // Map the capture to this local for future lookups.
                        if (valueKey.Kind == ReceiverKind.Capture && valueKey.CaptureId.HasValue)
                        {
                            captureToReceiver[valueKey.CaptureId.Value] = localKey;
                        }
                        // Transfer any state already recorded under the capture key.
                        state.TransferState(valueKey, localKey);
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Extracts a <see cref="ReceiverKey"/> that identifies the object instance being
    /// assigned to, so we can group assignments by receiver. Resolves flow captures
    /// through <paramref name="captureToReceiver"/> when possible.
    /// </summary>
    private static ReceiverKey? GetReceiverKey(
        IOperation? instance,
        Dictionary<CaptureId, ReceiverKey> captureToReceiver)
    {
        if (instance == null) return null;

        switch (instance)
        {
            case ILocalReferenceOperation localRef:
                return new ReceiverKey(ReceiverKind.Local, localRef.Local);

            case IParameterReferenceOperation paramRef:
                return new ReceiverKey(ReceiverKind.Parameter, paramRef.Parameter);

            case IInstanceReferenceOperation:
                return ReceiverKey.This;

            case IFlowCaptureReferenceOperation captureRef:
                // Try to resolve the capture to its underlying receiver.
                if (captureToReceiver.TryGetValue(captureRef.Id, out var resolved))
                    return resolved;
                // If not resolved, use the capture ID itself as a receiver key.
                return new ReceiverKey(captureRef.Id);

            case IConversionOperation conversion:
                return GetReceiverKey(conversion.Operand, captureToReceiver);

            default:
                return null;
        }
    }

    /// <summary>
    /// Like <see cref="GetReceiverKey"/> but does NOT resolve captures through the
    /// lookup table. Returns the raw capture key so we can identify the source capture
    /// when linking it to a local variable.
    /// </summary>
    private static ReceiverKey? GetReceiverKeyDirect(IOperation? instance)
    {
        if (instance == null) return null;

        switch (instance)
        {
            case ILocalReferenceOperation localRef:
                return new ReceiverKey(ReceiverKind.Local, localRef.Local);

            case IParameterReferenceOperation paramRef:
                return new ReceiverKey(ReceiverKind.Parameter, paramRef.Parameter);

            case IInstanceReferenceOperation:
                return ReceiverKey.This;

            case IFlowCaptureReferenceOperation captureRef:
                return new ReceiverKey(captureRef.Id);

            case IConversionOperation conversion:
                return GetReceiverKeyDirect(conversion.Operand);

            default:
                return null;
        }
    }

    private static void ReportConflicts(
        OperationBlockAnalysisContext context,
        FlowState state,
        HashSet<string> reported)
    {
        foreach (var receiverEntry in state.Receivers)
        {
            foreach (var groupEntry in receiverEntry.Value)
            {
                var groupId = groupEntry.Key;
                var arms = groupEntry.Value;

                if (arms.Count < 2) continue;

                var armList = arms.ToList();
                var first = armList[0];

                for (var i = 1; i < armList.Count; i++)
                {
                    var conflicting = armList[i];

                    // Build a dedup key from the group ID and the two locations.
                    var loc1 = first.Value.Location.GetLineSpan().ToString();
                    var loc2 = conflicting.Value.Location.GetLineSpan().ToString();
                    var dedupKey = string.Compare(loc1, loc2, StringComparison.Ordinal) < 0
                        ? $"{groupId}|{loc1}|{loc2}"
                        : $"{groupId}|{loc2}|{loc1}";

                    if (!reported.Add(dedupKey)) continue;

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
    }

    private static IEnumerable<ChoiceGroupInfo> GetChoiceGroupInfos(IPropertySymbol property)
    {
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.Name != AttributeShortName)
                continue;

            var args = attr.ConstructorArguments;
            if (args.Length == 2 &&
                args[0].Value is int g &&
                args[1].Value is int a)
            {
                yield return new ChoiceGroupInfo(g, a, IsRequiredChoiceGroupAttribute(attr));
            }
        }
    }

    private static HashSet<int> GetRequiredChoiceGroupIds(ITypeSymbol type)
    {
        var groupIds = new HashSet<int>();

        foreach (var property in GetPropertiesIncludingBaseTypes(type))
        {
            foreach (var choiceGroupInfo in GetChoiceGroupInfos(property))
            {
                if (choiceGroupInfo.IsRequired)
                {
                    groupIds.Add(choiceGroupInfo.GroupId);
                }
            }
        }

        return groupIds;
    }

    private static IEnumerable<IPropertySymbol> GetPropertiesIncludingBaseTypes(ITypeSymbol type)
    {
        for (var current = type; current != null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                yield return property;
            }
        }
    }

    private static HashSet<int> GetAssignedChoiceGroupIds(IObjectOrCollectionInitializerOperation initializer)
    {
        var groupIds = new HashSet<int>();

        foreach (var operation in initializer.Initializers)
        {
            if (operation is ISimpleAssignmentOperation assignment &&
                assignment.Target is IPropertyReferenceOperation propertyReference)
            {
                foreach (var choiceGroupInfo in GetChoiceGroupInfos(propertyReference.Property))
                {
                    groupIds.Add(choiceGroupInfo.GroupId);
                }
            }
        }

        return groupIds;
    }

    private static bool IsRequiredChoiceGroupAttribute(AttributeData attribute) =>
        attribute.NamedArguments.Any(argument =>
            argument.Key == "IsRequired" &&
            argument.Value.Value is bool isRequired &&
            isRequired);

    // -------------------------------------------------------------------------
    // Supporting types
    // -------------------------------------------------------------------------

    private enum ReceiverKind
    {
        Local,
        Parameter,
        This,
        Capture
    }

    private readonly struct ChoiceGroupInfo
    {
        public ChoiceGroupInfo(int groupId, int armId, bool isRequired)
        {
            GroupId = groupId;
            ArmId = armId;
            IsRequired = isRequired;
        }

        public int GroupId { get; }
        public int ArmId { get; }
        public bool IsRequired { get; }
    }

    /// <summary>
    /// Identifies the receiver of a property assignment so we can distinguish
    /// <c>x.PropA</c> from <c>y.PropA</c>.
    /// </summary>
    private sealed class ReceiverKey : IEquatable<ReceiverKey>
    {
        public ReceiverKind Kind { get; }
        public ISymbol? Symbol { get; }
        public CaptureId? CaptureId { get; }

        public static readonly ReceiverKey This = new ReceiverKey(ReceiverKind.This, null);

        public ReceiverKey(ReceiverKind kind, ISymbol? symbol)
        {
            Kind = kind;
            Symbol = symbol;
        }

        public ReceiverKey(CaptureId captureId)
        {
            Kind = ReceiverKind.Capture;
            CaptureId = captureId;
        }

        public bool Equals(ReceiverKey? other)
        {
            if (other == null) return false;
            if (Kind != other.Kind) return false;
            if (Kind == ReceiverKind.Capture)
                return CaptureId.Equals(other.CaptureId);
            if (Symbol != null && other.Symbol != null)
                return SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol);
            return Symbol == null && other.Symbol == null;
        }

        public override bool Equals(object obj) => Equals(obj as ReceiverKey);

        public override int GetHashCode()
        {
            if (Kind == ReceiverKind.Capture)
                return CaptureId.GetHashCode();
            return (Kind, Symbol != null ? SymbolEqualityComparer.Default.GetHashCode(Symbol) : 0).GetHashCode();
        }
    }

    /// <summary>
    /// Arm assignment entry: property name + source location for diagnostics.
    /// </summary>
    private struct ArmEntry : IEquatable<ArmEntry>
    {
        public string PropertyName;
        public Location Location;

        public bool Equals(ArmEntry other) =>
            PropertyName == other.PropertyName &&
            Location == other.Location;

        public override bool Equals(object obj) => obj is ArmEntry e && Equals(e);
        public override int GetHashCode() => (PropertyName, Location).GetHashCode();
    }

    /// <summary>
    /// Dataflow state: for each receiver, for each choice group, which arms
    /// have been assigned (armId → first assignment info).
    /// </summary>
    private sealed class FlowState : IEquatable<FlowState>
    {
        /// <summary>
        /// Receiver → GroupId → ArmId → ArmEntry
        /// </summary>
        public Dictionary<ReceiverKey, Dictionary<int, Dictionary<int, ArmEntry>>> Receivers { get; }

        public static FlowState Empty => new FlowState();

        private FlowState()
        {
            Receivers = new Dictionary<ReceiverKey, Dictionary<int, Dictionary<int, ArmEntry>>>();
        }

        private FlowState(Dictionary<ReceiverKey, Dictionary<int, Dictionary<int, ArmEntry>>> data)
        {
            Receivers = data;
        }

        public void RecordAssignment(ReceiverKey receiver, int groupId, int armId, string propertyName, Location location)
        {
            if (!Receivers.TryGetValue(receiver, out var groups))
            {
                groups = new Dictionary<int, Dictionary<int, ArmEntry>>();
                Receivers[receiver] = groups;
            }

            if (!groups.TryGetValue(groupId, out var arms))
            {
                arms = new Dictionary<int, ArmEntry>();
                groups[groupId] = arms;
            }

            // Only record the first assignment to a given arm (keeps the earliest location).
            if (!arms.ContainsKey(armId))
            {
                arms[armId] = new ArmEntry { PropertyName = propertyName, Location = location };
            }
        }

        /// <summary>
        /// Transfers all recorded state from <paramref name="source"/> to <paramref name="target"/>.
        /// Used when a flow capture (from an object initializer) is assigned to a local variable,
        /// linking the two receiver identities.
        /// </summary>
        public void TransferState(ReceiverKey source, ReceiverKey target)
        {
            if (!Receivers.TryGetValue(source, out var sourceGroups))
                return;

            if (!Receivers.TryGetValue(target, out var targetGroups))
            {
                targetGroups = new Dictionary<int, Dictionary<int, ArmEntry>>();
                Receivers[target] = targetGroups;
            }

            foreach (var groupKvp in sourceGroups)
            {
                if (!targetGroups.TryGetValue(groupKvp.Key, out var targetArms))
                {
                    targetArms = new Dictionary<int, ArmEntry>();
                    targetGroups[groupKvp.Key] = targetArms;
                }

                foreach (var armKvp in groupKvp.Value)
                {
                    if (!targetArms.ContainsKey(armKvp.Key))
                    {
                        targetArms[armKvp.Key] = armKvp.Value;
                    }
                }
            }

            // Remove the source entry since it's now merged into the target.
            Receivers.Remove(source);
        }

        public FlowState Clone()
        {
            var data = new Dictionary<ReceiverKey, Dictionary<int, Dictionary<int, ArmEntry>>>();
            foreach (var receiverKvp in Receivers)
            {
                var groups = new Dictionary<int, Dictionary<int, ArmEntry>>();
                foreach (var groupKvp in receiverKvp.Value)
                {
                    groups[groupKvp.Key] = new Dictionary<int, ArmEntry>(groupKvp.Value);
                }
                data[receiverKvp.Key] = groups;
            }
            return new FlowState(data);
        }

        /// <summary>
        /// May-analysis merge: union of all arm assignments across both states.
        /// If EITHER path records an arm, the merged state includes it.
        /// </summary>
        public FlowState MergeWith(FlowState other)
        {
            var merged = Clone();

            foreach (var receiverKvp in other.Receivers)
            {
                if (!merged.Receivers.TryGetValue(receiverKvp.Key, out var mergedGroups))
                {
                    mergedGroups = new Dictionary<int, Dictionary<int, ArmEntry>>();
                    merged.Receivers[receiverKvp.Key] = mergedGroups;
                }

                foreach (var groupKvp in receiverKvp.Value)
                {
                    if (!mergedGroups.TryGetValue(groupKvp.Key, out var mergedArms))
                    {
                        mergedArms = new Dictionary<int, ArmEntry>();
                        mergedGroups[groupKvp.Key] = mergedArms;
                    }

                    foreach (var armKvp in groupKvp.Value)
                    {
                        if (!mergedArms.ContainsKey(armKvp.Key))
                        {
                            mergedArms[armKvp.Key] = armKvp.Value;
                        }
                    }
                }
            }

            return merged;
        }

        public bool Equals(FlowState? other)
        {
            if (other == null) return false;
            if (Receivers.Count != other.Receivers.Count) return false;

            foreach (var receiverKvp in Receivers)
            {
                if (!other.Receivers.TryGetValue(receiverKvp.Key, out var otherGroups))
                    return false;
                if (receiverKvp.Value.Count != otherGroups.Count) return false;

                foreach (var groupKvp in receiverKvp.Value)
                {
                    if (!otherGroups.TryGetValue(groupKvp.Key, out var otherArms))
                        return false;
                    if (groupKvp.Value.Count != otherArms.Count) return false;

                    foreach (var armKvp in groupKvp.Value)
                    {
                        if (!otherArms.TryGetValue(armKvp.Key, out var otherEntry))
                            return false;
                        if (!armKvp.Value.Equals(otherEntry))
                            return false;
                    }
                }
            }

            return true;
        }

        public override bool Equals(object obj) => Equals(obj as FlowState);
        public override int GetHashCode() => Receivers.Count;
    }
}
