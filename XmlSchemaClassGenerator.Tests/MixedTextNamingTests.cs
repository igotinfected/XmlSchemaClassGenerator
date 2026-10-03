using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class MixedTextNamingTests
{
    [Theory]
    [InlineData(false, "Value", "Value")]
    [InlineData(false, "Content", "Content")]
    [InlineData(true, "Value", "Text_1")]
    [InlineData(true, "Content", "Text_1")]
    public void Should_use_selected_mixed_text_name_for_collisions_and_defaults(bool legacyName, string configuredName, string expectedName)
    {
        // arrange
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:complexType name="Mixed" mixed="true">
                <xs:attribute name="Text" type="xs:string" />
              </xs:complexType>
              <xs:element name="Root"><xs:complexType><xs:sequence>
                <xs:element name="Note" type="Mixed" default="ready" minOccurs="0" />
              </xs:sequence></xs:complexType></xs:element>
            </xs:schema>
            """;
        var writer = new MemoryOutputWriter();
        var generator = new Generator
        {
            OutputWriter = writer,
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "MixedNames" },
            TextValuePropertyName = configuredName,
        };
        if (legacyName)
        {
            generator.UseLegacyMixedTextPropertyName = true;
        }

        // act
        generator.Generate(new[] { new StringReader(schema) });
        var assembly = Compiler.Compile("MixedNames" + Guid.NewGuid().ToString("N"), writer.Content.ToArray());
        var type = assembly.GetType("MixedNames.Root");
        var root = Activator.CreateInstance(type);
        var note = type.GetProperty("Note").GetValue(root);
        var text = note.GetType().GetProperties().Single(property => property.IsDefined(typeof(XmlTextAttribute), false));
        using var xmlWriter = new StringWriter();
        new XmlSerializer(type).Serialize(xmlWriter, root);

        // assert
        Assert.Equal(expectedName, text.Name);
        Assert.Equal(new[] { "ready" }, (string[])text.GetValue(note));
        Assert.Equal("ready", XElement.Parse(xmlWriter.ToString()).Element("Note").Value);
        if (legacyName)
        {
            Assert.Contains("renamed from <c>Text</c> to <c>Text_1</c>", string.Join("\n", writer.Content));
        }
    }
}
