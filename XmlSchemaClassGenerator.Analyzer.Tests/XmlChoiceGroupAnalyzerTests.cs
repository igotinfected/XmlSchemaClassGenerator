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
    private const string AttributeSource = @"
namespace XmlSchemaClassGenerator.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed class XmlChoiceGroupAttribute : System.Attribute
    {
        public int GroupId { get; }
        public int ArmId { get; }
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

    // -- Object initializer tests -------------------------------------------------

    [Fact]
    public async Task ObjectInitializer_ConflictingArms_Reports()
    {
        var source = @"
using XmlSchemaClassGenerator.Attributes;

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
using XmlSchemaClassGenerator.Attributes;

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
using XmlSchemaClassGenerator.Attributes;

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
using XmlSchemaClassGenerator.Attributes;

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

    // -- Straight-line assignment tests -------------------------------------------

    [Fact]
    public async Task BlockAssignment_ConflictingArms_Reports()
    {
        var source = @"
using XmlSchemaClassGenerator.Attributes;

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
using XmlSchemaClassGenerator.Attributes;

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
    public async Task BlockAssignment_BrokenByNonAssignment_NoDiagnostic()
    {
        var source = @"
using XmlSchemaClassGenerator.Attributes;

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
        x.PropA = ""a"";
        System.Console.WriteLine(""break"");
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
using XmlSchemaClassGenerator.Attributes;

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
    public async Task BlockAssignment_SequenceArm_ConflictWithOtherArm_Reports()
    {
        // Simulates LocationStructure: Longitude(0) + Latitude(0) are same arm,
        // Coordinates(1) is a different arm.
        var source = @"
using XmlSchemaClassGenerator.Attributes;

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

    [Fact]
    public async Task BlockAssignment_MultipleGroups_OnlyConflictingReports()
    {
        // Two independent choice groups on same type; only group 1 has a conflict.
        var source = @"
using XmlSchemaClassGenerator.Attributes;

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
}
