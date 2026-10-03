using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class UpstreamIntegrationRegressionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_preserve_concrete_substitutes_below_abstract_members(bool separateSubstitutes)
    {
        // arrange
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:complexType name="HeadType"><xs:sequence>
                <xs:element name="Value" type="xs:string" />
              </xs:sequence></xs:complexType>
              <xs:complexType name="IntermediateType"><xs:complexContent><xs:extension base="HeadType" /></xs:complexContent></xs:complexType>
              <xs:complexType name="LeafType"><xs:complexContent><xs:extension base="IntermediateType" /></xs:complexContent></xs:complexType>
              <xs:element name="Head" type="HeadType" abstract="true" />
              <xs:element name="Intermediate" type="IntermediateType" abstract="true" substitutionGroup="Head" />
              <xs:element name="Leaf" type="LeafType" substitutionGroup="Intermediate" />
              <xs:element name="Root"><xs:complexType><xs:sequence>
                <xs:element ref="Head" minOccurs="0" />
              </xs:sequence></xs:complexType></xs:element>
            </xs:schema>
            """;
        var type = Generate(schema, separateSubstitutes);
        var serializer = new XmlSerializer(type);
        using var reader = new StringReader("<Root><Leaf><Value>present</Value></Leaf></Root>");
        using var writer = new StringWriter();

        // act
        var value = serializer.Deserialize(reader);
        serializer.Serialize(writer, value);

        // assert
        Assert.Equal("present", XElement.Parse(writer.ToString()).Element("Leaf")?.Value);
        Assert.DoesNotContain(type.GetProperties().SelectMany(property => property.GetCustomAttributes(typeof(XmlElementAttribute), false).Cast<XmlElementAttribute>()), attribute => attribute.ElementName == "Intermediate");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_limit_anonymous_object_default_changes_to_untracked_mode(bool trackScalarPresence)
    {
        // arrange
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:element name="Root"><xs:complexType><xs:sequence>
                <xs:element name="Note" default="ready" minOccurs="0">
                  <xs:complexType><xs:simpleContent><xs:extension base="xs:string">
                    <xs:attribute name="lang" type="xs:string" />
                  </xs:extension></xs:simpleContent></xs:complexType>
                </xs:element>
              </xs:sequence></xs:complexType></xs:element>
            </xs:schema>
            """;
        var type = Generate(schema, false, trackScalarPresence);
        var root = Activator.CreateInstance(type);
        var note = type.GetProperty("Note").GetValue(root);

        // act
        using var writer = new StringWriter();
        new XmlSerializer(type).Serialize(writer, root);

        // assert
        if (!trackScalarPresence)
        {
            Assert.Null(note);
            Assert.Null(XElement.Parse(writer.ToString()).Element("Note"));
            return;
        }

        Assert.NotNull(note);
        Assert.Equal("ready", XElement.Parse(writer.ToString()).Element("Note")?.Value);
        note.GetType().GetProperty("Value").SetValue(note, "changed");
        using var changedWriter = new StringWriter();
        new XmlSerializer(type).Serialize(changedWriter, root);
        Assert.Equal("changed", XElement.Parse(changedWriter.ToString()).Element("Note")?.Value);
    }

    private static Type Generate(string schema, bool separateSubstitutes, bool trackScalarPresence = false)
    {
        var writer = new MemoryOutputWriter();
        var generator = new Generator
        {
            OutputWriter = writer,
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "Integration" },
            SeparateSubstitutes = separateSubstitutes,
            UseShouldSerializeForDefaultValues = trackScalarPresence,
        };
        generator.Generate(new[] { new StringReader(schema) });
        return Compiler.Compile("Integration" + Guid.NewGuid().ToString("N"), writer.Content.ToArray()).GetType("Integration.Root");
    }
}
