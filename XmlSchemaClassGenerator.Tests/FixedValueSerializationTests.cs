using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class FixedValueSerializationTests
{
    private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="Root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="RequiredFlag" type="xs:boolean" fixed="true" />
                <xs:element name="OptionalFlag" type="xs:boolean" fixed="true" minOccurs="0" nillable="true" />
              </xs:sequence>
              <xs:attribute name="version" type="xs:string" use="required" fixed="2.1" />
              <xs:attribute name="optionalVersion" type="xs:string" fixed="2.1" />
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    [Fact]
    public void Should_serialize_required_fixed_elements_and_attributes()
    {
        // arrange
        var type = Generate();
        var value = Activator.CreateInstance(type);

        // act
        var xml = Serialize(value);

        // assert
        Assert.Equal("true", xml.Element("RequiredFlag")?.Value);
        Assert.Equal("2.1", xml.Attribute("version")?.Value);
    }

    [Fact]
    public void Should_preserve_presence_of_optional_fixed_elements()
    {
        // arrange
        var type = Generate();
        var value = Activator.CreateInstance(type);
        var property = type.GetProperty("OptionalFlag");

        // act
        var untouched = Serialize(value);
        type.GetMethod("IncludeOptionalFlag").Invoke(value, null);
        var assigned = Serialize(value);

        // assert
        Assert.Null(untouched.Element("OptionalFlag"));
        Assert.Equal("true", assigned.Element("OptionalFlag")?.Value);
    }

    [Theory]
    [InlineData("RequiredFlag", false)]
    [InlineData("OptionalFlag", false)]
    [InlineData("Version", "wrong")]
    public void Should_reject_values_that_differ_from_fixed_constraint(string name, object invalid)
    {
        // arrange
        var type = Generate();
        var value = Activator.CreateInstance(type);
        var property = type.GetProperty(name + "Xml");

        // act
        var exception = Record.Exception(() => property.SetValue(value, invalid));

        // assert
        Assert.IsType<ArgumentOutOfRangeException>(Assert.IsType<TargetInvocationException>(exception).InnerException);
    }

    [Theory]
    [InlineData("<Root version=\"2.1\"><RequiredFlag>false</RequiredFlag></Root>")]
    [InlineData("<Root version=\"wrong\"><RequiredFlag>true</RequiredFlag></Root>")]
    public void Should_reject_invalid_fixed_values_during_deserialization(string xml)
    {
        // arrange
        var serializer = new XmlSerializer(Generate());
        using var reader = new StringReader(xml);

        // act
        var exception = Record.Exception(() => serializer.Deserialize(reader));

        // assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Should_keep_fixed_properties_readonly_and_hide_serialization_proxies()
    {
        // arrange
        var type = Generate();

        // act
        var properties = new[] { "RequiredFlag", "OptionalFlag", "Version", "OptionalVersion" }.Select(type.GetProperty).ToArray();

        // assert
        Assert.All(properties, property => Assert.Null(property.SetMethod));
        Assert.All(properties, property => Assert.Single(property.GetCustomAttributes(typeof(XmlIgnoreAttribute), false)));
        Assert.All(properties, property => Assert.Equal(EditorBrowsableState.Never,
            ((EditorBrowsableAttribute)type.GetProperty(property.Name + "Xml").GetCustomAttributes(typeof(EditorBrowsableAttribute), false).Single()).State));
        Assert.DoesNotContain(type.GetProperties(), property => property.Name.EndsWith("Specified", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("<Root version=\"2.1\"><RequiredFlag>true</RequiredFlag></Root>", false)]
    [InlineData("<Root version=\"2.1\"><RequiredFlag>true</RequiredFlag><OptionalFlag>true</OptionalFlag></Root>", true)]
    public void Should_round_trip_optional_fixed_presence(string xml, bool present)
    {
        // arrange
        var serializer = new XmlSerializer(Generate());
        using var reader = new StringReader(xml);

        // act
        var result = Serialize(serializer.Deserialize(reader));

        // assert
        Assert.Equal(present, result.Element("OptionalFlag") != null);
    }

    [Fact]
    public void Should_reject_nil_for_fixed_nillable_elements()
    {
        // arrange
        var serializer = new XmlSerializer(Generate());
        using var reader = new StringReader("<Root xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" version=\"2.1\"><RequiredFlag>true</RequiredFlag><OptionalFlag xsi:nil=\"true\" /></Root>");

        // act
        var exception = Record.Exception(() => serializer.Deserialize(reader));

        // assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void Should_reject_assignment_to_main_property_at_compile_time()
    {
        // arrange
        var source = GenerateSource();
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create("ReadOnlyFixed", source.Select(text => CSharpSyntaxTree.ParseText(text))
            .Append(CSharpSyntaxTree.ParseText("public class Consumer { public void Set(FixedValues.Root value) { value.RequiredFlag = false; } }")),
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // act
        var diagnostics = compilation.GetDiagnostics();

        // assert
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "CS0200");
    }

    [Fact]
    public void Should_preserve_presence_of_optional_fixed_attributes()
    {
        // arrange
        var type = Generate();
        var value = Activator.CreateInstance(type);

        // act
        var untouched = Serialize(value);
        type.GetMethod("IncludeOptionalVersion").Invoke(value, null);
        var included = Serialize(value);
        using var reader = new StringReader("<Root version=\"2.1\" optionalVersion=\"2.1\"><RequiredFlag>true</RequiredFlag></Root>");
        var roundTrip = Serialize(new XmlSerializer(type).Deserialize(reader));

        // assert
        Assert.Null(untouched.Attribute("optionalVersion"));
        Assert.Equal("2.1", included.Attribute("optionalVersion")?.Value);
        Assert.Equal("2.1", roundTrip.Attribute("optionalVersion")?.Value);
    }

    [Fact]
    public void Should_avoid_proxy_and_include_method_name_collisions()
    {
        // arrange
        var schema = Schema.Replace("<xs:sequence>", "<xs:sequence><xs:element name=\"RequiredFlagXml\" type=\"xs:string\" minOccurs=\"0\"/><xs:element name=\"IncludeOptionalFlag\" type=\"xs:string\" minOccurs=\"0\"/>");
        var type = Compiler.Compile(Guid.NewGuid().ToString("N"), GenerateSource(schema)).GetType("FixedValues.Root");
        var value = Activator.CreateInstance(type);

        // act
        type.GetMethod("IncludeOptionalFlag_").Invoke(value, null);
        var xml = Serialize(value);

        // assert
        Assert.NotNull(type.GetProperty("RequiredFlagXml_"));
        Assert.NotNull(type.GetProperty("IncludeOptionalFlag"));
        Assert.Equal("true", xml.Element("OptionalFlag")?.Value);
    }

    [Fact]
    public void Should_keep_fixed_group_interface_getter_only()
    {
        // arrange
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:group name="Flags"><xs:sequence>
                <xs:element name="OptionalFlag" type="xs:boolean" fixed="true" minOccurs="0" />
              </xs:sequence></xs:group>
              <xs:element name="Root"><xs:complexType><xs:group ref="Flags" /></xs:complexType></xs:element>
            </xs:schema>
            """;

        // act
        var assembly = Compiler.Compile(Guid.NewGuid().ToString("N"), GenerateSource(schema));
        var property = Assert.Single(assembly.GetTypes(), type => type.IsInterface).GetProperty("OptionalFlag");

        // assert
        Assert.Null(property.SetMethod);
        Assert.Equal(typeof(bool), property.PropertyType);
    }

    [Fact]
    public void Should_compare_fixed_binary_values_by_contents()
    {
        // arrange
        var schema = Schema.Replace("type=\"xs:boolean\" fixed=\"true\"", "type=\"xs:hexBinary\" fixed=\"0A0B\"");
        var type = Compiler.Compile(Guid.NewGuid().ToString("N"), GenerateSource(schema)).GetType("FixedValues.Root");
        var serializer = new XmlSerializer(type);
        using var reader = new StringReader("<Root version=\"2.1\"><RequiredFlag>0A0B</RequiredFlag></Root>");

        // act
        var value = serializer.Deserialize(reader);

        // assert
        Assert.Equal(new byte[] { 10, 11 }, (byte[])type.GetProperty("RequiredFlag").GetValue(value));
    }

    private static Type Generate() => Compiler.Compile(Guid.NewGuid().ToString("N"), GenerateSource()).GetType("FixedValues.Root");

    private static string[] GenerateSource(string schema = Schema)
    {
        var writer = new MemoryOutputWriter();
        var generator = new Generator
        {
            OutputWriter = writer,
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "FixedValues" },
            GenerateStrictFixedValues = true,
            GenerateInterfaces = true,
            GenerateNullables = true,
            UseShouldSerializePattern = true,
        };
        generator.Generate(new[] { new StringReader(schema) });
        return writer.Content.ToArray();
    }

    private static XElement Serialize(object value)
    {
        var serializer = new XmlSerializer(value.GetType());
        using var writer = new StringWriter();
        serializer.Serialize(writer, value);
        return XElement.Parse(writer.ToString());
    }
}
