using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using Xunit;

namespace XmlSchemaClassGenerator.Tests;

public class StrictDigitRangeTests
{
    private const string Schema = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
          <xs:element name="Root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="Amount">
                  <xs:simpleType>
                    <xs:restriction base="xs:decimal">
                      <xs:totalDigits value="3"/>
                      <xs:fractionDigits value="2"/>
                    </xs:restriction>
                  </xs:simpleType>
                </xs:element>
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    [Theory]
    [InlineData("999")]
    [InlineData("-999")]
    [InlineData("99.9")]
    [InlineData("-99.9")]
    [InlineData("9.99")]
    public void Should_accept_schema_valid_values_with_fewer_fractional_digits(string value)
    {
        // arrange
        var settings = new XmlReaderSettings { ValidationType = ValidationType.Schema };
        using var schemaReader = XmlReader.Create(new StringReader(Schema));
        settings.Schemas.Add(null, schemaReader);
        using var reader = XmlReader.Create(new StringReader($"<Root><Amount>{value}</Amount></Root>"), settings);
        while (reader.Read())
        {
        }

        var range = GenerateRange();

        // act
        var valid = range.IsValid(value);

        // assert
        Assert.True(valid);
        Assert.Equal(-999m, Convert.ToDecimal(range.Minimum));
        Assert.Equal(999m, Convert.ToDecimal(range.Maximum));
    }

    [Theory]
    [InlineData("1000")]
    [InlineData("-1000")]
    public void Should_reject_values_outside_total_digit_range(string value)
    {
        // arrange
        var range = GenerateRange();

        // act
        var valid = range.IsValid(value);

        // assert
        Assert.False(valid);
    }

    private static RangeAttribute GenerateRange()
    {
        var writer = new MemoryOutputWriter();
        var generator = new Generator
        {
            OutputWriter = writer,
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = _ => "StrictDigits" },
            GenerateStrictRangeBounds = true,
            DataAnnotationMode = DataAnnotationMode.All,
            EmitMetadataAttributes = true,
        };
        generator.Generate([new StringReader(Schema)]);
        var assembly = Compiler.Compile(nameof(StrictDigitRangeTests) + Guid.NewGuid().ToString("N"), writer.Content.ToArray());
        var property = assembly.GetType("StrictDigits.Root").GetProperty("Amount");
        return property.GetCustomAttributes(typeof(RangeAttribute), false).Cast<RangeAttribute>().Single();
    }
}
