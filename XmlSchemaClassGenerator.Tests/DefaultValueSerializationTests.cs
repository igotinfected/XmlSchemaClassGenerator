using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class DefaultValueSerializationTests
{
    private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="Root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="Enabled" type="xs:boolean" default="true" minOccurs="0" />
                <xs:element name="Mode" default="active" minOccurs="0">
                  <xs:simpleType><xs:restriction base="xs:string"><xs:enumeration value="active"/><xs:enumeration value="idle"/></xs:restriction></xs:simpleType>
                </xs:element>
                <xs:element name="Label" type="xs:string" default="ready" minOccurs="0" nillable="true" />
                <xs:element name="NullableCount" type="xs:int" default="4" minOccurs="0" nillable="true" />
                <xs:element name="RequiredFlag" type="xs:boolean" default="true" />
              </xs:sequence>
              <xs:attribute name="code" type="xs:string" default="standard" />
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_omit_untouched_defaults_and_serialize_explicit_defaults(bool dataBinding)
    {
        // arrange
        var type = Generate(dataBinding);
        var value = Activator.CreateInstance(type);

        // act
        var untouched = Serialize(value);
        foreach (var name in new[] { "Enabled", "Mode", "Label", "NullableCount", "Code" })
        {
            var property = type.GetProperty(name);
            property.SetValue(value, property.GetValue(value));
        }
        var assigned = Serialize(value);

        // assert
        Assert.True((bool)type.GetProperty("Enabled").GetValue(value));
        Assert.Equal("ready", type.GetProperty("Label").GetValue(value));
        Assert.Equal("standard", type.GetProperty("Code").GetValue(value));
        Assert.Equal(new[] { "RequiredFlag" }, untouched.Elements().Select(element => element.Name.LocalName));
        Assert.Null(untouched.Attribute("code"));
        Assert.Equal("true", assigned.Element("Enabled").Value);
        Assert.Equal("active", assigned.Element("Mode").Value);
        Assert.Equal("ready", assigned.Element("Label").Value);
        Assert.Equal("4", assigned.Element("NullableCount").Value);
        Assert.Equal("standard", assigned.Attribute("code").Value);
        Assert.DoesNotContain(type.GetProperties(), property => property.Name.EndsWith("Specified", StringComparison.Ordinal));
        Assert.All(type.GetProperties(), property => Assert.Empty(property.GetCustomAttributes(typeof(DefaultValueAttribute), false)));
    }

    [Fact]
    public void Should_serialize_nondefault_values_and_explicit_nil()
    {
        // arrange
        var type = Generate(false);
        var value = Activator.CreateInstance(type);

        // act
        type.GetProperty("Enabled").SetValue(value, false);
        type.GetProperty("Label").SetValue(value, "changed");
        type.GetProperty("Code").SetValue(value, "custom");
        type.GetProperty("NullableCount").SetValue(value, null);
        var xml = Serialize(value);

        // assert
        Assert.Equal("false", xml.Element("Enabled").Value);
        Assert.Equal("changed", xml.Element("Label").Value);
        Assert.Equal("custom", xml.Attribute("code").Value);
        Assert.Equal("true", xml.Element("NullableCount").Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance")).Value);
    }

    [Theory]
    [InlineData("<Root><RequiredFlag>true</RequiredFlag></Root>")]
    [InlineData("<Root code=\"standard\"><Enabled>true</Enabled><Mode>active</Mode><Label>ready</Label><NullableCount>4</NullableCount><RequiredFlag>true</RequiredFlag></Root>")]
    [InlineData("<Root xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><NullableCount xsi:nil=\"true\"/><RequiredFlag>true</RequiredFlag></Root>")]
    public void Should_preserve_presence_when_deserializing(string xml)
    {
        // arrange
        var type = Generate(false);
        var serializer = new XmlSerializer(type);
        using var reader = new StringReader(xml);

        // act
        var value = serializer.Deserialize(reader);
        var result = Serialize(value);

        // assert
        var expected = XElement.Parse(xml);
        Assert.Equal(expected.Elements().Select(element => element.Name), result.Elements().Select(element => element.Name));
        Assert.Equal(expected.Attribute("code")?.Value, result.Attribute("code")?.Value);
        Assert.Equal(expected.Element("NullableCount")?.Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance"))?.Value,
            result.Element("NullableCount")?.Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance"))?.Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_preserve_explicit_nil_for_reference_defaults(bool nullableDirective)
    {
        // arrange
        var type = Generate(false, nullableDirective: nullableDirective);
        var value = Activator.CreateInstance(type);

        // act
        type.GetProperty("Label").SetValue(value, null);
        var xml = Serialize(value);

        // assert
        Assert.Equal("true", xml.Element("Label").Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance")).Value);
        Assert.Null(xml.Element("Enabled"));
    }

    [Fact]
    public void Should_keep_legacy_default_attribute_when_tracking_is_disabled()
    {
        // arrange
        var type = Generate(false, trackDefaults: false);
        var value = Activator.CreateInstance(type);

        // act
        type.GetProperty("Enabled").SetValue(value, true);
        var xml = Serialize(value);

        // assert
        Assert.Null(xml.Element("Enabled"));
        Assert.Single(type.GetProperty("Enabled").GetCustomAttributes(typeof(DefaultValueAttribute), false));
        Assert.Null(type.GetMethod("ShouldSerializeEnabled"));
    }

    [Theory]
    [InlineData("Value", "changed")]
    [InlineData("Lang", "fr")]
    public void Should_serialize_mutations_to_default_simple_content_objects(string propertyName, string value)
    {
        // arrange
        var type = Generate(schemaOverride: ObjectDefaultSchema);
        var root = Activator.CreateInstance(type);
        var note = type.GetProperty("Note").GetValue(root);

        // act
        note.GetType().GetProperty(propertyName).SetValue(note, value);
        var xml = Serialize(root);

        // assert
        Assert.NotNull(xml.Element("Note"));
        Assert.Equal(propertyName == "Value" ? "changed" : "ready", xml.Element("Note").Value);
        Assert.Equal(propertyName == "Lang" ? "fr" : null, xml.Element("Note").Attribute("lang")?.Value);
    }

    [Fact]
    public void Should_preserve_existing_serialization_of_untouched_object_defaults()
    {
        // arrange
        var type = Generate(schemaOverride: ObjectDefaultSchema);
        var root = Activator.CreateInstance(type);

        // act
        var xml = Serialize(root);

        // assert
        Assert.Equal("ready", xml.Element("Note")?.Value);
        Assert.Null(type.GetMethod("ShouldSerializeNote"));
    }

    [Fact]
    public void Should_serialize_mutations_to_default_byte_arrays()
    {
        // arrange
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="Root"><xs:complexType><xs:sequence>
                <xs:element name="Bytes" type="xs:hexBinary" default="0A0B" minOccurs="0" />
              </xs:sequence></xs:complexType></xs:element>
            </xs:schema>
            """;
        var type = Generate(schemaOverride: schema);
        var root = Activator.CreateInstance(type);
        var bytes = (byte[])type.GetProperty("Bytes").GetValue(root);

        // act
        bytes[0] = 12;
        var xml = Serialize(root);

        // assert
        Assert.Equal("0C0B", xml.Element("Bytes")?.Value);
        Assert.Null(type.GetMethod("ShouldSerializeBytes"));
    }

    private const string ObjectDefaultSchema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:complexType name="Text"><xs:simpleContent><xs:extension base="xs:string">
            <xs:attribute name="lang" type="xs:string" />
          </xs:extension></xs:simpleContent></xs:complexType>
          <xs:element name="Root"><xs:complexType><xs:sequence>
            <xs:element name="Note" type="Text" default="ready" minOccurs="0" />
          </xs:sequence></xs:complexType></xs:element>
        </xs:schema>
        """;

    private static Type Generate(bool dataBinding = false, bool trackDefaults = true, bool nullableDirective = false, string schemaOverride = null)
    {
        var writer = new MemoryOutputWriter();
        var generator = new Generator
        {
            OutputWriter = writer,
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "Defaults" },
            GenerateNullables = true,
            UseShouldSerializePattern = true,
            EnableDataBinding = dataBinding,
            UseShouldSerializeForDefaultValues = trackDefaults,
            EnableNullableDirective = nullableDirective,
        };
        generator.Generate(new[] { new StringReader(schemaOverride ?? Schema) });
        // the file output writer adds the nullable directive, the memory writer does not
        var contents = writer.Content.Select(content => nullableDirective ? "#nullable enable\n" + content : content).ToArray();
        return Compiler.Compile("Defaults" + Guid.NewGuid().ToString("N"), contents).GetType("Defaults.Root");
    }

    private static XElement Serialize(object value)
    {
        using var writer = new StringWriter();
        new XmlSerializer(value.GetType()).Serialize(writer, value);
        return XElement.Parse(writer.ToString());
    }
}
