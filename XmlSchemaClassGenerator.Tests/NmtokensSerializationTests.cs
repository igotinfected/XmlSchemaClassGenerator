using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class NmtokensSerializationTests
{
    [Theory]
    [InlineData("xs:NMTOKENS")]
    [InlineData("xs:IDREFS")]
    [InlineData("xs:ENTITIES")]
    [InlineData("Tokens")]
    public void Should_serialize_nmtokens_as_one_whitespace_separated_element(string schemaType)
    {
        // arrange
        var type = Generate(schemaType: schemaType);
        var value = Activator.CreateInstance(type);
        ((ICollection<string>)type.GetProperty("Keywords").GetValue(value)).Add("bus");
        ((ICollection<string>)type.GetProperty("Keywords").GetValue(value)).Add("delay");
        var serializer = new XmlSerializer(type);
        using var writer = new StringWriter();

        // act
        serializer.Serialize(writer, value);
        var xml = XElement.Parse(writer.ToString());

        // assert
        Assert.Equal("bus delay", Assert.Single(xml.Elements("Keywords")).Value);
    }

    [Fact]
    public void Should_deserialize_nmtokens_using_xml_whitespace()
    {
        // arrange
        var type = Generate();
        var serializer = new XmlSerializer(type);
        using var reader = new StringReader("<Root><Keywords> bus\t delay\n diversion </Keywords></Root>");

        // act
        var value = serializer.Deserialize(reader);

        // assert
        Assert.Equal(new[] { "bus", "delay", "diversion" }, (IEnumerable<string>)type.GetProperty("Keywords").GetValue(value));
    }

    [Theory]
    [InlineData(CollectionSettersMode.Public)]
    [InlineData(CollectionSettersMode.Private)]
    [InlineData(CollectionSettersMode.Init)]
    [InlineData(CollectionSettersMode.InitWithoutConstructorInitialization)]
    [InlineData(CollectionSettersMode.PublicWithoutConstructorInitialization)]
    public void Should_deserialize_without_changing_collection_setter_policy(CollectionSettersMode mode)
    {
        // arrange
        var type = Generate(mode: mode);
        using var reader = new StringReader("<Root><Keywords>bus delay</Keywords></Root>");

        // act
        var value = new XmlSerializer(type).Deserialize(reader);

        // assert
        Assert.Equal(new[] { "bus", "delay" }, (IEnumerable<string>)type.GetProperty("Keywords").GetValue(value));
        Assert.Equal(mode != CollectionSettersMode.Private, type.GetProperty("Keywords").SetMethod.IsPublic);
        Assert.NotNull(type.GetProperty("Keywords").GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Single());
        Assert.Equal(EditorBrowsableState.Never, ((EditorBrowsableAttribute)type.GetProperty("KeywordsXml").GetCustomAttributes(typeof(EditorBrowsableAttribute), false).Single()).State);
    }

    [Fact]
    public void Should_keep_namespaces_order_and_avoid_proxy_name_collisions()
    {
        // arrange
        var type = Generate(extraElement: "<xs:element name=\"KeywordsXml\" type=\"xs:string\" minOccurs=\"0\" />", qualified: true);
        using var reader = new StringReader("<Root xmlns=\"urn:tokens\"><Keywords>bus delay</Keywords><KeywordsXml>original</KeywordsXml></Root>");
        var serializer = new XmlSerializer(type);
        var value = serializer.Deserialize(reader);
        using var writer = new StringWriter();

        // act
        serializer.Serialize(writer, value);
        var xml = XElement.Parse(writer.ToString());

        // assert
        Assert.Equal(new[] { XName.Get("Keywords", "urn:tokens"), XName.Get("KeywordsXml", "urn:tokens") }, xml.Elements().Select(element => element.Name));
        Assert.Equal("original", type.GetProperty("KeywordsXml").GetValue(value));
        Assert.NotNull(type.GetProperty("KeywordsXml_"));
    }

    [Fact]
    public void Should_omit_empty_optional_list()
    {
        // arrange
        var type = Generate();
        var value = Activator.CreateInstance(type);
        using var writer = new StringWriter();

        // act
        new XmlSerializer(type).Serialize(writer, value);

        // assert
        Assert.Empty(XElement.Parse(writer.ToString()).Elements());
    }

    [Fact]
    public void Should_preserve_explicit_nil_but_omit_untouched_nillable_list()
    {
        // arrange
        var type = Generate(nillable: true);
        var serializer = new XmlSerializer(type);
        using var emptyWriter = new StringWriter();
        using var reader = new StringReader("<Root xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><Keywords xsi:nil=\"true\" /></Root>");
        using var writer = new StringWriter();

        // act
        serializer.Serialize(emptyWriter, Activator.CreateInstance(type));
        serializer.Serialize(writer, serializer.Deserialize(reader));

        // assert
        Assert.Empty(XElement.Parse(emptyWriter.ToString()).Elements());
        Assert.Equal("true", XElement.Parse(writer.ToString()).Element("Keywords").Attribute(XName.Get("nil", "http://www.w3.org/2001/XMLSchema-instance")).Value);
    }

    [Theory]
    [InlineData(typeof(Array))]
    [InlineData(typeof(Collection<>))]
    [InlineData(typeof(List<>))]
    public void Should_round_trip_configured_collection_types(Type collectionType)
    {
        // arrange
        var type = Generate(collectionType: collectionType, nullableDirective: true);
        var serializer = new XmlSerializer(type);
        using var reader = new StringReader("<Root><Keywords>bus delay</Keywords></Root>");
        using var writer = new StringWriter();

        // act
        var value = serializer.Deserialize(reader);
        serializer.Serialize(writer, value);

        // assert
        Assert.Equal(new[] { "bus", "delay" }, (IEnumerable<string>)type.GetProperty("Keywords").GetValue(value));
        Assert.Equal("bus delay", XElement.Parse(writer.ToString()).Element("Keywords").Value);
    }

    [Fact]
    public void Should_keep_complex_list_wrappers_without_string_proxies()
    {
        // arrange
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
              <xs:simpleType name="Coordinates"><xs:list itemType="xs:double" /></xs:simpleType>
              <xs:complexType name="Position"><xs:simpleContent><xs:extension base="Coordinates">
                <xs:attribute name="dimension" type="xs:int" />
              </xs:extension></xs:simpleContent></xs:complexType>
              <xs:element name="Root"><xs:complexType><xs:sequence>
                <xs:element name="Position" type="Position" />
              </xs:sequence></xs:complexType></xs:element>
            </xs:schema>
            """;

        // act
        var type = Generate(schemaOverride: schema);

        // assert
        Assert.Null(type.GetProperty("PositionXml"));
        Assert.Equal("Position", type.GetProperty("Position").PropertyType.GetGenericArguments().Single().Name);
    }

    private static Type Generate(string schemaType = "xs:NMTOKENS", CollectionSettersMode mode = CollectionSettersMode.Public, string extraElement = "", bool qualified = false, bool nillable = false, Type collectionType = null, bool nullableDirective = false, string schemaOverride = null)
    {
        var schema = $$"""
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" {{(qualified ? "targetNamespace=\"urn:tokens\" elementFormDefault=\"qualified\"" : "")}}>
              <xs:simpleType name="Tokens"><xs:list itemType="xs:string" /></xs:simpleType>
              <xs:element name="Root"><xs:complexType><xs:sequence>
                <xs:element name="Keywords" type="{{schemaType}}" minOccurs="0" nillable="{{nillable.ToString().ToLowerInvariant()}}" />
                {{extraElement}}
              </xs:sequence></xs:complexType></xs:element>
            </xs:schema>
            """;
        var writer = new MemoryOutputWriter();
        var generator = new Generator
        {
            OutputWriter = writer,
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "Tokens" },
            CollectionType = collectionType ?? typeof(List<>),
            EnableNullableDirective = nullableDirective,
            CollectionSettersMode = mode,
            EmitOrder = true,
        };
        generator.Generate(new[] { new StringReader(schemaOverride ?? schema) });
        var contents = writer.Content.Select(content => nullableDirective ? "#nullable enable\n" + content : content).ToArray();
        return Compiler.Compile("Tokens" + Guid.NewGuid().ToString("N"), contents).GetType("Tokens.Root");
    }
}
