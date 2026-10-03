using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    XmlSchemaClassGenerator.Analyzer.XmlChoiceGroupAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace XmlSchemaClassGenerator.Analyzer.Tests;

public class XmlChoiceGroupAnalyzerTests
{
    // The attribute source is provided as a separate file so that the using directive
    // in the test source doesn't conflict with the namespace declaration order.
    // The namespace is intentionally different from the original to verify that the
    // analyzer matches by short name, not by fully-qualified name.
    private const string AttributeSource = @"
namespace TestModels
{
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public sealed class XmlChoiceGroupAttribute : System.Attribute
    {
        public int GroupId { get; }
        public int ArmId { get; }
        public bool IsRequired { get; set; }
        public XmlChoiceGroupAttribute(int groupId, int armId)
        {
            GroupId = groupId;
            ArmId = armId;
        }
    }
}
";

    private static CSharpAnalyzerTest<XmlChoiceGroupAnalyzer, DefaultVerifier> CreateTest(
        string testSource,
        params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<XmlChoiceGroupAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources = { testSource },
            },
        };
        test.TestState.Sources.Add(("Attributes.cs", AttributeSource));
        test.ExpectedDiagnostics.AddRange(expected);
        return test;
    }

    // =========================================================================
    // Object initializer tests
    // =========================================================================

    [Fact]
    public async Task ObjectInitializer_MissingRequiredChoiceGroup_Reports()
    {
        // arrange
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? PropB { get; set; }

    public string? Name { get; set; }
}

public class Program
{
    public void M()
    {
        var x = {|#0:new MyType
        {
            Name = ""value"",
        }|};
    }
}
";

        // act
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.MissingRequiredChoiceGroupDiagnosticId)
                .WithLocation(0)
                .WithArguments("MyType", 1, "'PropA', 'PropB'"));

        // assert
        await test.RunAsync();
    }

    [Theory]
    [InlineData("Items = { \"item\" }", false)]
    [InlineData("Items = { }", true)]
    [InlineData("Child = { Value = \"item\" }", false)]
    public async Task ObjectInitializer_MemberInitializerSelectsRequiredArmOnlyWhenPopulated(string initializer, bool expectsMissingChoice)
    {
        // arrange
        var source = @"
using TestModels;
using System.Collections.Generic;
public class ChildType
{
    public string? Value { get; set; }
}
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public List<string> Items { get; } = new List<string>();
    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public ChildType Child { get; set; } = new ChildType();
}
public class Program
{
    public void M() { var x = {|#0:new MyType { " + initializer + @" }|}; }
}";
        var test = expectsMissingChoice
            ? CreateTest(source, Verify.Diagnostic(XmlChoiceGroupAnalyzer.MissingRequiredChoiceGroupDiagnosticId)
                .WithLocation(0).WithArguments("MyType", 1, "'Items', 'Child'"))
            : CreateTest(source);

        // act
        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_RequiredChoiceGroupWithOneArm_NoDiagnostic()
    {
        // arrange
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            PropA = ""a"",
        };
    }
}
";

        // act
        var test = CreateTest(source);

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_DerivedTypeMissingInheritedRequiredChoiceGroup_Reports()
    {
        // arrange
        var source = @"
using TestModels;

public class BaseType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? BaseA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? BaseB { get; set; }
}

public class DerivedType : BaseType
{
    public string? Name { get; set; }
}

public class Program
{
    public void M()
    {
        var x = {|#0:new DerivedType
        {
            Name = ""value"",
        }|};
    }
}
";

        // act
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.MissingRequiredChoiceGroupDiagnosticId)
                .WithLocation(0)
                .WithArguments("DerivedType", 1, "'BaseA', 'BaseB'"));

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_DerivedTypeWithInheritedRequiredChoiceGroupAssigned_NoDiagnostic()
    {
        // arrange
        var source = @"
using TestModels;

public class BaseType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? BaseA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? BaseB { get; set; }
}

public class DerivedType : BaseType
{
    public string? Name { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new DerivedType
        {
            BaseA = ""a"",
        };
    }
}
";

        // act
        var test = CreateTest(source);

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_NestedInitializerWithCollidingGroupIdDoesNotSatisfyParent_Reports()
    {
        // arrange
        var source = @"
using TestModels;

public class ParentType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? ParentA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? ParentB { get; set; }

    public ChildType? Child { get; set; }
}

public class ChildType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? ChildA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? ChildB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = {|#0:new ParentType
        {
            Child = new ChildType
            {
                ChildA = ""a"",
            },
        }|};
    }
}
";

        // act
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.MissingRequiredChoiceGroupDiagnosticId)
                .WithLocation(0)
                .WithArguments("ParentType", 1, "'ParentA', 'ParentB'"));

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_OptionalChoiceGroupOmitted_NoDiagnostic()
    {
        // arrange
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
        };
    }
}
";

        // act
        var test = CreateTest(source);

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_PropertyWithMultipleRequiredMemberships_SatisfiesAllGroups()
    {
        // arrange
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    [XmlChoiceGroupAttribute(2, 0, IsRequired = true)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? PropB { get; set; }

    [XmlChoiceGroupAttribute(2, 1, IsRequired = true)]
    public string? PropC { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            PropA = ""a"",
        };
    }
}
";

        // act
        var test = CreateTest(source);

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_TargetTypedNewMissingRequiredChoiceGroup_Reports()
    {
        // arrange
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        MyType x = {|#0:new()
        {
        }|};
    }
}
";

        // act
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.MissingRequiredChoiceGroupDiagnosticId)
                .WithLocation(0)
                .WithArguments("MyType", 1, "'PropA', 'PropB'"));

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_ConflictingArms_Reports()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            {|#1:PropA = ""a""|},
            {|#0:PropB = ""b""|},
        };
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_SameArm_NoDiagnostic()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            PropA = ""a"",
            PropB = ""b"",
        };
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_DifferentGroups_NoDiagnostic()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(2, 0)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            PropA = ""a"",
            PropB = ""b"",
        };
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_MultipleMemberships_ConflictsAcrossAllGroups()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    [XmlChoiceGroupAttribute(2, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(2, 1)]
    public string? PropB { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropC { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            {|#2:PropA = ""a""|},
            {|#0:PropB = ""b""|},
            {|#1:PropC = ""c""|},
        };
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(2)
                .WithArguments("PropB", "PropA", 2, 1, 0),
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(1)
                .WithLocation(2)
                .WithArguments("PropC", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_SamePropertyPairConflictsInMultipleGroups_ReportsEachGroup()
    {
        // arrange
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    [XmlChoiceGroupAttribute(2, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    [XmlChoiceGroupAttribute(2, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            {|#1:PropA = ""a""|},
            {|#0:PropB = ""b""|},
        };
    }
}
";

        // act
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0),
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 2, 1, 0));

        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_NoAttribute_NoDiagnostic()
    {
        var source = @"
public class MyType
{
    public string? PropA { get; set; }
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            PropA = ""a"",
            PropB = ""b"",
        };
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_ThreeArmsConflict_ReportsTwo()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }

    [XmlChoiceGroupAttribute(1, 2)]
    public string? PropC { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            {|#2:PropA = ""a""|},
            {|#0:PropB = ""b""|},
            {|#1:PropC = ""c""|},
        };
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(2)
                .WithArguments("PropB", "PropA", 1, 1, 0),
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(1)
                .WithLocation(2)
                .WithArguments("PropC", "PropA", 1, 2, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task ObjectInitializer_SequenceArm_ConflictWithOtherArm_Reports()
    {
        // Simulates LocationStructure: Longitude(0) + Latitude(0) are same arm,
        // Coordinates(1) is a different arm.
        var source = @"
using TestModels;

public class LocationStructure
{
    [XmlChoiceGroupAttribute(5, 0)]
    public double? Longitude { get; set; }

    [XmlChoiceGroupAttribute(5, 0)]
    public double? Latitude { get; set; }

    [XmlChoiceGroupAttribute(5, 1)]
    public string? Coordinates { get; set; }
}

public class Program
{
    public void M()
    {
        var loc = new LocationStructure
        {
            {|#1:Longitude = 10.0|},
            Latitude = 59.0,
            {|#0:Coordinates = ""10.0 59.0""|},
        };
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("Coordinates", "Longitude", 5, 1, 0));
        await test.RunAsync();
    }

    // =========================================================================
    // Block-level assignment tests
    // =========================================================================

    [Fact]
    public async Task BlockAssignment_ConflictingArms_Reports()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType();
        {|#1:x.PropA = ""a""|};
        {|#0:x.PropB = ""b""|};
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task BlockAssignment_SameArm_NoDiagnostic()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType();
        x.PropA = ""a"";
        x.PropB = ""b"";
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task BlockAssignment_DifferentReceivers_NoDiagnostic()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType();
        var y = new MyType();
        x.PropA = ""a"";
        y.PropB = ""b"";
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task BlockAssignment_MultipleGroups_OnlyConflictingReports()
    {
        // Two independent choice groups on same type; only group 1 has a conflict.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }

    [XmlChoiceGroupAttribute(2, 0)]
    public string? PropC { get; set; }

    [XmlChoiceGroupAttribute(2, 0)]
    public string? PropD { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType();
        {|#1:x.PropA = ""a""|};
        {|#0:x.PropB = ""b""|};
        x.PropC = ""c"";
        x.PropD = ""d"";
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    // =========================================================================
    // CFG-aware: assignments split by arbitrary statements (improvement over old)
    // =========================================================================

    [Fact]
    public async Task BlockAssignment_SplitByArbitraryStatement_StillReports()
    {
        // The old syntax-based analyzer would NOT detect this because a non-assignment
        // statement broke the tracking. The CFG-based analyzer tracks across all
        // statements in the same method.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType();
        {|#1:x.PropA = ""a""|};
        System.Console.WriteLine(""arbitrary statement"");
        var y = 42;
        System.Console.WriteLine(y);
        {|#0:x.PropB = ""b""|};
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    // =========================================================================
    // CFG-aware: object initializer + subsequent assignment (mixed)
    // =========================================================================

    [Fact]
    public async Task Mixed_InitializerThenAssignment_ConflictingArms_Reports()
    {
        // Object initializer sets one arm, subsequent assignment sets another.
        // The old analyzer missed this entirely because it analyzed object initializers
        // and block assignments independently.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            {|#1:PropA = ""a""|},
        };
        {|#0:x.PropB = ""b""|};
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task Mixed_InitializerThenAssignment_SameArm_NoDiagnostic()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M()
    {
        var x = new MyType
        {
            PropA = ""a"",
        };
        x.PropB = ""b"";
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    // =========================================================================
    // CFG-aware: if/else branches
    // =========================================================================

    [Fact]
    public async Task IfElse_ConflictInBothBranches_Reports()
    {
        // Both branches unconditionally set conflicting arms on the same variable.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M(bool cond)
    {
        var x = new MyType();
        {|#1:x.PropA = ""a""|};
        if (cond)
        {
            {|#0:x.PropB = ""b""|};
        }
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task IfElse_ConflictOnlyInOneBranch_Reports()
    {
        // One branch sets a conflicting arm, the other doesn't.
        // With may-analysis (union), we flag it because the conflict is possible.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M(bool cond)
    {
        var x = new MyType();
        {|#1:x.PropA = ""a""|};
        if (cond)
        {
            {|#0:x.PropB = ""b""|};
        }
        else
        {
            // Does not set PropB — no conflict on this path.
        }
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    [Fact]
    public async Task IfElse_DifferentArmsInDifferentBranches_NoDiagnostic()
    {
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M(bool cond)
    {
        var x = new MyType();
        if (cond)
        {
            x.PropA = ""a"";
        }
        else
        {
            x.PropB = ""b"";
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task IfElse_SameArmInBothBranches_NoDiagnostic()
    {
        // Both branches set the same arm — no conflict.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M(bool cond)
    {
        var x = new MyType();
        if (cond)
        {
            x.PropA = ""a"";
        }
        else
        {
            x.PropB = ""b"";
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    // =========================================================================
    // CFG-aware: parameter receivers
    // =========================================================================

    [Fact]
    public async Task ParameterReceiver_ConflictingArms_Reports()
    {
        // The receiver is a method parameter, not a local variable.
        var source = @"
using TestModels;

public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }

    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}

public class Program
{
    public void M(MyType x)
    {
        {|#1:x.PropA = ""a""|};
        {|#0:x.PropB = ""b""|};
    }
}
";

        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0)
                .WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));
        await test.RunAsync();
    }

    // =========================================================================
    // Edge case: empty method, no assignments
    // =========================================================================

    [Theory]
    [InlineData("var x = new MyType { PropA = null, PropB = \"b\" };")]
    [InlineData("var x = new MyType { PropA = \"a\" }; x.PropA = null; x.PropB = \"b\";")]
    public async Task NullOmittedByXmlSerializerDoesNotSelectAnArm(string statements)
    {
        // arrange
        var source = @"
using TestModels;
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }
    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}
public class Program
{
    public void M() { " + statements + @" }
}";
        var test = CreateTest(source);

        // act
        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task NillableNullStillSelectsAnArm()
    {
        // arrange
        var source = @"
using TestModels;
using System.Xml.Serialization;
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    [XmlElement(IsNullable = true)]
    public string? PropA { get; set; }
    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}
public class Program
{
    public void M()
    {
        var x = new MyType { {|#1:PropA = null|}, {|#0:PropB = ""b""|} };
    }
}";
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0).WithLocation(1)
                .WithArguments("PropB", "PropA", 1, 1, 0));

        // act
        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task NullDoesNotSatisfyRequiredNonNillableChoice()
    {
        // arrange
        var source = @"
using TestModels;
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    public string? PropA { get; set; }
    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? PropB { get; set; }
}
public class Program
{
    public void M() { var x = {|#0:new MyType { PropA = null }|}; }
}";
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.MissingRequiredChoiceGroupDiagnosticId)
                .WithLocation(0).WithArguments("MyType", 1, "'PropA', 'PropB'"));

        // act
        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task NestedRequiredChoiceIsNotRequiredWhenAnotherOuterArmIsSelected()
    {
        // arrange
        var source = @"
using TestModels;
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    [XmlChoiceGroupAttribute(2, 0, IsRequired = true)]
    public string? PropA { get; set; }
    [XmlChoiceGroupAttribute(1, 0, IsRequired = true)]
    [XmlChoiceGroupAttribute(2, 1, IsRequired = true)]
    public string? PropB { get; set; }
    [XmlChoiceGroupAttribute(1, 1, IsRequired = true)]
    public string? PropC { get; set; }
}
public class Program
{
    public void M() { var x = new MyType { PropC = ""c"" }; }
}";
        var test = CreateTest(source);

        // act
        // assert
        await test.RunAsync();
    }

    [Theory]
    [InlineData("var x = new MyType { PropA = \"a\" }; x = new MyType { PropB = \"b\" };")]
    [InlineData("var x = new MyType { PropA = \"a\" }; x = new MyType(); x.PropB = \"b\";")]
    [InlineData("for (var i = 0; i < 2; i++) { var x = new MyType { PropA = \"a\" }; x.PropA = null; x.PropB = \"b\"; }")]
    public async Task ReassignedLocalDoesNotRetainPreviousObjectChoice(string statements)
    {
        // arrange
        var source = @"
using TestModels;
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }
    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}
public class Program
{
    public void M() { " + statements + @" }
}";
        var test = CreateTest(source);

        // act
        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task AssignmentAfterBranchJoinStillReportsPossibleConflict()
    {
        // arrange
        var source = @"
using TestModels;
public class MyType
{
    [XmlChoiceGroupAttribute(1, 0)]
    public string? PropA { get; set; }
    [XmlChoiceGroupAttribute(1, 1)]
    public string? PropB { get; set; }
}
public class Program
{
    public void M(bool condition)
    {
        var x = new MyType();
        if (condition) { x.PropA = ""a""; }
        else { {|#1:x.PropB = ""b""|}; }
        {|#0:x.PropA = ""later""|};
    }
}";
        var test = CreateTest(source,
            Verify.Diagnostic(XmlChoiceGroupAnalyzer.DiagnosticId)
                .WithLocation(0).WithLocation(1)
                .WithArguments("PropA", "PropB", 1, 0, 1));

        // act
        // assert
        await test.RunAsync();
    }

    [Fact]
    public async Task EmptyMethod_NoDiagnostic()
    {
        var source = @"
public class Program
{
    public void M()
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
