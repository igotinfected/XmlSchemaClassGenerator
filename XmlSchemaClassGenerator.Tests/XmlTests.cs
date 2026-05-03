using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;
using Ganss.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Xml.XMLGen;
using Xunit;
using Xunit.Abstractions;

namespace XmlSchemaClassGenerator.Tests;

[TestCaseOrderer("XmlSchemaClassGenerator.Tests.PriorityOrderer", "XmlSchemaClassGenerator.Tests")]
public class XmlTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper Output = output;

    private static IEnumerable<string> ConvertXml(string name, IEnumerable<string> xsds, Generator generatorPrototype = null)
    {
        ArgumentNullException.ThrowIfNull(name);

        var writer = new MemoryOutputWriter();

        var gen = new Generator
        {
            OutputWriter = writer,
            Version = new VersionProvider("Tests", "1.0.0.1"),
            NamespaceProvider = generatorPrototype.NamespaceProvider,
            GenerateNullables = generatorPrototype.GenerateNullables,
            IntegerDataType = generatorPrototype.IntegerDataType,
            DataAnnotationMode = generatorPrototype.DataAnnotationMode,
            GenerateDesignerCategoryAttribute = generatorPrototype.GenerateDesignerCategoryAttribute,
            GenerateComplexTypesForCollections = generatorPrototype.GenerateComplexTypesForCollections,
            EntityFramework = generatorPrototype.EntityFramework,
            AssemblyVisible = generatorPrototype.AssemblyVisible,
            GenerateInterfaces = generatorPrototype.GenerateInterfaces,
            MemberVisitor = generatorPrototype.MemberVisitor,
            CodeTypeReferenceOptions = generatorPrototype.CodeTypeReferenceOptions,
            DoNotForceIsNullable = generatorPrototype.DoNotForceIsNullable,
            CreateGeneratedCodeAttributeVersion = generatorPrototype.CreateGeneratedCodeAttributeVersion,
            NetCoreSpecificCode = generatorPrototype.NetCoreSpecificCode,
            GenerateCommandLineArgumentsComment = generatorPrototype.GenerateCommandLineArgumentsComment,
            CommandLineArgumentsProvider = generatorPrototype.CommandLineArgumentsProvider,
            CollectionType = generatorPrototype.CollectionType,
            CollectionImplementationType = generatorPrototype.CollectionImplementationType,
            CollectionSettersMode = generatorPrototype.CollectionSettersMode,
            UseArrayItemAttribute = generatorPrototype.UseArrayItemAttribute,
            EnumAsString = generatorPrototype.EnumAsString,
            AllowDtdParse = generatorPrototype.AllowDtdParse,
            OmitXmlIncludeAttribute = generatorPrototype.OmitXmlIncludeAttribute,
            EnumCollection = generatorPrototype.EnumCollection,
            EnableNullableReferenceAttributes = generatorPrototype.EnableNullableReferenceAttributes,
            EnableNullableDirective = generatorPrototype.EnableNullableDirective,
            GenerateRequiredModifier = generatorPrototype.GenerateRequiredModifier,
            GenerateChoiceGroupAttributes = generatorPrototype.GenerateChoiceGroupAttributes,
            GenerateStrictFixedValues = generatorPrototype.GenerateStrictFixedValues,
            GenerateStrictRangeBounds = generatorPrototype.GenerateStrictRangeBounds,
        };

        gen.CommentLanguages.Clear();
        gen.CommentLanguages.UnionWith(generatorPrototype.CommentLanguages);

        gen.Generate(xsds.Select(i => new StringReader(i)));

        return writer.Content;
    }

    private static IEnumerable<string> ConvertXml(string name, string xsd, Generator generatorPrototype = null)
    {
        return ConvertXml(name, [xsd], generatorPrototype);
    }

    const string IS24Pattern = "xsd/is24/*/*.xsd";
    const string IS24ImmoTransferPattern = "xsd/is24immotransfer/is24immotransfer.xsd";
    const string WadlPattern = "xsd/wadl/wadl.xsd";
    const string ListPattern = "xsd/list/list.xsd";
    const string SimplePattern = "xsd/simple/*.xsd";
    const string ArrayOrderPattern = "xsd/array-order/array-order.xsd";
    const string ClientPattern = "xsd/client/client.xsd";
    const string IataPattern = "xsd/iata/*.xsd";
    const string TimePattern = "xsd/time/time.xsd";
    const string TableauPattern = "xsd/ts-api/*.xsd";
    const string VSTstPattern = "xsd/vstst/vstst.xsd";
    const string BpmnPattern = "xsd/bpmn/*.xsd";
    const string DtsxPattern = "xsd/dtsx/dtsx2.xsd";
    const string WfsPattern = "xsd/wfs/schemas.opengis.net/wfs/2.0/wfs.xsd";
    const string EppPattern = "xsd/epp/*.xsd";
    const string GraphMLPattern = "xsd/graphml/ygraphml.xsd";
    const string UnionPattern = "xsd/union/union.xsd";
    const string GuidPattern = "xsd/guid/*.xsd";
    const string NullableReferenceAttributesPattern = "xsd/nullablereferenceattributes/nullablereference.xsd";
    const string X3DPattern = "xsd/x3d/*.xsd";

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIata()
    {
        Compiler.Generate("Iata", IataPattern, new Generator
        {
            EntityFramework = true,
            DataAnnotationMode = DataAnnotationMode.All,
            NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey(""), "XmlSchema" }, { new NamespaceKey("http://www.iata.org/IATA/EDIST/2017.2"), "Iata" } }
                .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Iata" }.NamespaceProvider.GenerateNamespace),
            MemberVisitor = (member, model) => { },
            GenerateInterfaces = true
        });
        var typesToTest = new List<XmlQualifiedName> { new("AirShoppingRS", "http://www.iata.org/IATA/EDIST/2017.2") };
        SharedTestFunctions.TestSimple(Output, "Iata", IataPattern, typesToTest);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestGraphML()
    {
        Compiler.Generate("GraphML", GraphMLPattern, new Generator
        {
            NamespaceProvider = new Dictionary<NamespaceKey, string> {
                { new NamespaceKey(new Uri("graphml.xsd", UriKind.RelativeOrAbsolute), "http://graphml.graphdrawing.org/xmlns"), "GraphML.Main" },
                { new NamespaceKey(new Uri("graphml-structure.xsd", UriKind.RelativeOrAbsolute), "http://graphml.graphdrawing.org/xmlns"), "GraphML.Structure" },
                { new NamespaceKey(new Uri("ygraphxml.xsd", UriKind.RelativeOrAbsolute), "http://graphml.graphdrawing.org/xmlns"), "GraphML.Y" },
                { new NamespaceKey("http://www.w3.org/1999/xlink"), "XLink" },
                { new NamespaceKey("http://www.yworks.com/xml/graphml"), "YEd" },
            }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "GraphML" }.NamespaceProvider.GenerateNamespace),
        });
        SharedTestFunctions.TestSamples(Output, "GraphML", GraphMLPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestClient()
    {
        Compiler.Generate("Client", ClientPattern);
        SharedTestFunctions.TestSamples(Output, "Client", ClientPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestX3D()
    {
        Compiler.Generate("X3D", X3DPattern);
        var typesToTest = new List<XmlQualifiedName> { new("GeoLocation") };
        SharedTestFunctions.TestSimple(Output, "X3D", X3DPattern, typesToTest);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestGuid()
    {
        var assembly = Compiler.Generate("Guid", GuidPattern);
        var testType = assembly.GetType("Guid.Test");
        var idProperty = testType.GetProperty("Id");
        var elementIdProperty = testType.GetProperty("ElementId");
        var headerType = assembly.GetType("Guid.V1.Header");
        var referenceProperty = headerType.GetProperty("Reference");

        Assert.Equal(typeof(Nullable<>).MakeGenericType(typeof(Guid)), idProperty.PropertyType);
        Assert.Equal(typeof(Guid), elementIdProperty.PropertyType);
        Assert.Equal(typeof(Guid), referenceProperty.PropertyType);

        var serializer = new XmlSerializer(testType);

        var test = Activator.CreateInstance(testType);
        var idGuid = Guid.NewGuid();
        var elementGuid = Guid.NewGuid();

        idProperty.SetValue(test, idGuid);
        elementIdProperty.SetValue(test, elementGuid);

        var sw = new StringWriter();

        serializer.Serialize(sw, test);

        var xml = sw.ToString();
        var sr = new StringReader(xml);

        var o = serializer.Deserialize(sr);

        Assert.Equal(idGuid, idProperty.GetValue(o));
        Assert.Equal(elementGuid, elementIdProperty.GetValue(o));
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestUnion()
    {
        var assembly = Compiler.Generate("Union", UnionPattern, new Generator
        {
            NamespacePrefix = "Union",
            IntegerDataType = typeof(int),
            MapUnionToWidestCommonType = true
        });

        Assert.NotNull(assembly);

        SharedTestFunctions.TestSamples(Output, "Union", UnionPattern);

        var snapshotType = assembly.GetType("Union.Snapshot");
        Assert.NotNull(snapshotType);

        var date = snapshotType.GetProperty("Date");
        Assert.NotNull(date);
        Assert.Equal(typeof(DateTime), date.PropertyType);

        var count = snapshotType.GetProperty("Count");
        Assert.NotNull(count);
        Assert.Equal(typeof(int), count.PropertyType);

        var num = snapshotType.GetProperty("Num");
        Assert.NotNull(num);
        Assert.Equal(typeof(decimal), num.PropertyType);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSimpleContentEnum()
    {
        var assembly = Compiler.Generate("SimpleContentEnum", "xsd/simple/simplecontent-enum.xsd");

        const string ns = "SimpleContentEnum.Simplecontent";

        // The enum type should be generated
        var enumType = assembly.GetType($"{ns}.TransConfirmationCodeTypeEnum");
        if (enumType == null)
        {
            var names = string.Join(", ", assembly.GetTypes().Select(t => t.FullName));
            Assert.Fail($"Enum type not found. Available types: {names}");
        }

        // Verify it's an enum with the expected values
        Assert.True(enumType.IsEnum);
        var enumValues = Enum.GetNames(enumType);
        Assert.Contains("Always", enumValues);
        Assert.Contains("Never", enumValues);
        Assert.Contains("OnError", enumValues);

        // The derived class should exist and inherit from the base
        var type = assembly.GetType($"{ns}.TransConfirmationCodeType");
        Assert.NotNull(type);

        var baseType = assembly.GetType($"{ns}.CodeType");
        Assert.Equal(baseType, type.BaseType);

        // The derived class inherits the string Value property from the base class
        var valueProperty = type.GetProperty("Value");
        Assert.NotNull(valueProperty);
        Assert.Equal(typeof(string), valueProperty.PropertyType);

        // The derived class should have an EnumValue adapter property
        var enumValueProperty = type.GetProperty("EnumValue");
        Assert.NotNull(enumValueProperty);
        Assert.Equal(typeof(Nullable<>).MakeGenericType(enumType), enumValueProperty.PropertyType);

        // Test that the EnumValue property works correctly
        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        // Set Value to a string and verify EnumValue returns the correct enum
        valueProperty.SetValue(instance, "Always");
        var enumValue = enumValueProperty.GetValue(instance);
        Assert.NotNull(enumValue);
        Assert.Equal("Always", enumValue.ToString());

        // Set EnumValue and verify Value is updated
        var alwaysValue = Enum.Parse(enumType, "Never");
        enumValueProperty.SetValue(instance, alwaysValue);
        var stringValue = valueProperty.GetValue(instance);
        Assert.Equal("Never", stringValue);

        // Set EnumValue to null and verify Value is null
        enumValueProperty.SetValue(instance, null);
        stringValue = valueProperty.GetValue(instance);
        Assert.Null(stringValue);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestList()
    {
        Compiler.Generate("List", ListPattern);
        SharedTestFunctions.TestSamples(Output, "List", ListPattern);
    }

    [Fact]
    public void TestListWithPrivatePropertySetters()
    {
        var assembly = Compiler.Generate("List", ListPattern, new Generator() {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.Private
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPrivate).OrderBy(p=>p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);

        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithPublicPropertySetters()
    {
        var assembly = Compiler.Generate("ListPublic", ListPattern, new Generator {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.Public
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p=>p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p=>p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);

        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithPublicPropertySettersWithoutConstructors()
    {
        var assembly = Compiler.Generate("ListPublicWithoutConstructorInitialization", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.PublicWithoutConstructorInitialization
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.Null(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithInitPropertySetters()
    {
        var assembly = Compiler.Generate("ListPublic", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.Init
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var requiredCustomModifiers = collectionPropertyInfos.Select(p => p.SetMethod.ReturnParameter.GetRequiredCustomModifiers()).ToList();
        Assert.Equal(collectionPropertyInfos.Count, requiredCustomModifiers.Count);
        Assert.All(requiredCustomModifiers, m => Assert.Contains(typeof(System.Runtime.CompilerServices.IsExternalInit), m));

        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.NotNull(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithInitPropertySettersWithoutConstructors()
    {
        var assembly = Compiler.Generate("ListInitWithoutConstructorInitialization", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.InitWithoutConstructorInitialization
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var requiredCustomModifiers = collectionPropertyInfos.Select(p => p.SetMethod.ReturnParameter.GetRequiredCustomModifiers()).ToList();
        Assert.Equal(collectionPropertyInfos.Count, requiredCustomModifiers.Count);
        Assert.All(requiredCustomModifiers, m => Assert.Contains(typeof(System.Runtime.CompilerServices.IsExternalInit), m));
        var myClassInstance = Activator.CreateInstance(myClassType);
        foreach (var collectionPropertyInfo in publicCollectionPropertyInfos)
        {
            Assert.Null(collectionPropertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestListWithPublicPropertySettersWithoutConstructorsSpecified()
    {
        var assembly = Compiler.Generate("ListPublicWithoutConstructorInitialization", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CollectionSettersMode = CollectionSettersMode.PublicWithoutConstructorInitialization
        });
        Assert.NotNull(assembly);
        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);
        var iListType = typeof(Collection<>);
        var collectionPropertyInfos = myClassType.GetProperties().Where(p => p.PropertyType.IsGenericType && iListType.IsAssignableFrom(p.PropertyType.GetGenericTypeDefinition())).OrderBy(p => p.Name).ToList();
        var publicCollectionPropertyInfos = collectionPropertyInfos.Where(p => p.SetMethod.IsPublic).OrderBy(p => p.Name).ToList();
        Assert.NotEmpty(collectionPropertyInfos);
        Assert.Equal(collectionPropertyInfos, publicCollectionPropertyInfos);
        var myClassInstance = Activator.CreateInstance(myClassType);

        var propertyNamesWithSpecifiedPostfix = publicCollectionPropertyInfos.Select(p => p.Name + "Specified").ToHashSet();
        var propertiesWithSpecifiedPostfix =
            myClassType.GetProperties().Where(p => propertyNamesWithSpecifiedPostfix.Contains(p.Name)).ToList();

        //Null collection
        foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
        {
            Assert.False((bool)propertyInfo.GetValue(myClassInstance));
        }

        foreach (var propertyInfo in publicCollectionPropertyInfos)
        {
            var collection = Activator.CreateInstance(propertyInfo.PropertyType);
            propertyInfo.SetValue(myClassInstance, collection);
        }

        //Not Null but empty collection
        foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
        {
            Assert.False((bool)propertyInfo.GetValue(myClassInstance));
        }

        foreach (var propertyInfo in publicCollectionPropertyInfos)
        {

            var collection = Activator.CreateInstance(propertyInfo.PropertyType);
            propertyInfo.PropertyType.InvokeMember("Add", BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance, null, collection,
                [null]);
            propertyInfo.SetValue(myClassInstance, collection);
        }

        //Not Null and not empty collection
        foreach (var propertyInfo in propertiesWithSpecifiedPostfix)
        {
            Assert.True((bool)propertyInfo.GetValue(myClassInstance));
        }
    }

    [Fact]
    public void TestEnumCollection()
    {
        var assembly = Compiler.Generate("ListEnumCollection", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            EnumCollection = true
        });

        Assert.NotNull(assembly);

        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);

        var enumType = assembly.GetType("List.EnumType");
        Assert.NotNull(enumType);
        Assert.True(enumType.IsEnum);

        // Element property: should be Collection<EnumType>, not Collection<string>
        var enumElemProp = myClassType.GetProperty("EnumElem");
        Assert.NotNull(enumElemProp);
        Assert.True(enumElemProp.PropertyType.IsGenericType);
        Assert.Equal(enumType, enumElemProp.PropertyType.GetGenericArguments()[0]);

        // Attribute property: should use enum type, not string
        var enumAttrProp = myClassType.GetProperty("EnumAttr");
        Assert.NotNull(enumAttrProp);
        if (enumAttrProp.PropertyType.IsArray)
        {
            Assert.Equal(enumType, enumAttrProp.PropertyType.GetElementType());
        }
        else if (enumAttrProp.PropertyType.IsGenericType)
        {
            Assert.Equal(enumType, enumAttrProp.PropertyType.GetGenericArguments()[0]);
        }
        else
        {
            Assert.Fail($"Expected array or generic collection, got {enumAttrProp.PropertyType}");
        }

        // Non-enum list properties should remain string-based
        var timeListElemProp = myClassType.GetProperty("TimeListElem");
        Assert.NotNull(timeListElemProp);
        Assert.True(timeListElemProp.PropertyType.IsGenericType);
        Assert.Equal(typeof(string), timeListElemProp.PropertyType.GetGenericArguments()[0]);
    }

    [Theory]
    [InlineData(typeof(Collection<>), null)]
    [InlineData(typeof(List<>), null)]
    [InlineData(typeof(HashSet<>), null)]
    [InlineData(typeof(Array), null)]
    public void TestEnumCollectionRespectsCollectionType(Type collectionType, Type collectionImplementationType)
    {
        var assembly = Compiler.Generate($"ListEnumCollection_{collectionType.Name}_{collectionImplementationType?.Name}", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            EnumCollection = true,
            CollectionType = collectionType,
            CollectionImplementationType = collectionImplementationType
        });

        Assert.NotNull(assembly);

        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);

        var enumType = assembly.GetType("List.EnumType");
        Assert.NotNull(enumType);

        var enumElemProp = myClassType.GetProperty("EnumElem");
        Assert.NotNull(enumElemProp);

        if (collectionType == typeof(System.Array))
        {
            Assert.True(enumElemProp.PropertyType.IsArray);
            Assert.Equal(enumType, enumElemProp.PropertyType.GetElementType());
        }
        else
        {
            Assert.True(enumElemProp.PropertyType.IsGenericType);
            Assert.Equal(enumType, enumElemProp.PropertyType.GetGenericArguments()[0]);

            var expectedCollectionType = collectionType.MakeGenericType(enumType);
            Assert.Equal(expectedCollectionType, enumElemProp.PropertyType);
        }
    }

    [Fact]
    public void TestEnumCollectionListElementHasStringProxy()
    {
        // xsd:list element properties with EnumCollection should generate:
        // 1. A typed collection property with [XmlIgnore] for programmatic use
        // 2. A string proxy property with [XmlElement] for correct space-separated serialization
        var assembly = Compiler.Generate("ListEnumCollectionProxy", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            EnumCollection = true,
        });

        Assert.NotNull(assembly);

        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);

        var enumType = assembly.GetType("List.EnumType");
        Assert.NotNull(enumType);

        // The typed collection property should exist and have [XmlIgnore]
        var enumElemProp = myClassType.GetProperty("EnumElem");
        Assert.NotNull(enumElemProp);
        Assert.True(enumElemProp.PropertyType.IsGenericType);
        Assert.Equal(enumType, enumElemProp.PropertyType.GetGenericArguments()[0]);
        Assert.NotNull(enumElemProp.GetCustomAttribute(typeof(XmlIgnoreAttribute)));

        // The string proxy property should exist and have [XmlElement]
        var proxyProp = myClassType.GetProperty("EnumElemXml");
        Assert.NotNull(proxyProp);
        Assert.Equal(typeof(string), proxyProp.PropertyType);
        Assert.NotNull(proxyProp.GetCustomAttribute(typeof(XmlElementAttribute)));

        // The proxy should be hidden from IntelliSense
        var editorBrowsable = (System.ComponentModel.EditorBrowsableAttribute)proxyProp.GetCustomAttribute(typeof(System.ComponentModel.EditorBrowsableAttribute));
        Assert.NotNull(editorBrowsable);
        Assert.Equal(System.ComponentModel.EditorBrowsableState.Never, editorBrowsable.State);
    }

    [Fact]
    public void TestEnumCollectionListElementSerializesAsSpaceSeparated()
    {
        // Verify that the string proxy property correctly converts between
        // typed enum collection and space-separated XML string.
        var assembly = Compiler.Generate("ListEnumCollectionSerialize", ListPattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "List",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            EnumCollection = true,
        });

        Assert.NotNull(assembly);

        var myClassType = assembly.GetType("List.MyClass");
        Assert.NotNull(myClassType);

        var instance = Activator.CreateInstance(myClassType);

        // Set the typed collection to two enum values
        var enumType = assembly.GetType("List.EnumType");
        Assert.NotNull(enumType);
        var enumValues = Enum.GetValues(enumType);
        Assert.True(enumValues.Length >= 2);

        var listType = typeof(Collection<>).MakeGenericType(enumType);
        var list = Activator.CreateInstance(listType);
        listType.GetMethod("Add").Invoke(list, [enumValues.GetValue(0)]);
        listType.GetMethod("Add").Invoke(list, [enumValues.GetValue(1)]);

        myClassType.GetProperty("EnumElem").SetValue(instance, list);

        // Read the proxy property — should be space-separated XML enum names
        var proxyValue = (string)myClassType.GetProperty("EnumElemXml").GetValue(instance);
        Assert.NotNull(proxyValue);
        Assert.Contains(" ", proxyValue); // space-separated
        Assert.DoesNotContain("\n", proxyValue); // single line
        Assert.Equal(2, proxyValue.Split(' ').Length);
    }

    public static TheoryData<CodeTypeReferenceOptions, NamingScheme, Type> TestSimpleData() {
        var theoryData = new TheoryData<CodeTypeReferenceOptions, NamingScheme, Type>();
        foreach (var referenceMode in new[]
            { CodeTypeReferenceOptions.GlobalReference, /*CodeTypeReferenceOptions.GenericTypeParameter*/ })
        foreach (var namingScheme in new[] { NamingScheme.Direct, NamingScheme.PascalCase })
        foreach (var collectionType in new[] { typeof(Collection<>), /*typeof(Array)*/ })
        {
            theoryData.Add(referenceMode, namingScheme, collectionType);
        }
        return theoryData;
    }


    [Theory, TestPriority(1)]
    [MemberData(nameof(TestSimpleData))]
    [UseCulture("en-US")]
    public void TestSimple(CodeTypeReferenceOptions referenceMode, NamingScheme namingScheme, Type collectionType)
    {
        var name = $"Simple_{referenceMode}_{namingScheme}_{collectionType}";
        Compiler.Generate(name, SimplePattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "Simple",
            GenerateDescriptionAttribute = true,
            CodeTypeReferenceOptions = referenceMode,
            NetCoreSpecificCode = true,
            NamingScheme = namingScheme,
            CollectionType = collectionType,
            CollectionSettersMode = CollectionSettersMode.Public
        });
        SharedTestFunctions.TestSamples(Output, name, SimplePattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestArrayOrder()
    {
        Compiler.Generate("ArrayOrder", ArrayOrderPattern, new Generator
        {
            NamespacePrefix = "ArrayOrder",
            EmitOrder = true
        });
        SharedTestFunctions.TestSamples(Output, "ArrayOrder", ArrayOrderPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIS24RestApi()
    {
        Compiler.Generate("IS24RestApi", IS24Pattern, new Generator
        {
            GenerateNullables = true,
            IntegerDataType = typeof(int),
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateDesignerCategoryAttribute = false,
            GenerateComplexTypesForCollections = true,
            EntityFramework = false,
            GenerateInterfaces = true,
            NamespacePrefix = "IS24RestApi",
            GenerateDescriptionAttribute = true,
            TextValuePropertyName = "Value",
            CompactTypeNames = true
        });
        SharedTestFunctions.TestSamples(Output, "IS24RestApi", IS24Pattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIS24RestApiShouldSerialize()
    {
        Compiler.Generate("IS24RestApiShouldSerialize", IS24Pattern, new Generator
        {
            GenerateNullables = true,
            GenerateInterfaces = true,
            NamespacePrefix = "IS24RestApi",
            GenerateDescriptionAttribute = true,
            UseShouldSerializePattern = true
        });
        SharedTestFunctions.TestSamples(Output, "IS24RestApiShouldSerialize", IS24Pattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestWadl()
    {
        Compiler.Generate("Wadl", WadlPattern, new Generator
        {
            EntityFramework = true,
            DataAnnotationMode = DataAnnotationMode.All,
            NamespaceProvider = new Dictionary<NamespaceKey, string> { { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace),
            MemberVisitor = (member, model) => { }
        });
        SharedTestFunctions.TestSamples(Output, "Wadl", WadlPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestIS24ImmoTransfer()
    {
        Compiler.Generate("IS24ImmoTransfer", IS24ImmoTransferPattern);
        SharedTestFunctions.TestSamples(Output, "IS24ImmoTransfer", IS24ImmoTransferPattern);

        Compiler.Generate("IS24ImmoTransferSeparate", IS24ImmoTransferPattern, new Generator
        {
            SeparateSubstitutes = true
        });
        SharedTestFunctions.TestSamples(Output, "IS24ImmoTransferSeparate", IS24ImmoTransferPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestTableau()
    {
        Compiler.Generate("Tableau", TableauPattern, new Generator { CompactTypeNames = true });
        SharedTestFunctions.TestSamples(Output, "Tableau", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSeparateClasses()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.Separate"));
        Compiler.Generate("Tableau.Separate", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                SeparateClasses = true,
                EnableDataBinding = true
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.Separate", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestArray()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.Array"));
        Compiler.Generate("Tableau.Array", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                EnableDataBinding = true,
                CollectionType = typeof(System.Array),
                CollectionSettersMode = CollectionSettersMode.Public
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.Array", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSerializeEmptyCollection()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.EmptyCollection"));
        Compiler.Generate("Tableau.EmptyCollection", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                EnableDataBinding = true,
                CollectionSettersMode = CollectionSettersMode.Private,
                SerializeEmptyCollections = true
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.EmptyCollection", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestSerializeEmptyPublicCollection()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "Tableau.EmptyPublicCollection"));
        Compiler.Generate("Tableau.EmptyPublicCollection", TableauPattern,
            new Generator
            {
                OutputWriter = output,
                EnableDataBinding = true,
                CollectionSettersMode = CollectionSettersMode.Public,
                SerializeEmptyCollections = true
            });
        SharedTestFunctions.TestSamples(Output, "Tableau.EmptyPublicCollection", TableauPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestDtsx()
    {
        Compiler.Generate("Dtsx", DtsxPattern, new Generator());
        SharedTestFunctions.TestSamples(Output, "Dtsx", DtsxPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestVSTst()
    {
        Compiler.Generate("VSTst", VSTstPattern, new Generator
        {
            TextValuePropertyName = "TextValue",
            GenerateComplexTypesForCollections = true
        });
        SharedTestFunctions.TestSamples(Output, "VSTst", VSTstPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestWfs()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "wfs"));
        Compiler.Generate("wfs", WfsPattern,
            new Generator
            {
                OutputWriter = output,
                EmitOrder = true,
                GenerateInterfaces = true
            });
        SharedTestFunctions.TestSamples(Output, "wfs", WfsPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestEpp()
    {
        var output = new FileWatcherOutputWriter(Path.Combine("output", "epp"));
        Compiler.Generate("epp", EppPattern,
            new Generator
            {
                OutputWriter = output,
                GenerateInterfaces = false,
                UniqueTypeNamesAcrossNamespaces = true,
                NamespaceProvider = new Dictionary<NamespaceKey, string>
                {
                    { new NamespaceKey("urn:ietf:params:xml:ns:eppcom-1.0"), "FoxHillSolutions.Escrow.eppcom" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeDomain-1.0"), "FoxHillSolutions.Escrow.rdeDomain" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeHeader-1.0"), "FoxHillSolutions.Escrow.rdeHeader" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeHost-1.0"), "FoxHillSolutions.Escrow.rdeHost" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeIDN-1.0"), "FoxHillSolutions.Escrow.rdeIDN" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeRegistrar-1.0"), "FoxHillSolutions.Escrow.Registrar" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeDnrdCommon-1.0"), "FoxHillSolutions.Escrow.DnrdCommon" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:secDNS-1.1"), "FoxHillSolutions.Escrow.secDNS" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:domain-1.0"), "FoxHillSolutions.Escrow.domain" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:contact-1.0"), "FoxHillSolutions.Escrow.contact" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:host-1.0"), "FoxHillSolutions.Escrow.host" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rgp-1.0"), "FoxHillSolutions.Escrow.rgp" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:epp-1.0"), "FoxHillSolutions.Escrow.epp" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rde-1.0"), "FoxHillSolutions.Escrow.rde" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeContact-1.0"), "FoxHillSolutions.Escrow.rdeContact" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeEppParams-1.0"), "FoxHillSolutions.Escrow.rdeEppParams" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdeNNDN-1.0"), "FoxHillSolutions.Escrow.rdeNNDN" },
                    { new NamespaceKey("urn:ietf:params:xml:ns:rdePolicy-1.0"), "FoxHillSolutions.Escrow.rdePolicy" },
                }.ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Epp" }.NamespaceProvider.GenerateNamespace),
            });
        SharedTestFunctions.TestSamples(Output, "epp", EppPattern);
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestXbrl()
    {
        var outputPath = Path.Combine("output", "xbrl");

        var gen = new Generator
        {
            OutputFolder = outputPath,
            GenerateInterfaces = false,
            UniqueTypeNamesAcrossNamespaces = true,
        };

        gen.NamespaceProvider.Add(new NamespaceKey("http://www.xbrl.org/2003/XLink"), "XbrlLink");

        var xsdFiles = new[] { Path.Combine(Directory.GetCurrentDirectory(), "xsd", "xbrl", "xhtml-inlinexbrl-1_1.xsd") };

        var assembly = Compiler.GenerateFiles("Xbrl", xsdFiles, gen);
        Assert.NotNull(assembly);

        var testFiles = new Dictionary<string, string>
        {
            { "Schaltbau.xhtml", "XhtmlHtmlType" },
            { "GLEIF Annual Accounts.html", "XhtmlHtmlType" },
        };

        foreach (var testFile in testFiles)
        {
            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var xmlString = File.ReadAllText($"xml/xbrl_tests/{testFile.Key}");
            xmlString = Regex.Replace(xmlString, "xsi:schemaLocation=\"[^\"]*\"", string.Empty);
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject, SharedTestFunctions.GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    private static readonly XmlQualifiedName AnyType = new("anyType", XmlSchema.Namespace);

    public static TheoryData<string> Classes =>
    [
        "ApartmentBuy",
        "ApartmentRent",
        "AssistedLiving",
        "CompulsoryAuction",
        "GarageBuy",
        "GarageRent",
        "Gastronomy",
        "HouseBuy",
        "HouseRent",
        "HouseType",
        "Industry",
        "Investment",
        "LivingBuySite",
        "LivingRentSite",
        "Office",
        "SeniorCare",
        "ShortTermAccommodation",
        "SpecialPurpose",
        "Store",
        "TradeSite",
    ];

    [Theory, TestPriority(2)]
    [MemberData(nameof(Classes))]
    public void ProducesSameXmlAsXsd(string c)
    {
        var assembly = Compiler.Generate("IS24RestApi", IS24Pattern);

        var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
        Assert.NotNull(t1);
        var t2 = Assembly.GetExecutingAssembly().GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace == "IS24RestApi.Xsd");
        Assert.NotNull(t2);
        var f = char.ToLower(c[0]) + c[1..];
        TestCompareToXsd(t1, t2, f);
    }

    static void TestCompareToXsd(Type t1, Type t2, string file)
    {
        foreach (var suffix in new[] { "max", "min" })
        {
            var serializer1 = new XmlSerializer(t1);
            var serializer2 = new XmlSerializer(t2);
            var xml = ReadXml(string.Format("{0}_{1}", file, suffix));
            var o1 = serializer1.Deserialize(new StringReader(xml));
            var o2 = serializer2.Deserialize(new StringReader(xml));
            var x1 = SharedTestFunctions.Serialize(serializer1, o1);
            var x2 = SharedTestFunctions.Serialize(serializer2, o2);

            File.WriteAllText("x1.xml", x1);
            File.WriteAllText("x2.xml", x2);

            Assert.Equal(x2, x1);
        }
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestCustomNamespaces()
    {
        string customNsPattern = "|{0}={1}";
        string bpmnXsd = "BPMN20.xsd";
        string semantXsd = "Semantic.xsd";
        string bpmndiXsd = "BPMNDI.xsd";
        string dcXsd = "DC.xsd";
        string diXsd = "DI.xsd";

        Dictionary<string, string> xsdToCsharpNsMap = new()
        {
            { bpmnXsd, "Namespace1" },
            { semantXsd, "Namespace1" },
            { bpmndiXsd, "Namespace2" },
            { dcXsd, "Namespace3" },
            { diXsd, "Namespace4" }
        };

        Dictionary<string, string> xsdToCsharpTypeMap = new()
        {
            { bpmnXsd, "TDefinitions" },
            { semantXsd, "TActivity" },
            { bpmndiXsd, "BpmnDiagram" },
            { dcXsd, "Font" },
            { diXsd, "DiagramElement" }
        };

        List<string> customNamespaceConfig = [];

        foreach (var ns in xsdToCsharpNsMap)
            customNamespaceConfig.Add(string.Format(customNsPattern, ns.Key, ns.Value));

        var assembly = Compiler.Generate("Bpmn", BpmnPattern, new Generator
        {
            DataAnnotationMode = DataAnnotationMode.All,
            GenerateNullables = true,
            MemberVisitor = (member, model) => { },
            NamespaceProvider = customNamespaceConfig.Select(n => CodeUtilities.ParseNamespace(n, null)).ToNamespaceProvider()
        });
        Assert.NotNull(assembly);

        Type type = null;

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[bpmnXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[bpmnXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[semantXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[semantXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[bpmndiXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[bpmndiXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[dcXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[dcXsd], type.Namespace);

        type = assembly.GetTypes().SingleOrDefault(t => t.Name == xsdToCsharpTypeMap[diXsd]);
        Assert.NotNull(type);
        Assert.Equal(xsdToCsharpNsMap[diXsd], type.Namespace);
    }

    [Fact, TestPriority(2)]
    [UseCulture("en-US")]
    public void TestBpmn()
    {
        PerformBpmnTest("Bpmn");
        PerformBpmnTest("BpmnSeparate", new Generator
        {
            SeparateSubstitutes = true
        });
    }

    private void PerformBpmnTest(string name, Generator generator = null)
    {
        var assembly = Compiler.Generate(name, BpmnPattern, generator);
        Assert.NotNull(assembly);

        var type = assembly.GetTypes().SingleOrDefault(t => t.Name == "TDefinitions");
        Assert.NotNull(type);

        var serializer = new XmlSerializer(type);
        serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
        serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
        var unknownNodeError = false;
        var unknownAttrError = false;

        void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
        {
            unknownNodeError = true;
        }

        void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
        {
            unknownAttrError = true;
        }

        var testFiles = Glob.ExpandNames(Path.Combine("xml", "bpmn_tests", "*.bpmn"));

        foreach (var testFile in testFiles)
        {
            var xmlString = File.ReadAllText(testFile);
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject, SharedTestFunctions.GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    [Theory, TestPriority(3)]
    [MemberData(nameof(Classes))]
    public void CanSerializeAndDeserializeAllExampleXmlFiles(string c)
    {
        var assembly = Compiler.Generate("IS24RestApi", IS24Pattern);

        var t1 = assembly.GetTypes().SingleOrDefault(t => t.Name == c && t.Namespace.StartsWith("IS24RestApi.Offer.Realestates"));
        Assert.NotNull(t1);
        var f = char.ToLower(c[0]) + c[1..];
        TestRoundtrip(t1, f);
    }

    static void TestRoundtrip(Type t, string file)
    {
        var serializer = new XmlSerializer(t);

        foreach (var suffix in new[] { "min", "max" })
        {
            var xml = ReadXml(string.Format("{0}_{1}", file, suffix));

            var deserializedObject = serializer.Deserialize(new StringReader(xml));

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject);

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    static string ReadXml(string name)
    {
        var xml = File.ReadAllText(Path.Combine("xml", name + ".xml"));
        return xml;
    }

    [Fact]
    public void DontGenerateElementForEmptyCollectionInChoice()
    {
        var assembly = Compiler.Generate("Tableau", TableauPattern, new Generator());
        Assert.NotNull(assembly);
        var requestType = assembly.GetType("Api.TsRequest");
        Assert.NotNull(requestType);
        var r = Activator.CreateInstance(requestType);
        var s = new XmlSerializer(requestType);
        var sw = new StringWriter();
        s.Serialize(sw, r);
        var xml = sw.ToString();
        Assert.DoesNotContain("tags", xml, StringComparison.OrdinalIgnoreCase);
    }


    [Theory]
    [InlineData(CodeTypeReferenceOptions.GlobalReference, "[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]")]
    [InlineData((CodeTypeReferenceOptions)0, "[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]")]
    public void EditorBrowsableAttributeRespectsCodeTypeReferenceOptions(CodeTypeReferenceOptions codeTypeReferenceOptions, string expectedLine)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
		<xs:complexType name=""document"">
			<xs:attribute name=""some-value"">
				<xs:simpleType>
					<xs:restriction base=""xs:string"">
						<xs:enumeration value=""one""/>
						<xs:enumeration value=""two""/>
					</xs:restriction>
				</xs:simpleType>
			</xs:attribute>
			<xs:attribute name=""system"" type=""xs:string""/>
		</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(EditorBrowsableAttributeRespectsCodeTypeReferenceOptions), xsd, new Generator
        {
            CodeTypeReferenceOptions = codeTypeReferenceOptions,
            GenerateNullables = true,
            GenerateInterfaces = true,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        Assert.Contains(
            expectedLine,
            generatedType.First());
    }

    [Fact]
    public void MixedTypeMustNotCollideWithExistingMembers()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:elem"">
	</xs:element>
	<xs:complexType name=""elem"" mixed=""true"">
		<xs:attribute name=""Text"" type=""xs:string""/>
	</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(MixedTypeMustNotCollideWithExistingMembers), xsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        Assert.Contains(
            @"public string[] Text_1 { get; set; }",
            generatedType.First());
    }

    [Fact]
    public void MixedTypeMustNotCollideWithContainingTypeName()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:Text"">
	</xs:element>
	<xs:complexType name=""Text"" mixed=""true"">
	</xs:complexType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(MixedTypeMustNotCollideWithExistingMembers), xsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        });

        Assert.Contains(
            @"public string[] Text_1 { get; set; }",
            generatedType.First());
    }

    [Theory]
    [InlineData(@"xml/sameattributenames.xsd", @"xml/sameattributenames_import.xsd")]
    public void CollidingAttributeAndPropertyNamesCanBeResolved(params string[] files)
    {
        // Compilation would previously throw due to duplicate type name within type
        var assembly = Compiler.GenerateFiles("AttributesWithSameName", files);

        Assert.NotNull(assembly);
    }

    [Fact]
    public void CollidingElementAndComplexTypeNamesCanBeResolved()
    {
        const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
  <xs:complexType name=""MyType"">
    <xs:sequence>
      <xs:element maxOccurs=""1"" minOccurs=""0"" name=""output"" type=""xs:string""/>
    </xs:sequence>
  </xs:complexType>
  <xs:element name=""MyType"">
    <xs:simpleType>
      <xs:restriction base=""xs:string"">
        <xs:enumeration value=""Choice1""/>
        <xs:enumeration value=""Choice2""/>
        <xs:enumeration value=""Choice3""/>
      </xs:restriction>
    </xs:simpleType>
  </xs:element>
</xs:schema>
";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };

        var generatedType = ConvertXml(nameof(CollidingElementAndComplexTypeNamesCanBeResolved), xsd, generator).First();

        Assert.Contains(@"public partial class MyType", generatedType);
        Assert.Contains(@"public enum MyType2", generatedType);
    }

    [Fact]
    public void EnumAsStringOption()
    {
        const string xsd = @"<?xml version=""1.0"" encoding = ""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"">
	<xs:element name=""Authorisation"">
		<xs:complexType>
			<xs:sequence>
				<xs:element name=""type"">
					<xs:simpleType>
						<xs:restriction base=""xs:string"">
							<xs:enumeration value=""C019""/>
							<xs:enumeration value=""C512""/>
							<xs:enumeration value=""C513""/>
							<xs:enumeration value=""C514""/>
						</xs:restriction>
					</xs:simpleType>
				</xs:element>
			</xs:sequence>
		</xs:complexType>
	</xs:element>
</xs:schema>
";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            EnumAsString = true
        };

        var generatedType = ConvertXml(nameof(EnumAsStringOption), xsd, generator).First();

        Assert.Contains(@"public string Type", generatedType);
    }

    [Fact]
    public void ComplexTypeWithAttributeGroupExtension()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:attributeGroup name=""justify"">
    <xs:attribute name=""justify"" type=""simpleType""/>
  </xs:attributeGroup>
  <xs:complexType name=""group-name"">
    <xs:simpleContent>
      <xs:extension base=""xs:string"">
        <xs:attributeGroup ref=""justify""/>
      </xs:extension>
    </xs:simpleContent>
  </xs:complexType>
  <xs:simpleType name=""simpleType"">
    <xs:restriction base=""xs:token"">
      <xs:enumeration value=""foo""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

        var generator = new Generator
        {
            GenerateInterfaces = false,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            CommentLanguages = { "de", "en" },
            CreateGeneratedCodeAttributeVersion = false
        };

        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);

        var csharp = Assert.Single(contents);

        CompareOutput(
            @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by Tests
namespace Test
{


    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", """")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""group-name"", Namespace="""")]
    [System.ComponentModel.DesignerCategoryAttribute(""code"")]
    public partial class GroupName
    {

        /// <summary>
        /// <para xml:lang=""de"">Ruft den Text ab oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets the text value.</para>
        /// </summary>
        [System.Xml.Serialization.XmlTextAttribute()]
        public string Value { get; set; }

        [System.Xml.Serialization.XmlAttributeAttribute(""justify"")]
        public SimpleType Justify { get; set; }

        /// <summary>
        /// <para xml:lang=""de"">Ruft einen Wert ab, der angibt, ob die Justify-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
        /// <para xml:lang=""en"">Gets or sets a value indicating whether the Justify property is specified.</para>
        /// </summary>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool JustifySpecified { get; set; }
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", """")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""simpleType"", Namespace="""")]
    public enum SimpleType
    {

        [System.Xml.Serialization.XmlEnumAttribute(""foo"")]
        Foo,
    }
}
", csharp);
    }

    [Fact]
    public void ChoiceMembersAreNullable()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""Root"">
      <xs:sequence>
      <!-- Choice directly inside a complex type -->
      <xs:element name=""Sub"">
        <xs:complexType>
          <xs:choice>
              <xs:element name=""Opt1"" type=""xs:int""/>
              <xs:element name=""Opt2"" type=""xs:int""/>
          </xs:choice>
        </xs:complexType>
        </xs:element>
        <!-- Choice as part of a larger sequence -->
        <xs:choice>
          <xs:element name=""Opt3"" type=""xs:int""/>
          <xs:element name=""Opt4"" type=""xs:int""/>
        </xs:choice>
      </xs:sequence>
    </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("Opt1Specified", content);
        Assert.Contains("Opt2Specified", content);
        Assert.Contains("Opt3Specified", content);
        Assert.Contains("Opt4Specified", content);
    }

    [Fact]
    public void NestedElementInChoiceIsNullable()
    {
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Root"">
    <xs:complexType>
      <xs:choice>
        <xs:sequence>
          <xs:element name=""ElementA"" type=""xs:int""/>
        </xs:sequence>
        <xs:group ref=""Group""/>
      </xs:choice>
    </xs:complexType>
  </xs:element>

  <xs:group name=""Group"">
    <xs:sequence>
      <xs:element name=""ElementB"" type=""xs:int""/>
    </xs:sequence>
  </xs:group>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(NestedElementInChoiceIsNullable), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("ElementASpecified", content);
        Assert.Contains("ElementBSpecified", content);
    }

    [Fact]
    public void OnlyFirstElementOfNestedElementsIsForcedToNullableInChoice()
    {
        // Because nullability isn't directly exposed in the generated C#, we use the "RequiredAttribute"
        // as a proxy.
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Root"">
    <xs:complexType>
      <xs:choice>
        <xs:element name=""ElementWithChild"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""NestedChild"" type=""xs:int""/>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(OnlyFirstElementOfNestedElementsIsForcedToNullableInChoice), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(OnlyFirstElementOfNestedElementsIsForcedToNullableInChoice), contents);

        var elementWithChildProperty = assembly.GetType("Test.Root")?.GetProperty("ElementWithChild");
        var nestedChildProperty = assembly.GetType("Test.RootElementWithChild")?.GetProperty("NestedChild");
        Assert.NotNull(elementWithChildProperty);
        Assert.NotNull(nestedChildProperty);

        Type requiredType = typeof(System.ComponentModel.DataAnnotations.RequiredAttribute);
        bool elementWithChildIsRequired = Attribute.GetCustomAttribute(elementWithChildProperty, requiredType) != null;
        bool nestedChildIsRequired = Attribute.GetCustomAttribute(nestedChildProperty, requiredType) != null;
        Assert.False(elementWithChildIsRequired);
        Assert.True(nestedChildIsRequired);
    }

    [Fact]
    public void AssemblyVisibleIsInternalClass()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""Root"">
      <xs:sequence>
      <!-- Choice directly inside a complex type -->
      <xs:element name=""Sub"">
        <xs:complexType>
          <xs:choice>
              <xs:element name=""Opt1"" type=""xs:int""/>
              <xs:element name=""Opt2"" type=""xs:int""/>
          </xs:choice>
        </xs:complexType>
        </xs:element>
        <!-- Choice as part of a larger sequence -->
        <xs:choice>
          <xs:element name=""Opt3"" type=""xs:int""/>
          <xs:element name=""Opt4"" type=""xs:int""/>
        </xs:choice>
      </xs:sequence>
    </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("internal partial class RootSub", content);
        Assert.Contains("internal partial class Root", content);
    }

    [Fact]
    public void AssemblyVisibleIsInternalEnum()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:simpleType name=""Answer"">
    <xs:restriction base=""xs:string"">
      <xs:enumeration value=""Yes""/>
      <xs:enumeration value=""No""/>
      <xs:enumeration value=""Probably""/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("internal enum Answer", content);
    }

    [Fact]
    public void AssemblyVisibleIsInternalInterface()
    {
        // We test to see whether choices which are part of a larger ComplexType are marked as nullable.
        // Because nullability isn't directly exposed in the generated C#, we use "XXXSpecified" on a value type
        // as a proxy.

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:tns=""http://test.test/schema/AssemblyVisibleIsInternalInterface"" targetNamespace=""http://test.test/schema/AssemblyVisibleIsInternalInterface"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attributeGroup ref=""tns:NamedElement""/>
  </xs:complexType>
  <xs:attributeGroup name=""NamedElement"">
    <xs:attribute name=""Name"" use=""required"" type=""xs:string"" />
  </xs:attributeGroup>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("internal partial interface INamedElement", content);
    }

    [Fact]
    public void DecimalSeparatorTest()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/101

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attribute name=""SomeAttr"" type=""xs:decimal"" default=""1.5"" />
  </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("private decimal _someAttr = 1.5m;", content);
    }

    [Fact]
    public void DoNotGenerateIntermediaryClassForArrayElements()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/32

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema version=""1.0"" targetNamespace=""test""
  elementFormDefault=""qualified""
  xmlns:test=""test""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""foo"" type=""test:foo""/>
<xs:complexType name=""foo"">
       <xs:sequence>
        <xs:element name=""bar"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""baz"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded"">
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
</xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateComplexTypesForCollections = false,
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(DoNotGenerateIntermediaryClassForArrayElements), content);

        var fooType = assembly.DefinedTypes.Single(t => t.FullName == "Test.Foo");
        Assert.NotNull(fooType);
        Assert.Equal("Test.Foo", fooType.FullName);
    }

    [Fact]
    public void GenerateIntermediaryClassForArrayElements()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/32

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema version=""1.0"" targetNamespace=""test""
  elementFormDefault=""qualified""
  xmlns:test=""test""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""foo"" type=""test:foo""/>
<xs:complexType name=""foo"">
       <xs:sequence>
        <xs:element name=""bar"" minOccurs=""0"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""baz"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded"">
              </xs:element>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
</xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateComplexTypesForCollections = true,
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(GenerateIntermediaryClassForArrayElements), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(GenerateIntermediaryClassForArrayElements), content);

        Assert.Single(assembly.DefinedTypes, x => x.FullName == "Test.Foo");
        Assert.Single(assembly.DefinedTypes, x => x.FullName == "Test.FooBar");
    }

    [Fact]
    public void BoolTest()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/103

        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
  <xs:complexType name=""NamedType"">
    <xs:attribute name=""b0"" type=""xs:boolean"" default=""0"" />
    <xs:attribute name=""b1"" type=""xs:boolean"" default=""1"" />
    <xs:attribute name=""bf"" type=""xs:boolean"" default=""false"" />
    <xs:attribute name=""bt"" type=""xs:boolean"" default=""true"" />
  </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(ComplexTypeWithAttributeGroupExtension), xsd, generator);
        var content = Assert.Single(contents);

        Assert.Contains("private bool _b0 = false;", content);
        Assert.Contains("private bool _b1 = true;", content);
        Assert.Contains("private bool _bf = false;", content);
        Assert.Contains("private bool _bt = true;", content);
    }

    private static void CompareOutput(string expected, string actual)
    {
        static string Normalize(string input) => Regex.Replace(input, @"[ \t]*\r?\n", "\n");
        Assert.Equal(Normalize(expected), Normalize(actual));
    }

    [Theory]
    [InlineData(typeof(decimal), "decimal")]
    [InlineData(typeof(long), "long")]
    [InlineData(null, "string")]
    public void UnmappedIntegerDerivedTypesAreMappedToExpectedCSharpType(Type integerDataType, string expectedTypeName)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""root"">
        <xs:sequence>
	        <xs:element name=""unboundedInteger01"" type=""xs:integer""/>
	        <xs:element name=""unboundedInteger02"" type=""xs:nonNegativeInteger""/>
	        <xs:element name=""unboundedInteger03"" type=""xs:positiveInteger""/>
	        <xs:element name=""unboundedInteger04"" type=""xs:nonPositiveInteger""/>
	        <xs:element name=""unboundedInteger05"" type=""xs:negativeInteger""/>

	        <xs:element name=""outOfBoundsInteger01"" type=""tooLongPositiveInteger""/>
	        <xs:element name=""outOfBoundsInteger02"" type=""tooLongNonNegativeInteger""/>
	        <xs:element name=""outOfBoundsInteger03"" type=""tooLongInteger""/>
	        <xs:element name=""outOfBoundsInteger04"" type=""tooLongNegativeInteger""/>
	        <xs:element name=""outOfBoundsInteger05"" type=""tooLongNonPositiveInteger""/>
        </xs:sequence>
	</xs:complexType>

    <xs:simpleType name=""tooLongPositiveInteger"">
	    <xs:restriction base=""xs:positiveInteger"">
		    <xs:totalDigits value=""30""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNonNegativeInteger"">
	    <xs:restriction base=""xs:nonNegativeInteger"">
		    <xs:totalDigits value=""30""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongInteger"">
	    <xs:restriction base=""xs:integer"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNegativeInteger"">
	    <xs:restriction base=""xs:negativeInteger"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""tooLongNonPositiveInteger"">
	    <xs:restriction base=""xs:nonPositiveInteger"">
		    <xs:totalDigits value=""29""/>
	    </xs:restriction>
    </xs:simpleType>
</xs:schema>";

        var generatedType = ConvertXml(nameof(UnmappedIntegerDerivedTypesAreMappedToExpectedCSharpType), xsd, new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateNullables = true,
            NamingScheme = NamingScheme.PascalCase,
            IntegerDataType = integerDataType,
        }).First();

        Assert.Contains($"public {expectedTypeName} UnboundedInteger01", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger02", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger03", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger04", generatedType);
        Assert.Contains($"public {expectedTypeName} UnboundedInteger05", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger01", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger02", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger03", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger04", generatedType);
        Assert.Contains($"public {expectedTypeName} OutOfBoundsInteger05", generatedType);
    }

    [Theory]
    [InlineData("xs:positiveInteger", 1, 2, "byte")]
    [InlineData("xs:nonNegativeInteger", 1, 2, "byte")]
    [InlineData("xs:integer", 1, 2, "sbyte")]
    [InlineData("xs:negativeInteger", 1, 2, "sbyte")]
    [InlineData("xs:nonPositiveInteger", 1, 2, "sbyte")]
    [InlineData("xs:positiveInteger", 3, 4, "ushort")]
    [InlineData("xs:nonNegativeInteger", 3, 4, "ushort")]
    [InlineData("xs:integer", 3, 4, "short")]
    [InlineData("xs:negativeInteger", 3, 4, "short")]
    [InlineData("xs:nonPositiveInteger", 3, 4, "short")]
    [InlineData("xs:positiveInteger", 5, 9, "uint")]
    [InlineData("xs:nonNegativeInteger", 5, 9, "uint")]
    [InlineData("xs:integer", 5, 9, "int")]
    [InlineData("xs:negativeInteger", 5, 9, "int")]
    [InlineData("xs:nonPositiveInteger", 5, 9, "int")]
    [InlineData("xs:positiveInteger", 10, 19, "ulong")]
    [InlineData("xs:nonNegativeInteger", 10, 19, "ulong")]
    [InlineData("xs:integer", 10, 18, "long")]
    [InlineData("xs:negativeInteger", 10, 18, "long")]
    [InlineData("xs:nonPositiveInteger", 10, 18, "long")]
    [InlineData("xs:positiveInteger", 20, 29, "decimal")]
    [InlineData("xs:nonNegativeInteger", 20, 29, "decimal")]
    [InlineData("xs:integer", 20, 28, "decimal")]
    [InlineData("xs:negativeInteger", 20, 28, "decimal")]
    [InlineData("xs:nonPositiveInteger", 20, 28, "decimal")]
    public void RestrictedIntegerDerivedTypesAreMappedToExpectedCSharpTypes(string restrictionBase, int totalDigitsRangeFrom, int totalDigitsRangeTo, string expectedTypeName)
    {
        const string xsdTemplate = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""root"">
        <xs:sequence>
	        {0}
        </xs:sequence>
	</xs:complexType>

    {1}
</xs:schema>";

        const string elementTemplate = @"<xs:element name=""restrictedInteger{0}"" type=""RestrictedInteger{0}""/>";

        const string simpleTypeTemplate = @"
<xs:simpleType name=""RestrictedInteger{1}"">
	<xs:restriction base=""{0}"">
		<xs:totalDigits value=""{1}""/>
	</xs:restriction>
</xs:simpleType>
";

        string elementDefinitions = "", simpleTypeDefinitions = "";
        for (var i = totalDigitsRangeFrom; i <= totalDigitsRangeTo; i++)
        {
            elementDefinitions += string.Format(elementTemplate, i);
            simpleTypeDefinitions += string.Format(simpleTypeTemplate, restrictionBase, i);
        }

        var xsd = string.Format(xsdTemplate, elementDefinitions, simpleTypeDefinitions);
        var generatedType = ConvertXml(nameof(RestrictedIntegerDerivedTypesAreMappedToExpectedCSharpTypes), xsd,
            new Generator
            {
                NamespaceProvider = new NamespaceProvider
                {
                    GenerateNamespace = key => "Test",
                },
                GenerateNullables = true,
                NamingScheme = NamingScheme.PascalCase,
            }).First();

        for (var i = totalDigitsRangeFrom; i <= totalDigitsRangeTo; i++)
        {
            Assert.Contains($"public {expectedTypeName} RestrictedInteger{i}", generatedType);
        }
    }

    [Fact]
    public void EnumWithNonUniqueEntriesTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
				<xs:simpleType name=""TestEnum"">
					<xs:restriction base=""xs:string"">
					    <xs:enumeration value=""test_case""/>
					    <xs:enumeration value=""test_Case""/>
					    <xs:enumeration value=""Test_case""/>
					    <xs:enumeration value=""Test_Case""/>
					</xs:restriction>
				</xs:simpleType>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(EnumWithNonUniqueEntriesTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(EnumWithNonUniqueEntriesTest), content);
        var durationEnumType = assembly.GetType("Test.TestEnum");
        Assert.NotNull(durationEnumType);

        var expectedEnumValues = new[] {"TestCase", "TestCase1", "TestCase2", "TestCase3"};
        var enumValues = durationEnumType.GetEnumNames().OrderBy(n => n).ToList();
        Assert.Equal(expectedEnumValues, enumValues);

        var mEnumValue = durationEnumType.GetMembers().First(mi => mi.Name == "TestCase1");
        var xmlEnumAttribute = mEnumValue.GetCustomAttributes<XmlEnumAttribute>().FirstOrDefault();
        Assert.NotNull(xmlEnumAttribute);
        Assert.Equal("test_Case", xmlEnumAttribute.Name);
    }

    [Fact]
    public void RenameInterfacePropertyInDerivedClassTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

                <xs:complexType name=""ClassItemBase"">
			        <xs:sequence>
                        <xs:group ref=""Level1""/>
                    </xs:sequence>
		        </xs:complexType>

	            <xs:element name=""ClassItem"">
		            <xs:complexType>
			            <xs:complexContent>
                            <xs:extension base=""ClassItemBase""/>
		                </xs:complexContent>
		            </xs:complexType>
                </xs:element>

                <xs:element name=""SomeType1"">
		            <xs:complexType>
			            <xs:group ref=""Level1""/>
		            </xs:complexType>
                </xs:element>

	            <xs:group name=""Level1"">
		            <xs:choice>
                        <xs:group ref=""Level2""/>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level2"">
		            <xs:choice>
			            <xs:group ref=""Level3""/>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level3"">
		            <xs:choice>
			            <xs:element name=""ClassItemBase"" type=""xs:string""/>
		            </xs:choice>
	            </xs:group>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(RenameInterfacePropertyInDerivedClassTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(RenameInterfacePropertyInDerivedClassTest), content);
        var classType = assembly.GetType("Test.ClassItem");
        Assert.NotNull(classType);
        Assert.Single(classType.GetProperties());
        Assert.Equal("ClassItemBaseProperty", classType.GetProperties().First().Name);

        var level1Interface = assembly.GetType("Test.ILevel1");
        Assert.NotNull(level1Interface);
        Assert.Empty(level1Interface.GetProperties());

        var level2Interface = assembly.GetType("Test.ILevel1");
        Assert.NotNull(level2Interface);
        Assert.Empty(level2Interface.GetProperties());

        var level3Interface = assembly.GetType("Test.ILevel3");
        Assert.NotNull(level3Interface);
        Assert.Single(level3Interface.GetProperties());
        Assert.Equal("ClassItemBaseProperty", level3Interface.GetProperties().First().Name);
    }

    [Fact]
    public void RefTypesGetNoXmlElementAttributeTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema xmlns=""SampleNamespace"" targetNamespace=""SampleNamespace"" version=""1.0"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""SampleRoot"">
    <xs:complexType>
      <xs:sequence>
        <xs:element name=""Direct"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""Direct1"" type=""xs:string"" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
        <xs:element ref=""ViaRef"" />
      </xs:sequence>
    </xs:complexType>
  </xs:element>
  <xs:element name=""ViaRef"">
    <xs:complexType>
      <xs:sequence>
        <xs:element name=""ViaRef1"" type=""xs:string"" />
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(RefTypesGetNoXmlElementAttributeTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(RefTypesGetNoXmlElementAttributeTest), content);
        var classType = assembly.GetType("Test.SampleRoot");
        Assert.NotNull(classType);

        var directProperty = Assert.Single(classType.GetProperties(), p => p.Name == "Direct");
        Assert.Equal(XmlSchemaForm.Unqualified, directProperty.GetCustomAttributes<XmlElementAttribute>().FirstOrDefault()?.Form);

        var viaRefProperty = Assert.Single(classType.GetProperties(), p => p.Name == "ViaRef");
        Assert.Equal(XmlSchemaForm.None, viaRefProperty.GetCustomAttributes<XmlElementAttribute>().FirstOrDefault()?.Form);
    }

    [Fact]
    public void DoNotGenerateSamePropertiesInDerivedInterfacesClassTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:element name=""ParentClass"">
		            <xs:complexType>
			            <xs:group ref=""Level1""/>
		            </xs:complexType>
                </xs:element>

	            <xs:group name=""Level1"">
		            <xs:choice>
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:group ref=""Level2""/>
                    </xs:sequence>
		            </xs:choice>
	            </xs:group>

	            <xs:group name=""Level2"">
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:group ref=""Level3""/>
                    </xs:sequence>
	            </xs:group>

	            <xs:group name=""Level22"">
                    <xs:sequence>
                        <xs:element name=""InterfaceProperty"" type=""xs:string""/>
                        <xs:element name=""Level22OwnProperty"" type=""xs:string""/>
                        <xs:group ref=""Level3""/>
                    </xs:sequence>
	            </xs:group>

	            <xs:group name=""Level3"">
		            <xs:choice>
			            <xs:element name=""InterfaceProperty"" type=""xs:string""/>
		            </xs:choice>
	            </xs:group>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateInterfaces = true,
            AssemblyVisible = true
        };
        var contents = ConvertXml(nameof(DoNotGenerateSamePropertiesInDerivedInterfacesClassTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(DoNotGenerateSamePropertiesInDerivedInterfacesClassTest), content);

        var listType = assembly.GetType("Test.ParentClass");
        Assert.NotNull(listType);

        var listTypePropertyInfo = listType.GetProperties().FirstOrDefault(p => p.Name == "InterfaceProperty");
        Assert.NotNull(listTypePropertyInfo);

        var level1Interface = assembly.GetType("Test.ILevel1");
        Assert.NotNull(level1Interface);
        Assert.Empty(level1Interface.GetProperties());

        var level2Interface = assembly.GetType("Test.ILevel2");
        Assert.NotNull(level2Interface);
        Assert.Empty(level2Interface.GetProperties());

        var level3Interface = assembly.GetType("Test.ILevel3");
        Assert.NotNull(level3Interface);
        var level3InterfacePropertyInfo = level3Interface.GetProperties().FirstOrDefault(p => p.Name == "InterfaceProperty");
        Assert.NotNull(level3InterfacePropertyInfo);

    }

    [Fact]
    public void NillableWithDefaultValueTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

                <xs:complexType name=""TestType"">
                    <xs:sequence>
                        <xs:element name=""IntProperty"" type=""xs:int"" nillable=""true"" default=""9000"" />
                    </xs:sequence>
                </xs:complexType>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(NillableWithDefaultValueTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(NillableWithDefaultValueTest), content);

        var testType = assembly.GetType("Test.TestType");
        Assert.NotNull(testType);

        var propertyInfo = testType.GetProperties().FirstOrDefault(p => p.Name == "IntProperty");
        Assert.NotNull(propertyInfo);
        var testTypeInstance = Activator.CreateInstance(testType);
        var propertyDefaultValue = propertyInfo.GetValue(testTypeInstance);
        Assert.IsType<int>(propertyDefaultValue);
        Assert.Equal(9000, propertyDefaultValue);

        propertyInfo.SetValue(testTypeInstance, null);
        var serializer = new XmlSerializer(testType);

        var serializedXml = SharedTestFunctions.Serialize(serializer, testTypeInstance);
        Assert.Contains(
            @":nil=""true""",
            serializedXml);
    }

    [Fact]
    public void GenerateXmlRootAttributeForEnumTest()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://test.namespace""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var contents = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd, generator);
        var content = Assert.Single(contents);

        var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), content);

        var testType = assembly.GetType("Test.EnumTestType");
        Assert.NotNull(testType);
        var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
        Assert.Equal("http://test.namespace", xmlRootAttribute.Namespace);
    }

    [Fact]
    public void AmbiguousTypesTest()
    {
        const string xsd1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";
        const string xsd2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS2""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

		        <xs:element name=""EnumTestType"">
		            <xs:simpleType>
					    <xs:restriction base=""xs:string"">
						    <xs:enumeration value=""EnumValue""/>
					    </xs:restriction>
				    </xs:simpleType>
	            </xs:element>

            </xs:schema>";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key =>key.XmlSchemaNamespace
            }
        };
        var contents1 = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd1, generator);
        var contents2 = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), xsd2, generator);
        var content1 = Assert.Single(contents1);
        var content2 = Assert.Single(contents2);

        var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), content1, content2);

        var testType = assembly.GetType("Test_NS1.EnumTestType");
        Assert.NotNull(testType);
        var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);

        testType = assembly.GetType("Test_NS2.EnumTestType");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("EnumTestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
    }

    [Fact]
    public void AmbiguousAnonymousTypesTest()
    {
        const string xsd1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:complexType name=""TestType"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:simpleType>
					            <xs:restriction base=""xs:string"">
						            <xs:enumeration value=""EnumValue""/>
					            </xs:restriction>
				            </xs:simpleType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

	            <xs:complexType name=""TestType2"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:complexType>
					            <xs:sequence>
			                        <xs:element name=""Property"" type=""xs:string""/>
                                </xs:sequence>
				            </xs:complexType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

            </xs:schema>";
        const string xsd2 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS2""
                elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">

	            <xs:complexType name=""TestType"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:simpleType>
					            <xs:restriction base=""xs:string"">
						            <xs:enumeration value=""EnumValue""/>
					            </xs:restriction>
				            </xs:simpleType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

	            <xs:complexType name=""TestType2"">
		            <xs:sequence>
			            <xs:element name=""Property"">
				            <xs:complexType>
					            <xs:sequence>
			                        <xs:element name=""Property"" type=""xs:string""/>
                                </xs:sequence>
				            </xs:complexType>
			            </xs:element>
					</xs:sequence>
	            </xs:complexType>

            </xs:schema>";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key =>key.XmlSchemaNamespace
            }
        };
        var contents = ConvertXml(nameof(GenerateXmlRootAttributeForEnumTest), [xsd1, xsd2], generator).ToArray();
        Assert.Equal(2, contents.Length);

        var assembly = Compiler.Compile(nameof(GenerateXmlRootAttributeForEnumTest), contents);

        var testType = assembly.GetType("Test_NS1.TestType");
        Assert.NotNull(testType);
        var xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS1.TestTypeProperty");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestTypeProperty", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS1.TestType2Property");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType2Property", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS1", xmlRootAttribute.Namespace);


        testType = assembly.GetType("Test_NS2.TestType");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS2.TestTypeProperty");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestTypeProperty", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
        testType = assembly.GetType("Test_NS2.TestType2Property");
        Assert.NotNull(testType);
        xmlRootAttribute = testType.GetCustomAttributes<XmlRootAttribute>().FirstOrDefault();
        Assert.NotNull(xmlRootAttribute);
        Assert.Equal("TestType2Property", xmlRootAttribute.ElementName);
        Assert.Equal("Test_NS2", xmlRootAttribute.Namespace);
    }

    [Fact]
    public void TestShouldPatternForCollections()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:choice maxOccurs=""unbounded"">
						<xs:element name=""DateValue"" type=""xs:dateTime"" nillable=""true""/>
					</xs:choice>
				</xs:complexType>
            </xs:element>
            <xs:element name=""TestType2"">
				<xs:complexType>
					<xs:choice maxOccurs=""unbounded"">
						<xs:element name=""StringValue"" type=""xs:string"" nillable=""true""/>
					</xs:choice>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace
            }
        };

        var contents = ConvertXml(nameof(TestShouldPatternForCollections), xsd, generator).ToArray();
        Assert.Single(contents);
        var assembly = Compiler.Compile(nameof(TestShouldPatternForCollections), contents);
        var testType = assembly.GetType("Test_NS1.TestType");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);
    }


    [Fact]
    public void TestDoNotForceIsNullableGeneration()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:sequence>
						<xs:element name=""StringProperty"" type=""xs:string"" nillable=""true"" minOccurs=""0""/>
						<xs:element name=""StringNullableProperty"" type=""xs:string"" nillable=""true""/>
						<xs:element name=""StringNullableProperty2"" type=""xs:string"" nillable=""true"" minOccurs=""1""/>
					</xs:sequence>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace
            },
            DoNotForceIsNullable = true
        };

        var contents = ConvertXml(nameof(TestDoNotForceIsNullableGeneration), xsd, generator).ToArray();
        Assert.Single(contents);
        var assembly = Compiler.Compile(nameof(TestDoNotForceIsNullableGeneration), contents);
        var testType = assembly.GetType("Test_NS1.TestType");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);

        var prop = testType.GetProperty("StringProperty");
        Assert.NotNull(prop);
        var xmlElementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.False(xmlElementAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty2");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute2 = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute2.IsNullable);
    }

    [Fact]
    public void TestForceIsNullableGeneration()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
            <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""Test_NS1""
            elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
            <xs:element name=""TestType"">
				<xs:complexType>
					<xs:sequence>
						<xs:element name=""StringProperty"" type=""xs:string"" nillable=""true"" minOccurs=""0""/>
						<xs:element name=""StringNullableProperty"" type=""xs:string"" nillable=""true""/>
						<xs:element name=""StringNullableProperty2"" type=""xs:string"" nillable=""true"" minOccurs=""1""/>
					</xs:sequence>
				</xs:complexType>
            </xs:element>
            </xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => key.XmlSchemaNamespace
            },
            DoNotForceIsNullable = false
        };

        var contents = ConvertXml(nameof(TestForceIsNullableGeneration), xsd, generator).ToArray();
        Assert.Single(contents);
        var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
        var testType = assembly.GetType("Test_NS1.TestType");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);

        var prop = testType.GetProperty("StringProperty");
        Assert.NotNull(prop);
        var xmlElementAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute.IsNullable);

        prop = testType.GetProperty("StringNullableProperty2");
        Assert.NotNull(prop);
        var xmlElementNullableAttribute2 = prop.GetCustomAttribute<XmlElementAttribute>();
        Assert.True(xmlElementNullableAttribute2.IsNullable);
    }

    [Fact]
    public void TestArrayOfMsTypeGeneration()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/214

        var xsd0 =
            @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" targetNamespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"">
            <xs:complexType name=""ArrayOfstring"">
                <xs:sequence>
	                <xs:element name=""string"" type=""xs:string"" nillable=""true"" minOccurs=""0"" maxOccurs=""unbounded""/>
                </xs:sequence>
            </xs:complexType>
            <xs:element name=""ArrayOfstring"" type=""tns:ArrayOfstring"" nillable=""true""/>
        </xs:schema>
        ";
        var xsd1 =
            @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:q1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"" elementFormDefault=""qualified"">
            <xs:import namespace=""http://schemas.microsoft.com/2003/10/Serialization/Arrays""/>
            <xs:complexType name=""c_ai"">
                <xs:sequence>
	                <xs:element name=""d"" type=""q1:ArrayOfstring"" nillable=""true"" minOccurs=""0"">
		                <xs:annotation>
			                <xs:appinfo>
				                <DefaultValue EmitDefaultValue=""false"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/""/>
			                </xs:appinfo>
		                </xs:annotation>
	                </xs:element>
                </xs:sequence>
            </xs:complexType>
            <xs:element name=""c_ai"" type=""c_ai"" nillable=""true""/>
        </xs:schema>
        ";
        var validXml =
            @"<c_ai xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
            <d>
                <tns:string>String</tns:string>
                <tns:string>String</tns:string>
                <tns:string>String</tns:string>
            </d>
        </c_ai>
        ";
        var generator = new Generator
        {
            IntegerDataType = typeof(int),
            NamespacePrefix = "TestNS1",
            GenerateNullables = true,
            CollectionType = typeof(System.Collections.Generic.List<>)
        };
        var contents = ConvertXml(nameof(TestArrayOfMsTypeGeneration), [xsd0, xsd1], generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
        var testType = assembly.GetType("TestNS1.CAi");
        var serializer = new XmlSerializer(testType);
        Assert.NotNull(serializer);
        dynamic deserialized = serializer.Deserialize(new StringReader(validXml));
        //Assert.NotEmpty((System.Collections.IEnumerable)deserialized.D);  //<== oops
    }

    [Fact]
    public void TestArrayOfStringsWhenPublicAndNull()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/282

        // arrange
        var xsd =
            @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
                    <xs:complexType name=""ArrayOfstring"">
                        <xs:sequence>
                             <xs:element name=""testString"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded""/>
                        </xs:sequence>
                    </xs:complexType>
                </xs:schema>
                ";
        var validXml =
            @"<ArrayOfstring xmlns:tns=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
                </ArrayOfstring>
                ";
        var generator = new Generator
        {
            IntegerDataType = typeof(int),
            NamespacePrefix = "Test_NS1",
            GenerateNullables = true,
            CollectionType = typeof(System.Array),
            CollectionSettersMode = CollectionSettersMode.Public
        };
        var contents = ConvertXml(nameof(TestArrayOfStringsWhenPublicAndNull), [xsd], generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestForceIsNullableGeneration), contents);
        var testType = assembly.GetType("Test_NS1.ArrayOfstring");
        Assert.NotNull(testType);
        var serializer = new XmlSerializer(testType);

        // act
        dynamic deserialized = serializer.Deserialize(new StringReader(validXml));
        var xml = SharedTestFunctions.Serialize(serializer, deserialized);

        // assert
        Assert.NotNull(xml);
    }

    [Fact, TestPriority(1)]
    public void AirspaceServicesTest1()
    {
        var outputPath = Path.Combine("output", "aixm");

        string xlink = "http://www.w3.org/1999/xlink";
        string gml3 = "http://www.opengis.net/gml/3.2";
        string gts = "http://www.isotc211.org/2005/gts";
        string gss = "http://www.isotc211.org/2005/gss";
        string gsr = "http://www.isotc211.org/2005/gsr";
        string gmd = "http://www.isotc211.org/2005/gmd";
        string gco = "http://www.isotc211.org/2005/gco";

        string fixmBase = "http://www.fixm.aero/base/4.1";
        string fixmFlight = "http://www.fixm.aero/flight/4.1";
        string fixmNm = "http://www.fixm.aero/nm/1.2";
        string fixmMessaging = "http://www.fixm.aero/messaging/4.1";

        string adr = "http://www.aixm.aero/schema/5.1.1/extensions/EUR/ADR";
        string aixmV511 = "http://www.aixm.aero/schema/5.1.1";

        string adrmessage = "http://www.eurocontrol.int/cfmu/b2b/ADRMessage";

        var _xsdToCsharpNsMap = new Dictionary<NamespaceKey, string>
        {
            { new NamespaceKey(), "other" },
            { new NamespaceKey(xlink), "org.w3._1999.xlink" },
            { new NamespaceKey(gts), "org.isotc211._2005.gts" },
            { new NamespaceKey(gss), "org.isotc211._2005.gss" },
            { new NamespaceKey(gsr), "org.isotc211._2005.gsr" },
            { new NamespaceKey(gmd), "org.isotc211._2005.gmd" },
            { new NamespaceKey(gco), "org.isotc211._2005.gco" },
            { new NamespaceKey(gml3), "net.opengis.gml._3" },
            { new NamespaceKey(aixmV511), "aero.aixm.v5_1_1" },
            { new NamespaceKey(fixmNm), "aero.fixm.v4_1_0.nm.v1_2" },
            { new NamespaceKey(fixmMessaging), "aero.fixm.v4_1_0.messaging" },
            { new NamespaceKey(fixmFlight), "aero.fixm.v4_1_0.flight" },
            { new NamespaceKey(fixmBase), "aero.fixm.v4_1_0.base" },
            { new NamespaceKey(adr), "aero.aixm.schema._5_1_1.extensions.eur.adr" },
            { new NamespaceKey(adrmessage), "_int.eurocontrol.cfmu.b2b.adrmessage" }
        };

        var gen = new Generator
        {
            OutputFolder = outputPath,
            NamespaceProvider = _xsdToCsharpNsMap.ToNamespaceProvider(),
            CollectionSettersMode = CollectionSettersMode.Public,
            SeparateSubstitutes = true,
            GenerateInterfaces = false,
            EmitOrder = true
        };
        var xsdFiles = new[]
        {
            "AIXM_AbstractGML_ObjectTypes.xsd",
            "AIXM_DataTypes.xsd",
            "AIXM_Features.xsd",
            Path.Combine("extensions", "ADR-23.5.0", "ADR_DataTypes.xsd"),
            Path.Combine("extensions", "ADR-23.5.0", "ADR_Features.xsd"),
            Path.Combine("message", "ADR_Message.xsd"),
            Path.Combine("message", "AIXM_BasicMessage.xsd"),
        }.Select(x => Path.Combine(Directory.GetCurrentDirectory(), "xsd", "aixm", "aixm-5.1.1", x)).ToList();

        var assembly = Compiler.GenerateFiles("Aixm", xsdFiles, gen);
        Assert.NotNull(assembly);

        /*
        var testFiles = new Dictionary<string, string>
        {
            { "airport1.xml", "AirportHeliportType" },
            { "airportHeliportTimeSlice.xml", "AirportHeliportTimeSliceType" },
            { "airspace1.xml", "AirspaceType" },
            { "navaid1.xml", "NavaidType" },
            { "navaidTimeSlice.xml", "NavaidTimeSliceType" },
            { "navaidWithAbstractTime.xml", "NavaidWithAbstractTime" },
            { "navaid.xml", "Navaid" },
            { "routesegment1.xml", "RouteSegment" },
            { "timePeriod.xml", "TimePeriod" },
        };

        foreach (var testFile in testFiles)
        {
            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var xmlString = File.ReadAllText($"xml/aixm_tests/{testFile.Key}");
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = Serialize(serializer, deserializedObject, GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
        */
    }

    [Fact, TestPriority(1)]
    [UseCulture("en-US")]
    public void TestNullableReferenceAttributes()
    {
        var files = Glob.ExpandNames(NullableReferenceAttributesPattern).OrderByDescending(f => f);
        var generator = new Generator
        {
            EnableNullableReferenceAttributes = true,
            UseShouldSerializePattern = true,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            }
        };
        var assembly = Compiler.Generate(nameof(TestNullableReferenceAttributes), NullableReferenceAttributesPattern, generator);
        void assertNullable(string typename, bool nullable)
        {
            Type c = assembly.GetType(typename);
            var property = c.GetProperty("Text");
            var setParameter = property.SetMethod.GetParameters();
            var getReturnParameter = property.GetMethod.ReturnParameter;
            var allowNullableAttribute = setParameter.Single().CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(AllowNullAttribute));
            var maybeNullAttribute = getReturnParameter.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(MaybeNullAttribute));
            var hasAllowNullAttribute = allowNullableAttribute != null;
            var hasMaybeNullAttribute = maybeNullAttribute != null;
            Assert.Equal(nullable, hasAllowNullAttribute);
            Assert.Equal(nullable, hasMaybeNullAttribute);
        }
        assertNullable("Test.ElementReferenceNullable", true);
        assertNullable("Test.ElementReferenceList", false);
        assertNullable("Test.ElementReferenceNonNullable", false);
        assertNullable("Test.AttributeReferenceNullable", true);
        assertNullable("Test.AttributeReferenceNonNullable", false);
        assertNullable("Test.AttributeValueNullableInt", false);
    }

    [Fact, TestPriority(1)]
    public void TestNetex()
    {
        var outputPath = Path.Combine("output", "netex");

        var gen = new Generator
        {
            OutputFolder = outputPath,
            CollectionSettersMode = CollectionSettersMode.Public,
            EmitOrder = true,
            SeparateSubstitutes = true,
            GenerateInterfaces = false,
            UniqueTypeNamesAcrossNamespaces = true,
        };

        var xsdFiles = new[] { Path.Combine(Directory.GetCurrentDirectory(), "xsd", "netex", "NeTEx_publication.xsd") };

        var assembly = Compiler.GenerateFiles("Netex", xsdFiles, gen);
        Assert.NotNull(assembly);

        var testFiles = new Dictionary<string, string>
        {
            { "functions/calendar/Netex_calendarCodeing_02.xml", "PublicationDeliveryStructure" },
        };

        foreach (var testFile in testFiles)
        {
            var type = assembly.GetTypes().SingleOrDefault(t => t.Name == testFile.Value);
            Assert.NotNull(type);

            var serializer = new XmlSerializer(type);
            serializer.UnknownNode += new XmlNodeEventHandler(UnknownNodeHandler);
            serializer.UnknownAttribute += new XmlAttributeEventHandler(UnknownAttributeHandler);
            var unknownNodeError = false;
            var unknownAttrError = false;

            void UnknownNodeHandler(object sender, XmlNodeEventArgs e)
            {
                unknownNodeError = true;
            }

            void UnknownAttributeHandler(object sender, XmlAttributeEventArgs e)
            {
                unknownAttrError = true;
            }

            var xmlString = File.ReadAllText($"xml/netex_tests/{testFile.Key}");
            xmlString = Regex.Replace(xmlString, "xsi:schemaLocation=\"[^\"]*\"", string.Empty);
            var reader = XmlReader.Create(new StringReader(xmlString), new XmlReaderSettings { IgnoreWhitespace = true });

            var isDeserializable = serializer.CanDeserialize(reader);
            Assert.True(isDeserializable);

            var deserializedObject = serializer.Deserialize(reader);
            Assert.False(unknownNodeError);
            Assert.False(unknownAttrError);

            var serializedXml = SharedTestFunctions.Serialize(serializer, deserializedObject, SharedTestFunctions.GetNamespacesFromSource(xmlString));

            var deserializedXml = serializer.Deserialize(new StringReader(serializedXml));
            AssertEx.Equal(deserializedObject, deserializedXml);
        }
    }

    [Theory]
    [InlineData("fake command line arguments", "fake command line arguments")]
    [InlineData(null, "N/A")]
    public void IncludeCommandLineArguments(string commandLineArguments, string expectedCommandLineArguments)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://local.none"" xmlns:l=""http://local.none"">
	<xs:element name=""document"" type=""l:elem"" />
	<xs:complexType name=""elem"">
		<xs:attribute name=""Text"" type=""xs:string""/>
	</xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            GenerateInterfaces = false,
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            GenerateCommandLineArgumentsComment = true,
            CommandLineArgumentsProvider = new CommandLineArgumentsProvider(commandLineArguments)
        };

        var contents = ConvertXml(nameof(IncludeCommandLineArguments), xsd, generator);

        var csharp = Assert.Single(contents);

        CompareOutput(
            $@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// This code was generated by Tests version 1.0.0.1 using the following command:
// {expectedCommandLineArguments}
namespace Test
{{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute(""Tests"", ""1.0.0.1"")]
    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(""elem"", Namespace=""http://local.none"")]
    [System.ComponentModel.DesignerCategoryAttribute(""code"")]
    [System.Xml.Serialization.XmlRootAttribute(""document"", Namespace=""http://local.none"")]
    public partial class Elem
    {{

        [System.Xml.Serialization.XmlAttributeAttribute(""Text"")]
        public string Text {{ get; set; }}
    }}
}}
", csharp);
    }

    [Fact]
    public void TestArrayItemAttribute()
    {
        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/313

        var xsd =
@"<?xml version=""1.0"" encoding=""UTF-8""?>

<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema""
	 xmlns=""test_generation_namespace/common.xsd""
	 xmlns:ct=""test_generation_namespace/commontypes.xsd""
	 targetNamespace=""test_generation_namespace/common.xsd""
	 version=""1.1""
	 elementFormDefault=""qualified""
	 attributeFormDefault=""unqualified"">
	<xs:import namespace=""test_generation_namespace/commontypes.xsd"" schemaLocation=""TheCommonTypes.xsd""/>
	<xs:complexType name=""T_NameValue"">
		<xs:sequence>
			<xs:element name=""Name"" type=""xs:string""/>
			<xs:element name=""Value"" type=""xs:string"" minOccurs=""0""/>
		</xs:sequence>
	</xs:complexType>
	<xs:complexType name=""T_OptionList"">
		<xs:sequence>
			<xs:element name=""Option"" type=""T_NameValue"" minOccurs=""0"" maxOccurs=""unbounded""/>
		</xs:sequence>
	</xs:complexType>
    <xs:complexType name=""T_Application"">
		<xs:sequence>
			<xs:element name=""OptionList"" type=""T_OptionList"" minOccurs=""0""/>
		</xs:sequence>
	</xs:complexType>
</xs:schema>";
        var generator = new Generator
        {
            IntegerDataType = typeof(int),
            GenerateNullables = true,
            CollectionType = typeof(System.Array),
            CollectionSettersMode = CollectionSettersMode.Public,
            UseArrayItemAttribute = false
        };
        var contents = ConvertXml(nameof(TestArrayItemAttribute), [xsd], generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestArrayItemAttribute), contents);
        var applicationType = assembly.GetType("TestGenerationNamespace.TApplication");
        Assert.NotNull(applicationType);
        var optionList = applicationType.GetProperty("OptionList");
        Assert.Equal("TestGenerationNamespace.TOptionList", optionList.PropertyType.FullName);
    }

    [Fact]
    public void CollectionSetterInAttributeGroupInterfaceIsPrivateIfCollectionSetterModeIsPrivate()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Element"">
    <xs:complexType>
      <xs:attributeGroup ref=""AttrGroup""/>
    </xs:complexType>
  </xs:element>

  <xs:attributeGroup name=""AttrGroup"">
    <xs:attribute name=""Attr"">
      <xs:simpleType>
        <xs:list itemType=""xs:int""/>
      </xs:simpleType>
    </xs:attribute>
  </xs:attributeGroup>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            CollectionSettersMode = CollectionSettersMode.Private
        };
        var contents = ConvertXml(nameof(CollectionSetterInAttributeGroupInterfaceIsPrivateIfCollectionSetterModeIsPrivate), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(CollectionSetterInAttributeGroupInterfaceIsPrivateIfCollectionSetterModeIsPrivate), contents);

        var interfaceProperty = assembly.GetType("Test.IAttrGroup")?.GetProperty("Attr");
        var implementerProperty = assembly.GetType("Test.Element")?.GetProperty("Attr");
        Assert.NotNull(interfaceProperty);
        Assert.NotNull(implementerProperty);

        var interfaceHasPublicSetter = interfaceProperty.GetSetMethod() != null;
        var implementerHasPublicSetter = implementerProperty.GetSetMethod() != null;
        Assert.False(interfaceHasPublicSetter);
        Assert.False(implementerHasPublicSetter);
    }

    [Fact]
    public void CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""Element"">
    <xs:complexType>
      <xs:attributeGroup ref=""AttrGroup""/>
    </xs:complexType>
  </xs:element>

  <xs:attributeGroup name=""AttrGroup"">
    <xs:attribute name=""Attr"">
      <xs:simpleType>
        <xs:list itemType=""xs:int""/>
      </xs:simpleType>
    </xs:attribute>
  </xs:attributeGroup>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = true,
            CollectionSettersMode = CollectionSettersMode.Public
        };
        var contents = ConvertXml(nameof(CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic), contents);

        var interfaceProperty = assembly.GetType("Test.IAttrGroup")?.GetProperty("Attr");
        var implementerProperty = assembly.GetType("Test.Element")?.GetProperty("Attr");
        Assert.NotNull(interfaceProperty);
        Assert.NotNull(implementerProperty);

        var interfaceHasPublicSetter = interfaceProperty.GetSetMethod() != null;
        var implementerHasPublicSetter = implementerProperty.GetSetMethod() != null;
        Assert.True(interfaceHasPublicSetter);
        Assert.True(implementerHasPublicSetter);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SimpleInterface(bool generateInterface)
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:attributeGroup name=""Common"">
    <xs:attribute name=""name"" type=""xs:string""></xs:attribute>
  </xs:attributeGroup>

  <xs:complexType name=""A"">
    <xs:attributeGroup ref=""Common""/>
  </xs:complexType>

  <xs:complexType name=""B"">
    <xs:attributeGroup ref=""Common""/>
  </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test",
            },
            GenerateInterfaces = generateInterface,
        };
        var contents = ConvertXml(nameof(SimpleInterface) + $"({generateInterface})", xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(CollectionSetterInAttributeGroupInterfaceIsPublicIfCollectionSetterModeIsPublic), contents);

        var interfaceCommon = assembly.GetType("Test.ICommon");
        var typeA = assembly.GetType("Test.A");
        var typeB = assembly.GetType("Test.B");
        if(generateInterface)
        {
            Assert.True(interfaceCommon.IsInterface);
            Assert.True(interfaceCommon.IsAssignableFrom(typeA));
            Assert.True(interfaceCommon.IsAssignableFrom(typeB));
        }
        else
        {
            Assert.Null(interfaceCommon);
        }
    }


    [Fact]
    public void TestAllowDtdParse()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE schema [
	<!ENTITY lowalpha ""a-z"">
	<!ENTITY hialpha ""A-Z"">
	<!ENTITY digit ""0-9"">
]>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:simpleType name=""CodecsType"">
		<xs:annotation>
			<xs:documentation xml:lang=""en"">
				List of Profiles
			</xs:documentation>
		</xs:annotation>
		<xs:restriction base=""xs:string"">
			<xs:pattern value=""[&lowalpha;&hialpha;&digit;]+""/>
		</xs:restriction>
	</xs:simpleType>
    <xs:complexType name=""ComplexType"">
      <xs:attribute name=""codecs"" type=""CodecsType""/>
    </xs:complexType>
</xs:schema>

";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AllowDtdParse = true
        };

        var generatedType = ConvertXml(nameof(TestAllowDtdParse), xsd, generator).First();

        Assert.Contains(@"public partial class ComplexType", generatedType);
        Assert.Contains(@"[a-zA-Z0-9]+", generatedType);
    }

    [Fact]
    public void TestNotAllowDtdParse()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE schema [
	<!ENTITY lowalpha ""a-z"">
	<!ENTITY hialpha ""A-Z"">
	<!ENTITY digit ""0-9"">
]>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	<xs:simpleType name=""CodecsType"">
		<xs:annotation>
			<xs:documentation xml:lang=""en"">
				List of Profiles
			</xs:documentation>
		</xs:annotation>
		<xs:restriction base=""xs:string"">
			<xs:pattern value=""[&lowalpha;&hialpha;&digit;]+""/>
		</xs:restriction>
	</xs:simpleType>
    <xs:complexType name=""ComplexType"">
      <xs:attribute name=""codecs"" type=""CodecsType""/>
    </xs:complexType>
</xs:schema>

";
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            AllowDtdParse = false
        };

        var exception = Assert.Throws<XmlException>(() => ConvertXml(nameof(TestNotAllowDtdParse), xsd, generator));
        Assert.Contains("Reference to undeclared entity 'lowalpha'", exception.Message);
    }

    [Fact]
    public void TestOmitXmlIncludeAttribute()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:xlink=""http://www.w3.org/1999/xlink"" elementFormDefault=""qualified"" attributeFormDefault=""unqualified"">
    <xs:complexType name=""BaseType"">
        <xs:sequence>
            <xs:element name=""BaseProperty"" type=""xs:string""/>
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name=""DerivedType1"">
        <xs:complexContent>
            <xs:extension base=""BaseType"">
                <xs:sequence>
                    <xs:element name=""DerivedProperty1"" type=""xs:string""/>
                </xs:sequence>
            </xs:extension>
        </xs:complexContent>
    </xs:complexType>
    <xs:complexType name=""DerivedType2"">
        <xs:complexContent>
            <xs:extension base=""BaseType"">
                <xs:sequence>
                    <xs:element name=""DerivedProperty2"" type=""xs:string""/>
                </xs:sequence>
            </xs:extension>
        </xs:complexContent>
    </xs:complexType>
</xs:schema>";

        // Test with OmitXmlIncludeAttribute = false (default behavior)
        var generatorWithInclude = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            OmitXmlIncludeAttribute = false
        };

        var contentsWithInclude = ConvertXml(nameof(TestOmitXmlIncludeAttribute) + "WithInclude", xsd, generatorWithInclude);
        var contentWithInclude = Assert.Single(contentsWithInclude);

        // Verify XmlIncludeAttribute is present for both derived types
        Assert.Contains("[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedType1))]", contentWithInclude);
        Assert.Contains("[System.Xml.Serialization.XmlIncludeAttribute(typeof(DerivedType2))]", contentWithInclude);

        // Test with OmitXmlIncludeAttribute = true
        var generatorWithoutInclude = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test"
            },
            OmitXmlIncludeAttribute = true
        };

        var contentsWithoutInclude = ConvertXml(nameof(TestOmitXmlIncludeAttribute) + "WithoutInclude", xsd, generatorWithoutInclude);
        var contentWithoutInclude = Assert.Single(contentsWithoutInclude);

        // Verify XmlIncludeAttribute is NOT present
        Assert.DoesNotContain("XmlIncludeAttribute", contentWithoutInclude);

        // Verify that the base and derived types are still generated correctly
        Assert.Contains("public partial class BaseType", contentWithoutInclude);
        Assert.Contains("public partial class DerivedType1 : BaseType", contentWithoutInclude);
        Assert.Contains("public partial class DerivedType2 : BaseType", contentWithoutInclude);
    }

    [Fact]
    public void TestOmitXmlIncludeAttributeSerializationWithExtraTypes()
    {
        const string xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://test.example/omit"" xmlns:t=""http://test.example/omit"" elementFormDefault=""qualified"">
    <xs:element name=""Container"" type=""t:ContainerType""/>
    <xs:complexType name=""ContainerType"">
        <xs:sequence>
            <xs:element name=""Item"" type=""t:BaseType""/>
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name=""BaseType"">
        <xs:sequence>
            <xs:element name=""BaseProperty"" type=""xs:string""/>
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name=""DerivedType1"">
        <xs:complexContent>
            <xs:extension base=""t:BaseType"">
                <xs:sequence>
                    <xs:element name=""DerivedProperty1"" type=""xs:string""/>
                </xs:sequence>
            </xs:extension>
        </xs:complexContent>
    </xs:complexType>
    <xs:complexType name=""DerivedType2"">
        <xs:complexContent>
            <xs:extension base=""t:BaseType"">
                <xs:sequence>
                    <xs:element name=""DerivedProperty2"" type=""xs:int""/>
                </xs:sequence>
            </xs:extension>
        </xs:complexContent>
    </xs:complexType>
</xs:schema>";

        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider
            {
                GenerateNamespace = key => "Test.Omit"
            },
            OmitXmlIncludeAttribute = true
        };

        var contents = ConvertXml(nameof(TestOmitXmlIncludeAttributeSerializationWithExtraTypes), xsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestOmitXmlIncludeAttributeSerializationWithExtraTypes), contents);

        Assert.NotNull(assembly);

        // Get the generated types
        var containerType = assembly.GetType("Test.Omit.ContainerType");
        var baseType = assembly.GetType("Test.Omit.BaseType");
        var derivedType1 = assembly.GetType("Test.Omit.DerivedType1");
        var derivedType2 = assembly.GetType("Test.Omit.DerivedType2");

        Assert.NotNull(containerType);
        Assert.NotNull(baseType);
        Assert.NotNull(derivedType1);
        Assert.NotNull(derivedType2);

        // Verify that BaseType does NOT have XmlIncludeAttribute
        var xmlIncludeAttributes = baseType.GetCustomAttributes(typeof(XmlIncludeAttribute), false);
        Assert.Empty(xmlIncludeAttributes);

        // Test serialization with extraTypes parameter for DerivedType1
        var serializer1 = new XmlSerializer(containerType, [derivedType1, derivedType2]);

        // Create an instance with DerivedType1
        var container1 = Activator.CreateInstance(containerType);
        var derived1 = Activator.CreateInstance(derivedType1);

        var basePropertyProp = baseType.GetProperty("BaseProperty");
        var derivedProperty1Prop = derivedType1.GetProperty("DerivedProperty1");

        basePropertyProp.SetValue(derived1, "Base Value 1");
        derivedProperty1Prop.SetValue(derived1, "Derived Value 1");

        var itemProp = containerType.GetProperty("Item");
        itemProp.SetValue(container1, derived1);

        // Serialize
        var sw1 = new StringWriter();
        serializer1.Serialize(sw1, container1);
        var xml1 = sw1.ToString();

        // Verify the XML contains xsi:type information
        Assert.Contains("DerivedType1", xml1);
        Assert.Contains("Base Value 1", xml1);
        Assert.Contains("Derived Value 1", xml1);

        // Deserialize back
        var sr1 = new StringReader(xml1);
        var deserialized1 = serializer1.Deserialize(sr1);

        Assert.NotNull(deserialized1);
        var deserializedItem1 = itemProp.GetValue(deserialized1);
        Assert.NotNull(deserializedItem1);
        Assert.Equal(derivedType1, deserializedItem1.GetType());
        Assert.Equal("Base Value 1", basePropertyProp.GetValue(deserializedItem1));
        Assert.Equal("Derived Value 1", derivedProperty1Prop.GetValue(deserializedItem1));

        // Test serialization with DerivedType2
        var serializer2 = new XmlSerializer(containerType, [derivedType1, derivedType2]);

        var container2 = Activator.CreateInstance(containerType);
        var derived2 = Activator.CreateInstance(derivedType2);

        var derivedProperty2Prop = derivedType2.GetProperty("DerivedProperty2");

        basePropertyProp.SetValue(derived2, "Base Value 2");
        derivedProperty2Prop.SetValue(derived2, 42);

        itemProp.SetValue(container2, derived2);

        // Serialize
        var sw2 = new StringWriter();
        serializer2.Serialize(sw2, container2);
        var xml2 = sw2.ToString();

        // Verify the XML contains xsi:type information
        Assert.Contains("DerivedType2", xml2);
        Assert.Contains("Base Value 2", xml2);
        Assert.Contains("42", xml2);

        // Deserialize back
        var sr2 = new StringReader(xml2);
        var deserialized2 = serializer2.Deserialize(sr2);

        Assert.NotNull(deserialized2);
        var deserializedItem2 = itemProp.GetValue(deserialized2);
        Assert.NotNull(deserializedItem2);
        Assert.Equal(derivedType2, deserializedItem2.GetType());
        Assert.Equal("Base Value 2", basePropertyProp.GetValue(deserializedItem2));
        Assert.Equal(42, derivedProperty2Prop.GetValue(deserializedItem2));
    }

    // -- EnableNullableDirective and GenerateRequiredModifier tests ----------------

    private const string NullableAndRequiredXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:simpleType name=""StatusEnum"">
        <xs:restriction base=""xs:string"">
            <xs:enumeration value=""Active""/>
            <xs:enumeration value=""Inactive""/>
            <xs:enumeration value=""Pending""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""RequiredString"" type=""xs:string"" minOccurs=""1""/>
            <xs:element name=""OptionalString"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""RequiredDate"" type=""xs:dateTime"" minOccurs=""1""/>
            <xs:element name=""OptionalDate"" type=""xs:dateTime"" minOccurs=""0""/>
            <xs:element name=""OptionalComplex"" type=""Child"" minOccurs=""0""/>
            <xs:element name=""Items"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded""/>
            <xs:element name=""RequiredInt"" type=""xs:int"" minOccurs=""1""/>
            <xs:element name=""RequiredBool"" type=""xs:boolean"" minOccurs=""1""/>
            <xs:element name=""RequiredStatus"" type=""StatusEnum"" minOccurs=""1""/>
            <xs:element name=""OptionalStatus"" type=""StatusEnum"" minOccurs=""0""/>
            <xs:element name=""RequiredWithDefault"" type=""xs:string"" minOccurs=""1"" default=""hello""/>
            <xs:element name=""OptionalStringWithDefault"" type=""xs:string"" minOccurs=""0"" default=""fallback""/>
        </xs:sequence>
        <xs:attribute name=""RequiredAttr"" type=""xs:string"" use=""required""/>
        <xs:attribute name=""OptionalAttr"" type=""xs:string"" use=""optional""/>
    </xs:complexType>
    <xs:complexType name=""Child"">
        <xs:sequence>
            <xs:element name=""Name"" type=""xs:string"" minOccurs=""1""/>
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name=""TextValue"">
        <xs:simpleContent>
            <xs:extension base=""xs:string"">
                <xs:attribute name=""Lang"" type=""xs:string"" use=""optional""/>
            </xs:extension>
        </xs:simpleContent>
    </xs:complexType>
    <xs:simpleType name=""NonEmptyString"">
        <xs:restriction base=""xs:string"">
            <xs:minLength value=""1""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:complexType name=""RequiredTextValue"">
        <xs:simpleContent>
            <xs:extension base=""NonEmptyString"">
                <xs:attribute name=""Lang"" type=""xs:string"" use=""optional""/>
            </xs:extension>
        </xs:simpleContent>
    </xs:complexType>
    <xs:complexType name=""EnumTextValue"">
        <xs:simpleContent>
            <xs:extension base=""StatusEnum"">
                <xs:attribute name=""Source"" type=""xs:string"" use=""optional""/>
            </xs:extension>
        </xs:simpleContent>
    </xs:complexType>
    <xs:complexType name=""DoubleTextValue"">
        <xs:simpleContent>
            <xs:extension base=""xs:double"">
                <xs:attribute name=""Unit"" type=""xs:string"" use=""optional""/>
            </xs:extension>
        </xs:simpleContent>
    </xs:complexType>
</xs:schema>";

    /// <summary>
    /// Extracts the body of a specific class from the generated code content.
    /// Matches from "class ClassName" to the next class declaration or end of content.
    /// </summary>
    private static string ExtractClassBlock(string content, string className)
    {
        var pattern = $@"partial class {Regex.Escape(className)}\b.*?(?=partial class |\z)";
        var match = Regex.Match(content, pattern, RegexOptions.Singleline);
        Assert.True(match.Success, $"Class '{className}' not found in generated content.");
        return match.Value;
    }

    private static Generator CreateNullableRequiredGenerator(
        bool enableNullableDirective = false,
        bool generateRequiredModifier = false,
        bool enableNullableReferenceAttributes = false)
    {
        return new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            EnableNullableReferenceAttributes = enableNullableReferenceAttributes,
            EnableNullableDirective = enableNullableDirective,
            GenerateRequiredModifier = generateRequiredModifier,
            GenerateNullables = true,
            DataAnnotationMode = DataAnnotationMode.All,
            NetCoreSpecificCode = true,
        };
    }

    [Fact]
    public void TestRequiredModifierOnRequiredElements()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierOnRequiredElements), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Required string element: gets 'required' modifier
        Assert.Contains("public required string RequiredString", content);

        // Required dateTime element: gets 'required' modifier
        Assert.Contains("public required System.DateTime RequiredDate", content);

        // Required attribute: gets 'required' modifier
        Assert.Contains("public required string RequiredAttr", content);
    }

    [Fact]
    public void TestRequiredModifierNotOnOptionalElements()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierNotOnOptionalElements), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Optional string element: no 'required' modifier
        Assert.DoesNotContain("required string OptionalString", content);
        Assert.DoesNotContain("required string OptionalAttr", content);

        // Optional complex element: no 'required' modifier
        Assert.DoesNotContain("required Test.Child OptionalComplex", content);
    }

    [Fact]
    public void TestRequiredModifierNotOnCollections()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierNotOnCollections), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Collection property: never gets 'required' modifier
        Assert.DoesNotContain("required", content.Split('\n')
            .FirstOrDefault(l => l.Contains("Items")) ?? "");
    }

    [Fact]
    public void TestXmlTextValuePropertyIsNullableWhenUnconstrained()
    {
        // TextValue extends xs:string with no minLength — text body is optional.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyIsNullableWhenUnconstrained), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var textValueBlock = ExtractClassBlock(content, "TextValue");

        // Unconstrained simpleContent: nullable, not required.
        Assert.Contains("public string? Value", textValueBlock);
        Assert.DoesNotContain("required string Value", textValueBlock);
    }

    [Fact]
    public void TestXmlTextValuePropertyIsRequiredWhenConstrainedByMinLength()
    {
        // RequiredTextValue extends NonEmptyString (minLength=1) — text body is required.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyIsRequiredWhenConstrainedByMinLength), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var requiredBlock = ExtractClassBlock(content, "RequiredTextValue");

        // Constrained simpleContent: required, not nullable.
        Assert.Contains("public required string Value", requiredBlock);
        Assert.DoesNotContain("string? Value", requiredBlock);
    }

    [Fact]
    public void TestXmlTextValuePropertyPlainStringWhenNullableDirectiveOff()
    {
        // Without EnableNullableDirective, unconstrained Value is a plain string.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: false,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyPlainStringWhenNullableDirectiveOff), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var textValueBlock = ExtractClassBlock(content, "TextValue");

        Assert.Contains("public string Value", textValueBlock);
        Assert.DoesNotContain("string? Value", textValueBlock);
        Assert.DoesNotContain("required string Value", textValueBlock);
    }

    [Fact]
    public void TestXmlTextValuePropertyRequiredWhenConstrainedAndNullableDirectiveOff()
    {
        // Constrained text value gets required even without nullable directive.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: false,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyRequiredWhenConstrainedAndNullableDirectiveOff), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var requiredBlock = ExtractClassBlock(content, "RequiredTextValue");

        Assert.Contains("public required string Value", requiredBlock);
        Assert.DoesNotContain("string? Value", requiredBlock);
    }

    [Fact]
    public void TestXmlTextValuePropertyNoBothFlagsOff()
    {
        // Both flags off: always plain string, regardless of constraints.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: false,
            generateRequiredModifier: false);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyNoBothFlagsOff), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Both TextValue and RequiredTextValue should be plain string.
        foreach (var className in new[] { "TextValue", "RequiredTextValue" })
        {
            var block = ExtractClassBlock(content, className);
            Assert.Contains("public string Value", block);
            Assert.DoesNotContain("string? Value", block);
            Assert.DoesNotContain("required string Value", block);
        }
    }

    [Fact]
    public void TestXmlTextValuePropertyEnumBaseIsNeverNullable()
    {
        // EnumTextValue extends StatusEnum — value types must NOT get '?' suffix
        // because Nullable<T> + [XmlText] causes XmlSerializer to crash.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyEnumBaseIsNeverNullable), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "EnumTextValue");

        // Enum value type: never nullable, never Nullable<T>.
        // The type may or may not be namespace-qualified depending on CodeDom output.
        Assert.Contains("StatusEnum Value", block);
        Assert.DoesNotContain("StatusEnum? Value", block);
        Assert.DoesNotContain("Nullable", block);
    }

    [Fact]
    public void TestXmlTextValuePropertyDoubleBaseIsNeverNullable()
    {
        // DoubleTextValue extends xs:double — value types must NOT get '?' suffix
        // because Nullable<T> + [XmlText] causes XmlSerializer to crash.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyDoubleBaseIsNeverNullable), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "DoubleTextValue");

        // Double value type: never nullable.
        Assert.Contains("public double Value", block);
        Assert.DoesNotContain("double? Value", block);
        Assert.DoesNotContain("Nullable", block);
    }

    [Fact]
    public void TestXmlTextValuePropertyStringBaseStillNullable()
    {
        // Verify that the value-type guard does NOT affect string (reference type) behavior.
        // TextValue extends xs:string with no minLength — should still be nullable.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestXmlTextValuePropertyStringBaseStillNullable), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "TextValue");

        // String reference type: still nullable when unconstrained.
        Assert.Contains("public string? Value", block);
        Assert.DoesNotContain("required string Value", block);
    }

    [Fact]
    public void TestNullableDirectiveUsesQuestionMarkSyntax()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);

        var contents = ConvertXml(nameof(TestNullableDirectiveUsesQuestionMarkSyntax), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Optional string element: uses '?' suffix instead of [AllowNull][MaybeNull]
        Assert.Contains("string? OptionalString", content);

        // Optional complex element: uses '?' suffix
        Assert.Contains("Child? OptionalComplex", content);

        // [AllowNull] and [MaybeNull] should NOT be present for nullable properties
        Assert.DoesNotContain("AllowNullAttribute", content);
        Assert.DoesNotContain("MaybeNullAttribute", content);
    }

    [Fact]
    public void TestNullableDirectiveOffUsesAttributes()
    {
        // When EnableNullableDirective is off but EnableNullableReferenceAttributes is on,
        // the old [AllowNull]/[MaybeNull] attributes should be used.
        var generator = CreateNullableRequiredGenerator(
            enableNullableReferenceAttributes: true);

        var contents = ConvertXml(nameof(TestNullableDirectiveOffUsesAttributes), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Should use attributes, not '?' syntax
        Assert.Contains("AllowNullAttribute", content);
        Assert.Contains("MaybeNullAttribute", content);
        Assert.DoesNotContain("System.String?", content);
    }

    [Fact]
    public void TestRequiredModifierOffDoesNotEmitRequired()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: false);

        var contents = ConvertXml(nameof(TestRequiredModifierOffDoesNotEmitRequired), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // 'required' keyword should not appear anywhere in the generated code
        Assert.DoesNotContain("required ", content);
    }

    [Fact]
    public void TestNullableDirectiveInjectsDirectiveInFileOutput()
    {
        // This test uses the file-based output pipeline to verify
        // that #nullable enable is injected into the generated file.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestNullableDirectiveInjectsDirectiveInFileOutput)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(NullableAndRequiredXsd) });

        // Read the generated files and verify #nullable enable is present
        foreach (var file in output.Files)
        {
            var fileContent = File.ReadAllText(file);
            Assert.Contains("#nullable enable", fileContent);
            // Should come after the auto-generated comment
            var directiveIndex = fileContent.IndexOf("#nullable enable", StringComparison.Ordinal);
            var autoGenIndex = fileContent.IndexOf("</auto-generated>", StringComparison.Ordinal);
            Assert.True(directiveIndex > autoGenIndex,
                $"#nullable enable should appear after </auto-generated> in {Path.GetFileName(file)}");
        }
    }

    [Fact]
    public void TestNullableDirectiveNotInjectedWhenDisabled()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: false);

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestNullableDirectiveNotInjectedWhenDisabled)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(NullableAndRequiredXsd) });

        foreach (var file in output.Files)
        {
            var fileContent = File.ReadAllText(file);
            Assert.DoesNotContain("#nullable enable", fileContent);
        }
    }

    [Fact]
    public void TestNullableCollectionUsesQuestionMarkSuffix()
    {
        // Generic collection types (e.g. Collection<string>) now correctly get the '?' suffix
        // because WrapTypeRef renders the type via CSharpCodeProvider first, then creates a
        // literal CodeTypeReference that CodeDom outputs verbatim. No attribute fallback needed.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);
        generator.CollectionSettersMode = CollectionSettersMode.Init;
        generator.CollectionType = typeof(System.Collections.ObjectModel.Collection<>);

        var contents = ConvertXml(nameof(TestNullableCollectionUsesQuestionMarkSuffix), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // The collection backing field should have '?' on its type
        var lines = content.Split('\n');
        var backingFieldLine = lines.FirstOrDefault(l => l.Contains("_items") && l.Contains("private"));
        Assert.NotNull(backingFieldLine);
        Assert.Contains("?", backingFieldLine);

        // No [AllowNull]/[MaybeNull] attribute fallback when EnableNullableDirective is on
        Assert.DoesNotContain("AllowNullAttribute", content);
        Assert.DoesNotContain("MaybeNullAttribute", content);
    }

    [Fact]
    public void TestRequiredModifierOnRequiredValueTypes()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierOnRequiredValueTypes), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Required int element: gets 'required' modifier
        Assert.Contains("public required int RequiredInt", content);

        // Required bool element: gets 'required' modifier
        Assert.Contains("public required bool RequiredBool", content);
    }

    [Fact]
    public void TestRequiredReferenceTypeNotNullable()
    {
        // When both flags are on, a required string should be 'required string',
        // NOT 'required string?' — required properties are non-nullable.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredReferenceTypeNotNullable), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Required string must NOT have '?' suffix
        Assert.Contains("public required string RequiredString", content);
        Assert.DoesNotContain("required string? RequiredString", content);

        // Required attribute must NOT have '?' suffix
        Assert.Contains("public required string RequiredAttr", content);
        Assert.DoesNotContain("required string? RequiredAttr", content);
    }

    [Fact]
    public void TestRequiredModifierWithoutNullableDirective()
    {
        // GenerateRequiredModifier works independently of EnableNullableDirective.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: false,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierWithoutNullableDirective), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // 'required' should still be emitted on required properties
        Assert.Contains("public required string RequiredString", content);
        Assert.Contains("public required int RequiredInt", content);
        Assert.Contains("public required string RequiredAttr", content);

        // '?' syntax should NOT be used (EnableNullableDirective is off)
        Assert.DoesNotContain("string?", content);
    }

    [Fact]
    public void TestRequiredModifierWithDefaultValue()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierWithDefaultValue), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // A required element with a default value should still get 'required'.
        // It takes the DefaultValue != null code path in PropertyModel.AddMembersTo.
        Assert.Contains("public required string RequiredWithDefault", content);
    }

    [Fact]
    public void TestRequiredModifierOnRequiredEnum()
    {
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierOnRequiredEnum), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // Required enum element: gets 'required' modifier
        Assert.Contains("public required StatusEnum RequiredStatus", content);

        // Optional enum element: no 'required' modifier
        Assert.DoesNotContain("required StatusEnum OptionalStatus", content);
    }

    [Fact]
    public void TestNullableDirectiveAndRequiredCompilationRoundTrip()
    {
        // Generate files with both flags on and compile via Roslyn to verify
        // the generated code is valid C# with zero errors and zero warnings.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestNullableDirectiveAndRequiredCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(NullableAndRequiredXsd) });

        // CompileFiles reads the generated .cs files (which include #nullable enable)
        // and compiles them with the latest C# language version.
        // It asserts zero errors and zero warnings internally.
        var assembly = Compiler.CompileFiles(nameof(TestNullableDirectiveAndRequiredCompilationRoundTrip), output.Files);

        Assert.NotNull(assembly);

        // Verify the types exist in the compiled assembly
        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);

        var childType = assembly.GetType("Test.Child");
        Assert.NotNull(childType);

        var textValueType = assembly.GetType("Test.TextValue");
        Assert.NotNull(textValueType);

        var enumType = assembly.GetType("Test.StatusEnum");
        Assert.NotNull(enumType);
    }

    [Fact]
    public void TestNullableDirectiveOnInterfaceMembers()
    {
        // When GenerateInterfaces and EnableNullableDirective are both on,
        // interface members for optional reference types should use '?' syntax
        // to match the implementing class.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);
        generator.GenerateInterfaces = true;

        var contents = ConvertXml(nameof(TestNullableDirectiveOnInterfaceMembers), NullableAndRequiredInterfaceXsd, generator);
        var content = string.Join("\n", contents);

        // Extract the interface section (from 'public partial interface' to the end of content)
        var idx = content.IndexOf("public partial interface", StringComparison.Ordinal);
        Assert.True(idx >= 0, "Expected to find 'public partial interface' in generated code");
        var interfaceSection = content.Substring(idx);

        // The interface should declare optional string property with '?'
        Assert.Contains("string? OptionalLabel", interfaceSection);
        // The required string property should NOT have '?'
        Assert.DoesNotContain("string? RequiredId", interfaceSection);
    }

    [Fact]
    public void TestRequiredModifierSuppressesRequiredAttribute()
    {
        // When GenerateRequiredModifier is on, the C# 11 'required' modifier supersedes
        // [RequiredAttribute] — the attribute should not be emitted.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var contents = ConvertXml(nameof(TestRequiredModifierSuppressesRequiredAttribute), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // 'required' keyword should be present
        Assert.Contains("public required string RequiredString", content);

        // [RequiredAttribute] should NOT be present
        Assert.DoesNotContain("RequiredAttribute", content);
        Assert.DoesNotContain("AllowEmptyStrings", content);
    }

    [Fact]
    public void TestRequiredAttributeStillEmittedWithoutRequiredModifier()
    {
        // When GenerateRequiredModifier is off, [RequiredAttribute] should still be emitted
        // as before — this is the pre-existing behavior.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: false);

        var contents = ConvertXml(nameof(TestRequiredAttributeStillEmittedWithoutRequiredModifier), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // No 'required' keyword
        Assert.DoesNotContain("public required ", content);

        // [RequiredAttribute] should be present on required properties
        Assert.Contains("RequiredAttribute", content);
        Assert.Contains("AllowEmptyStrings", content);
    }

    // XSD that exercises generic collection types and array types for nullable rendering.
    // - Tags: optional unbounded string elements (→ List<string> or Collection<string>)
    // - Data: optional base64Binary (→ byte[])
    // - Name: required string (control — should NOT be nullable)
    private const string NullableCollectionAndArrayXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:complexType name=""Container"">
        <xs:sequence>
            <xs:element name=""Name"" type=""xs:string"" minOccurs=""1""/>
            <xs:element name=""Tags"" type=""xs:string"" minOccurs=""0"" maxOccurs=""unbounded""/>
            <xs:element name=""Data"" type=""xs:base64Binary"" minOccurs=""0""/>
            <xs:element name=""OptionalChild"" type=""Nested"" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
    <xs:complexType name=""Nested"">
        <xs:sequence>
            <xs:element name=""Values"" type=""xs:int"" minOccurs=""0"" maxOccurs=""unbounded""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestNullableGenericCollectionRendersCorrectSyntax()
    {
        // Verify that optional collection types produce "Collection<string>?" and NOT
        // the broken "Collection<>?<string>" that CodeDom produces when '?' is appended
        // directly to the BaseType of a generic CodeTypeReference.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);
        generator.CollectionSettersMode = CollectionSettersMode.Init;
        generator.CollectionType = typeof(System.Collections.ObjectModel.Collection<>);

        var contents = ConvertXml(nameof(TestNullableGenericCollectionRendersCorrectSyntax), NullableCollectionAndArrayXsd, generator);
        var content = string.Join("\n", contents);

        // Should contain the correct nullable collection syntax
        Assert.Contains("Collection<string>?", content);

        // Must NOT contain the broken CodeDom output
        Assert.DoesNotContain("Collection<>?", content);
        Assert.DoesNotContain("<>?<", content);
    }

    [Fact]
    public void TestNullableListCollectionRendersCorrectSyntax()
    {
        // Same test but with List<T> which is the more common collection type.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);
        generator.CollectionSettersMode = CollectionSettersMode.Init;
        // CollectionType defaults to Collection<>, so set it to List<> explicitly
        generator.CollectionType = typeof(System.Collections.Generic.List<>);

        var contents = ConvertXml(nameof(TestNullableListCollectionRendersCorrectSyntax), NullableCollectionAndArrayXsd, generator);
        var content = string.Join("\n", contents);

        // Should contain the correct nullable List<T> syntax for string collections
        // (string is a reference type so IsNullableReferenceType is true → gets '?')
        Assert.Contains("List<string>?", content);

        // List<int> does NOT get '?' because the element type (int) is a value type,
        // so IsNullableReferenceType is false for that property. This matches upstream behavior.
        Assert.DoesNotContain("List<int>?", content);

        // Must NOT contain broken CodeDom output
        Assert.DoesNotContain("List<>?", content);
        Assert.DoesNotContain("<>?<", content);
    }

    [Fact]
    public void TestNullableByteArrayRendersCorrectSyntax()
    {
        // Verify that optional byte[] (from xs:base64Binary) produces "byte[]?" and NOT
        // "byte?[]" (which would mean "array of nullable bytes" — wrong semantics).
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);

        var contents = ConvertXml(nameof(TestNullableByteArrayRendersCorrectSyntax), NullableCollectionAndArrayXsd, generator);
        var content = string.Join("\n", contents);

        // Should contain "byte[]?" for optional base64Binary
        Assert.Contains("byte[]?", content);

        // Must NOT contain "byte?[]" (nullable element instead of nullable array)
        Assert.DoesNotContain("byte?[]", content);
    }

    [Fact]
    public void TestNullableCollectionAndArrayCompilationRoundTrip()
    {
        // Compilation round-trip: generate files with EnableNullableDirective and
        // Collection<T> with init setters, then compile via Roslyn.
        // This catches any broken type syntax that slips past string assertions.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);
        generator.CollectionSettersMode = CollectionSettersMode.Init;
        generator.CollectionType = typeof(System.Collections.ObjectModel.Collection<>);

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestNullableCollectionAndArrayCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(NullableCollectionAndArrayXsd) });

        var assembly = Compiler.CompileFiles(nameof(TestNullableCollectionAndArrayCompilationRoundTrip), output.Files);

        Assert.NotNull(assembly);

        var containerType = assembly.GetType("Test.Container");
        Assert.NotNull(containerType);

        var nestedType = assembly.GetType("Test.Nested");
        Assert.NotNull(nestedType);
    }

    [Fact]
    public void TestOptionalReferenceTypeWithDefaultIsNullable()
    {
        // Issue: optional reference types with a default value (minOccurs=0 + default="...")
        // were not getting the '?' suffix because IsNullable requires DefaultValue == null.
        // Under #nullable enable, these properties must be nullable — the element can be absent
        // from XML, and users should be able to assign null without a compiler warning.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true);

        var contents = ConvertXml(nameof(TestOptionalReferenceTypeWithDefaultIsNullable), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // OptionalStringWithDefault: minOccurs=0, default="fallback" → should be string?
        Assert.Contains("string? OptionalStringWithDefault", content);

        // Control: RequiredWithDefault (minOccurs=1) should NOT be nullable
        Assert.DoesNotContain("string? RequiredWithDefault", content);

        // Control: OptionalString (no default) should still be nullable
        Assert.Contains("string? OptionalString", content);
    }

    [Fact]
    public void TestOptionalReferenceTypeWithDefaultNotNullableWhenDirectiveOff()
    {
        // When EnableNullableDirective is off, the default-value + optional combination
        // should NOT add '?' (preserves upstream behavior).
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: false);

        var contents = ConvertXml(nameof(TestOptionalReferenceTypeWithDefaultNotNullableWhenDirectiveOff), NullableAndRequiredXsd, generator);
        var content = string.Join("\n", contents);

        // No '?' syntax should appear at all when the directive is off
        Assert.DoesNotContain("string?", content);
        Assert.DoesNotContain("Child?", content);
    }

    [Fact]
    public void TestOptionalReferenceTypeWithDefaultCompilationRoundTrip()
    {
        // Compilation round-trip: optional ref types with defaults should compile cleanly.
        var generator = CreateNullableRequiredGenerator(
            enableNullableDirective: true,
            generateRequiredModifier: true);

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestOptionalReferenceTypeWithDefaultCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(NullableAndRequiredXsd) });

        var assembly = Compiler.CompileFiles(nameof(TestOptionalReferenceTypeWithDefaultCompilationRoundTrip), output.Files);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);
    }

    private const string NullableAndRequiredInterfaceXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:attributeGroup name=""CommonAttrs"">
        <xs:attribute name=""RequiredId"" type=""xs:string"" use=""required""/>
        <xs:attribute name=""OptionalLabel"" type=""xs:string"" use=""optional""/>
    </xs:attributeGroup>
    <xs:complexType name=""ItemA"">
        <xs:sequence>
            <xs:element name=""Name"" type=""xs:string"" minOccurs=""1""/>
        </xs:sequence>
        <xs:attributeGroup ref=""CommonAttrs""/>
    </xs:complexType>
</xs:schema>";

    // -- GenerateChoiceGroupAttributes tests ----------------

    private const string ChoiceGroupXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:simpleType name=""PercentageType"">
        <xs:restriction base=""xs:decimal"">
            <xs:minInclusive value=""0""/>
        </xs:restriction>
    </xs:simpleType>

    <!-- Case 1: Simple choice (each arm = single element) -->
    <xs:complexType name=""SimpleChoice"">
        <xs:sequence>
            <xs:element name=""Name"" type=""xs:string"" minOccurs=""1""/>
            <xs:choice>
                <xs:element name=""Count"" type=""xs:integer""/>
                <xs:element name=""Percentage"" type=""PercentageType""/>
            </xs:choice>
        </xs:sequence>
    </xs:complexType>

    <!-- Case 2: Multiple choice groups in the same type -->
    <xs:complexType name=""MultipleChoices"">
        <xs:sequence>
            <xs:choice>
                <xs:element name=""Alpha"" type=""xs:string""/>
                <xs:element name=""Beta"" type=""xs:string""/>
            </xs:choice>
            <xs:element name=""Middle"" type=""xs:string"" minOccurs=""0""/>
            <xs:choice>
                <xs:element name=""Gamma"" type=""xs:string""/>
                <xs:element name=""Delta"" type=""xs:string""/>
            </xs:choice>
        </xs:sequence>
    </xs:complexType>

    <!-- Case 3: Choice with sequence arm -->
    <xs:complexType name=""ChoiceWithSequence"">
        <xs:sequence>
            <xs:choice>
                <xs:element name=""Simple"" type=""xs:string""/>
                <xs:sequence>
                    <xs:element name=""PartA"" type=""xs:string""/>
                    <xs:element name=""PartB"" type=""xs:string""/>
                </xs:sequence>
            </xs:choice>
        </xs:sequence>
    </xs:complexType>

    <!-- Case 4: Both arms are sequences -->
    <xs:complexType name=""BothArmsSequences"">
        <xs:sequence>
            <xs:choice>
                <xs:sequence>
                    <xs:element name=""StartRef"" type=""xs:string""/>
                    <xs:element name=""StartName"" type=""xs:string""/>
                </xs:sequence>
                <xs:sequence>
                    <xs:element name=""EndRef"" type=""xs:string""/>
                    <xs:element name=""EndName"" type=""xs:string""/>
                </xs:sequence>
            </xs:choice>
        </xs:sequence>
    </xs:complexType>

    <!-- Case 5: Nested choice-in-choice (direct) — should flatten -->
    <xs:complexType name=""NestedChoice"">
        <xs:sequence>
            <xs:choice>
                <xs:choice>
                    <xs:element name=""A"" type=""xs:string""/>
                    <xs:element name=""B"" type=""xs:string""/>
                </xs:choice>
                <xs:element name=""C"" type=""xs:string""/>
            </xs:choice>
        </xs:sequence>
    </xs:complexType>

    <!-- Case 6: Choice inside sequence inside choice — NOT flattened -->
    <xs:complexType name=""ChoiceInSequenceInChoice"">
        <xs:sequence>
            <xs:choice>
                <xs:sequence>
                    <xs:choice>
                        <xs:element name=""InnerX"" type=""xs:string""/>
                        <xs:element name=""InnerY"" type=""xs:string""/>
                    </xs:choice>
                    <xs:element name=""Extra"" type=""xs:string""/>
                </xs:sequence>
                <xs:element name=""Standalone"" type=""xs:string""/>
            </xs:choice>
        </xs:sequence>
    </xs:complexType>

    <!-- Case 7: No choice (control — should have no attributes) -->
    <xs:complexType name=""NoChoice"">
        <xs:sequence>
            <xs:element name=""Foo"" type=""xs:string""/>
            <xs:element name=""Bar"" type=""xs:string""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    private static Generator CreateChoiceGroupGenerator()
    {
        return new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
            DataAnnotationMode = DataAnnotationMode.All,
            NetCoreSpecificCode = true,
            GenerateChoiceGroupAttributes = true,
        };
    }

    [Fact]
    public void TestChoiceGroupSimpleChoiceEmitsAttributes()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupSimpleChoiceEmitsAttributes), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "SimpleChoice");

        // Count and Percentage should have XmlChoiceGroup attributes with same groupId, different armIds.
        Assert.Matches(@"XmlChoiceGroupAttribute\(\d+, 0\).*Count", block.Replace("\n", " "));
        Assert.Matches(@"XmlChoiceGroupAttribute\(\d+, 1\).*Percentage", block.Replace("\n", " "));

        // Name is not in a choice — should NOT have the attribute.
        var nameLines = block.Split('\n').Where(l => l.Contains("\"Name\"") || l.Contains("Name {")).ToList();
        Assert.DoesNotContain("XmlChoiceGroup", string.Join(" ", nameLines));
    }

    [Fact]
    public void TestChoiceGroupMultipleGroupsHaveDifferentIds()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupMultipleGroupsHaveDifferentIds), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "MultipleChoices");

        // Extract all XmlChoiceGroupAttribute occurrences.
        var matches = Regex.Matches(block, @"XmlChoiceGroupAttribute\((\d+), (\d+)\)");
        Assert.Equal(4, matches.Count); // Alpha, Beta, Gamma, Delta

        var groupIds = matches.Select(m => int.Parse(m.Groups[1].Value)).Distinct().ToList();
        Assert.Equal(2, groupIds.Count); // Two distinct group IDs

        // Alpha and Beta share a group, Gamma and Delta share a different group.
        var group1Arms = matches.Where(m => int.Parse(m.Groups[1].Value) == groupIds[0])
            .Select(m => int.Parse(m.Groups[2].Value)).OrderBy(x => x).ToList();
        var group2Arms = matches.Where(m => int.Parse(m.Groups[1].Value) == groupIds[1])
            .Select(m => int.Parse(m.Groups[2].Value)).OrderBy(x => x).ToList();
        Assert.Equal([0, 1], group1Arms);
        Assert.Equal([0, 1], group2Arms);

        // Middle is not in a choice — should NOT have the attribute.
        Assert.DoesNotContain("XmlChoiceGroup", block.Split('\n')
            .FirstOrDefault(l => l.Contains("\"Middle\"")) ?? "");
    }

    [Fact]
    public void TestChoiceGroupSequenceArmsShareArmId()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupSequenceArmsShareArmId), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "ChoiceWithSequence");

        var matches = Regex.Matches(block, @"XmlChoiceGroupAttribute\((\d+), (\d+)\)");
        Assert.Equal(3, matches.Count); // Simple, PartA, PartB

        // All should share the same groupId.
        var groupIds = matches.Select(m => int.Parse(m.Groups[1].Value)).Distinct().ToList();
        Assert.Single(groupIds);

        // Simple should be arm 0. PartA and PartB should share arm 1 (from the sequence).
        var arms = matches.Select(m => (
            arm: int.Parse(m.Groups[2].Value),
            // Find the property name after this attribute
            text: block.Substring(m.Index)
        )).ToList();

        // Simple = arm 0
        var simpleArm = matches.First(m => block.Substring(m.Index, 100).Contains("Simple"));
        Assert.Equal("0", simpleArm.Groups[2].Value);

        // PartA and PartB = arm 1
        var partAArm = matches.First(m => block.Substring(m.Index, 100).Contains("PartA"));
        var partBArm = matches.First(m => block.Substring(m.Index, 100).Contains("PartB"));
        Assert.Equal("1", partAArm.Groups[2].Value);
        Assert.Equal("1", partBArm.Groups[2].Value);
    }

    [Fact]
    public void TestChoiceGroupBothArmsSequences()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupBothArmsSequences), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "BothArmsSequences");

        var matches = Regex.Matches(block, @"XmlChoiceGroupAttribute\((\d+), (\d+)\)");
        Assert.Equal(4, matches.Count); // StartRef, StartName, EndRef, EndName

        // All share the same groupId.
        var groupIds = matches.Select(m => int.Parse(m.Groups[1].Value)).Distinct().ToList();
        Assert.Single(groupIds);

        // StartRef + StartName = arm 0, EndRef + EndName = arm 1.
        var startRefArm = matches.First(m => block.Substring(m.Index, 100).Contains("StartRef"));
        var startNameArm = matches.First(m => block.Substring(m.Index, 100).Contains("StartName"));
        var endRefArm = matches.First(m => block.Substring(m.Index, 100).Contains("EndRef"));
        var endNameArm = matches.First(m => block.Substring(m.Index, 100).Contains("EndName"));

        Assert.Equal(startRefArm.Groups[2].Value, startNameArm.Groups[2].Value); // same arm
        Assert.Equal(endRefArm.Groups[2].Value, endNameArm.Groups[2].Value);     // same arm
        Assert.NotEqual(startRefArm.Groups[2].Value, endRefArm.Groups[2].Value); // different arms
    }

    [Fact]
    public void TestChoiceGroupNestedChoiceFlattens()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupNestedChoiceFlattens), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "NestedChoice");

        var matches = Regex.Matches(block, @"XmlChoiceGroupAttribute\((\d+), (\d+)\)");
        Assert.Equal(3, matches.Count); // A, B, C

        // All should share the same groupId (nested choice flattened).
        var groupIds = matches.Select(m => int.Parse(m.Groups[1].Value)).Distinct().ToList();
        Assert.Single(groupIds);

        // A, B, C should all have distinct arm IDs.
        var armIds = matches.Select(m => int.Parse(m.Groups[2].Value)).OrderBy(x => x).ToList();
        Assert.Equal(3, armIds.Distinct().Count());
    }

    [Fact]
    public void TestChoiceGroupChoiceInSequenceInChoiceNotFlattened()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupChoiceInSequenceInChoiceNotFlattened), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "ChoiceInSequenceInChoice");

        var matches = Regex.Matches(block, @"XmlChoiceGroupAttribute\((\d+), (\d+)\)");
        // InnerX, InnerY (inner choice group), Extra (outer arm 0), Standalone (outer arm 1)
        Assert.Equal(4, matches.Count);

        // Should have TWO distinct group IDs (inner choice is NOT flattened).
        var groupIds = matches.Select(m => int.Parse(m.Groups[1].Value)).Distinct().OrderBy(x => x).ToList();
        Assert.Equal(2, groupIds.Count);

        // InnerX and InnerY have the inner group ID with different arms.
        var innerXMatch = matches.First(m => block.Substring(m.Index, 120).Contains("InnerX"));
        var innerYMatch = matches.First(m => block.Substring(m.Index, 120).Contains("InnerY"));
        Assert.Equal(innerXMatch.Groups[1].Value, innerYMatch.Groups[1].Value); // same group
        Assert.NotEqual(innerXMatch.Groups[2].Value, innerYMatch.Groups[2].Value); // different arms

        // Extra and Standalone have the outer group ID.
        var extraMatch = matches.First(m => block.Substring(m.Index, 120).Contains("Extra"));
        var standaloneMatch = matches.First(m => block.Substring(m.Index, 120).Contains("Standalone"));
        Assert.Equal(extraMatch.Groups[1].Value, standaloneMatch.Groups[1].Value); // same group
        Assert.NotEqual(extraMatch.Groups[1].Value, innerXMatch.Groups[1].Value); // different from inner group
    }

    [Fact]
    public void TestChoiceGroupNotEmittedWhenFlagOff()
    {
        var generator = CreateChoiceGroupGenerator();
        generator.GenerateChoiceGroupAttributes = false;
        var contents = ConvertXml(nameof(TestChoiceGroupNotEmittedWhenFlagOff), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);

        Assert.DoesNotContain("XmlChoiceGroup", content);
    }

    [Fact]
    public void TestChoiceGroupNoAttributeOnNonChoiceElements()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupNoAttributeOnNonChoiceElements), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);
        var block = ExtractClassBlock(content, "NoChoice");

        Assert.DoesNotContain("XmlChoiceGroup", block);
    }

    [Fact]
    public void TestChoiceGroupAttributeClassGenerated()
    {
        var generator = CreateChoiceGroupGenerator();
        var contents = ConvertXml(nameof(TestChoiceGroupAttributeClassGenerated), ChoiceGroupXsd, generator);
        var content = string.Join("\n", contents);

        // The XmlChoiceGroupAttribute class should be generated.
        Assert.Contains("class XmlChoiceGroupAttribute", content);
        Assert.Contains("public int GroupId", content);
        Assert.Contains("public int ArmId", content);
    }

    // -- GenerateStrictFixedValues tests ----------------

    private const string FixedValueXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""Normal"" type=""xs:string"" minOccurs=""0""/>
            <xs:element name=""DefaultVal"" type=""xs:string"" default=""hello"" minOccurs=""0""/>
            <xs:element name=""FixedVal"" type=""xs:string"" fixed=""constant"" minOccurs=""0""/>
            <xs:element name=""FixedInt"" type=""xs:int"" fixed=""42"" minOccurs=""0""/>
        </xs:sequence>
        <xs:attribute name=""Version"" type=""xs:string"" fixed=""1.0""/>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestFixedValueWithoutStrictHasSetter()
    {
        // Without strict fixed values, properties with fixed values should have setters.
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictFixedValues = false,
        };
        var contents = ConvertXml(nameof(TestFixedValueWithoutStrictHasSetter), FixedValueXsd, generator);
        var content = string.Join("\n", contents);

        // FixedVal should have get and set
        Assert.Contains("get", content);
        Assert.Matches(@"FixedVal[^}]*\bset\b", content);
    }

    [Fact]
    public void TestFixedValueWithStrictIsReadOnly()
    {
        // With strict fixed values, properties with fixed values should be getter-only.
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictFixedValues = true,
        };
        var contents = ConvertXml(nameof(TestFixedValueWithStrictIsReadOnly), FixedValueXsd, generator);
        var content = string.Join("\n", contents);

        // FixedVal and FixedInt should NOT have setters
        var fixedValBlock = ExtractPropertyBlock(content, "FixedVal");
        Assert.Contains("get", fixedValBlock);
        Assert.DoesNotContain("set", fixedValBlock);

        var fixedIntBlock = ExtractPropertyBlock(content, "FixedInt");
        Assert.Contains("get", fixedIntBlock);
        Assert.DoesNotContain("set", fixedIntBlock);

        var versionBlock = ExtractPropertyBlock(content, "Version");
        Assert.Contains("get", versionBlock);
        Assert.DoesNotContain("set", versionBlock);

        // DefaultVal should still have a setter (it has a default, not a fixed value)
        var defaultBlock = ExtractPropertyBlock(content, "DefaultVal");
        Assert.Contains("get", defaultBlock);
        Assert.Contains("set", defaultBlock);

        // Normal should still have a setter
        var normalBlock = ExtractPropertyBlock(content, "Normal");
        Assert.Contains("set", normalBlock);
    }

    [Fact]
    public void TestFixedValueWithStrictCompiles()
    {
        // The generated code with strict fixed values should compile successfully.
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictFixedValues = true,
        };
        var contents = ConvertXml(nameof(TestFixedValueWithStrictCompiles), FixedValueXsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestFixedValueWithStrictCompiles), contents);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);

        // FixedVal property should exist and have no setter
        var fixedValProp = rootType.GetProperty("FixedVal");
        Assert.NotNull(fixedValProp);
        Assert.True(fixedValProp.CanRead);
        Assert.False(fixedValProp.CanWrite);

        // FixedInt property should exist and have no setter
        var fixedIntProp = rootType.GetProperty("FixedInt");
        Assert.NotNull(fixedIntProp);
        Assert.True(fixedIntProp.CanRead);
        Assert.False(fixedIntProp.CanWrite);

        // Version attribute should exist and have no setter
        var versionProp = rootType.GetProperty("Version");
        Assert.NotNull(versionProp);
        Assert.True(versionProp.CanRead);
        Assert.False(versionProp.CanWrite);

        // DefaultVal should still have a setter
        var defaultProp = rootType.GetProperty("DefaultVal");
        Assert.NotNull(defaultProp);
        Assert.True(defaultProp.CanRead);
        Assert.True(defaultProp.CanWrite);

        // Verify the fixed value is correct via a default instance
        var instance = Activator.CreateInstance(rootType);
        Assert.Equal("constant", fixedValProp.GetValue(instance));
        Assert.Equal(42, fixedIntProp.GetValue(instance));
        Assert.Equal("1.0", versionProp.GetValue(instance));
    }

    /// <summary>
    /// Extracts the property block (from the property type through its closing brace)
    /// for a given property name from generated C# source.
    /// </summary>
    private static string ExtractPropertyBlock(string source, string propertyName)
    {
        // Match pattern: anything up to and including the property name, then capture
        // until the next property or end of class. Properties in the CodeDom hack are
        // CodeMemberFields whose Name includes the accessor block.
        var idx = source.IndexOf(propertyName);
        if (idx < 0) return string.Empty;

        // Walk forward to find the balanced braces for the property accessors
        var start = idx;
        int braceCount = 0;
        bool inBraces = false;
        for (int i = idx; i < source.Length; i++)
        {
            if (source[i] == '{') { braceCount++; inBraces = true; }
            if (source[i] == '}') { braceCount--; }
            if (inBraces && braceCount == 0)
                return source.Substring(start, i - start + 1);
        }
        return source.Substring(start);
    }

    // -- GenerateStrictRangeBounds tests ----------------

    private const string SoloRangeBoundsXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:simpleType name=""PercentageType"">
        <xs:restriction base=""xs:decimal"">
            <xs:minInclusive value=""0""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""BoundedType"">
        <xs:restriction base=""xs:decimal"">
            <xs:minInclusive value=""0""/>
            <xs:maxInclusive value=""100""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""CappedType"">
        <xs:restriction base=""xs:decimal"">
            <xs:maxInclusive value=""999""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""Pct"" type=""PercentageType""/>
            <xs:element name=""Bounded"" type=""BoundedType""/>
            <xs:element name=""Capped"" type=""CappedType""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestSoloRangeBoundsWithoutStrictNoRange()
    {
        // Without strict range bounds, solo minInclusive should NOT produce a [Range] attribute.
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = false,
        };
        var contents = ConvertXml(nameof(TestSoloRangeBoundsWithoutStrictNoRange), SoloRangeBoundsXsd, generator);
        var content = string.Join("\n", contents);

        // BoundedType (both bounds) should still produce a [Range]
        Assert.Contains(@"RangeAttribute(typeof(decimal)", content);

        // Count total [Range] attributes — should be exactly 1 (only BoundedType)
        var rangeCount = System.Text.RegularExpressions.Regex.Matches(content, @"RangeAttribute\(typeof").Count;
        Assert.Equal(1, rangeCount);
    }

    [Fact]
    public void TestSoloRangeBoundsWithStrictEmitsRange()
    {
        // With strict range bounds, solo minInclusive should produce a [Range] attribute
        // with the type's maximum as the upper bound.
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = true,
        };
        var contents = ConvertXml(nameof(TestSoloRangeBoundsWithStrictEmitsRange), SoloRangeBoundsXsd, generator);
        var content = string.Join("\n", contents);

        // Should have 3 [Range] attributes total (Percentage, Bounded, Capped)
        var rangeCount = System.Text.RegularExpressions.Regex.Matches(content, @"RangeAttribute\(typeof").Count;
        Assert.Equal(3, rangeCount);

        // PercentageType (solo minInclusive=0) should include "0" as min bound
        Assert.Contains(@"""0""", content);

        // CappedType (solo maxInclusive=999) should include "999" as max bound
        Assert.Contains(@"""999""", content);
    }

    [Fact]
    public void TestSoloRangeBoundsWithStrictCompiles()
    {
        // The generated code with strict range bounds should compile successfully.
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = true,
        };
        var contents = ConvertXml(nameof(TestSoloRangeBoundsWithStrictCompiles), SoloRangeBoundsXsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestSoloRangeBoundsWithStrictCompiles), contents);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);

        // Pct property should have a [Range] attribute
        var pctProp = rootType.GetProperty("Pct");
        Assert.NotNull(pctProp);
        var rangeAttr = pctProp.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), false);
        Assert.Single(rangeAttr);
    }

    // -- totalDigits/fractionDigits → Range tests ----------------

    private const string DigitsRangeXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:simpleType name=""ThreeDigitType"">
        <xs:restriction base=""xs:decimal"">
            <xs:totalDigits value=""3""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""FiveDigitTwoFractionType"">
        <xs:restriction base=""xs:decimal"">
            <xs:totalDigits value=""5""/>
            <xs:fractionDigits value=""2""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:simpleType name=""ExplicitBoundsType"">
        <xs:restriction base=""xs:decimal"">
            <xs:totalDigits value=""3""/>
            <xs:minInclusive value=""0""/>
            <xs:maxInclusive value=""100""/>
        </xs:restriction>
    </xs:simpleType>
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""ThreeDigit"" type=""ThreeDigitType""/>
            <xs:element name=""FiveTwo"" type=""FiveDigitTwoFractionType""/>
            <xs:element name=""Explicit"" type=""ExplicitBoundsType""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestTotalDigitsRangeWithoutStrictNoRange()
    {
        // Without strict, totalDigits should NOT produce a [Range].
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = false,
        };
        var contents = ConvertXml(nameof(TestTotalDigitsRangeWithoutStrictNoRange), DigitsRangeXsd, generator);
        var content = string.Join("\n", contents);

        // Only ExplicitBoundsType should produce a [Range] (both bounds present)
        var rangeCount = System.Text.RegularExpressions.Regex.Matches(content, @"RangeAttribute\(typeof").Count;
        Assert.Equal(1, rangeCount);
    }

    [Fact]
    public void TestTotalDigitsRangeWithStrictEmitsRange()
    {
        // With strict, totalDigits=3 should produce [Range(-999, 999)].
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = true,
        };
        var contents = ConvertXml(nameof(TestTotalDigitsRangeWithStrictEmitsRange), DigitsRangeXsd, generator);
        var content = string.Join("\n", contents);

        // ThreeDigitType: totalDigits=3 → [-999, 999]
        Assert.Contains(@"""-999""", content);
        Assert.Contains(@"""999""", content);

        // FiveDigitTwoFractionType: totalDigits=5, fractionDigits=2 → [-999.99, 999.99]
        Assert.Contains(@"""-999.99""", content);
        Assert.Contains(@"""999.99""", content);

        // Should have 3 [Range] attributes: ThreeDigit, FiveTwo, Explicit
        var rangeCount = System.Text.RegularExpressions.Regex.Matches(content, @"RangeAttribute\(typeof").Count;
        Assert.Equal(3, rangeCount);
    }

    [Fact]
    public void TestTotalDigitsExplicitBoundsOverrideDigits()
    {
        // When explicit bounds are present, totalDigits should NOT produce an additional [Range].
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = true,
        };
        var contents = ConvertXml(nameof(TestTotalDigitsExplicitBoundsOverrideDigits), DigitsRangeXsd, generator);
        var content = string.Join("\n", contents);

        // ExplicitBoundsType should have [Range(0, 100)], NOT [Range(-999, 999)]
        Assert.Contains(@"""0""", content);
        Assert.Contains(@"""100""", content);

        // Should have exactly 3 ranges (one per type), not 4
        var rangeCount = System.Text.RegularExpressions.Regex.Matches(content, @"RangeAttribute\(typeof").Count;
        Assert.Equal(3, rangeCount);
    }

    [Fact]
    public void TestTotalDigitsRangeWithStrictCompiles()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateStrictRangeBounds = true,
        };
        var contents = ConvertXml(nameof(TestTotalDigitsRangeWithStrictCompiles), DigitsRangeXsd, generator).ToArray();
        var assembly = Compiler.Compile(nameof(TestTotalDigitsRangeWithStrictCompiles), contents);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);

        // ThreeDigit should have a [Range] attribute with bounds -999..999
        var prop = rootType.GetProperty("ThreeDigit");
        Assert.NotNull(prop);
        var rangeAttrs = prop.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RangeAttribute), false);
        Assert.Single(rangeAttrs);
        var range = (System.ComponentModel.DataAnnotations.RangeAttribute)rangeAttrs[0];
        Assert.Equal("-999", range.Minimum?.ToString());
        Assert.Equal("999", range.Maximum?.ToString());
    }

    // -- DefaultValueAttribute suppression for nullable reference types ----------------

    private const string DefaultValueNullableXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:complexType name=""DefaultValueRoot"">
        <xs:sequence>
            <xs:element name=""OptionalStringWithDefault"" type=""xs:string"" minOccurs=""0"" default=""hello""/>
            <xs:element name=""OptionalIntWithDefault"" type=""xs:int"" minOccurs=""0"" default=""42""/>
            <xs:element name=""RequiredStringWithDefault"" type=""xs:string"" default=""world""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestNullableDirectiveSuppressesDefaultValueAttributeForOptionalString()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            EnableNullableDirective = true,
            GenerateNullables = true,
        };

        var contents = ConvertXml(nameof(TestNullableDirectiveSuppressesDefaultValueAttributeForOptionalString), DefaultValueNullableXsd, generator);
        var content = string.Join("\n", contents);

        Assert.DoesNotContain("DefaultValueAttribute(\"hello\")", content);
        Assert.Contains("= \"hello\"", content);
    }

    [Fact]
    public void TestDefaultValueAttributeStillEmittedWithoutNullableDirective()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            EnableNullableDirective = false,
            GenerateNullables = true,
        };

        var contents = ConvertXml(nameof(TestDefaultValueAttributeStillEmittedWithoutNullableDirective), DefaultValueNullableXsd, generator);
        var content = string.Join("\n", contents);

        Assert.Contains("DefaultValueAttribute(\"hello\")", content);
    }

    [Fact]
    public void TestDefaultValueAttributeStillEmittedForValueTypeWithNullableDirective()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            EnableNullableDirective = true,
            GenerateNullables = true,
        };

        var contents = ConvertXml(nameof(TestDefaultValueAttributeStillEmittedForValueTypeWithNullableDirective), DefaultValueNullableXsd, generator);
        var content = string.Join("\n", contents);

        Assert.Contains("DefaultValueAttribute(42)", content);
    }

    [Fact]
    public void TestNullableDirectiveDefaultValueCompilationRoundTrip()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            EnableNullableDirective = true,
            GenerateNullables = true,
        };

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestNullableDirectiveDefaultValueCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(DefaultValueNullableXsd) });

        var assembly = Compiler.CompileFiles(nameof(TestNullableDirectiveDefaultValueCompilationRoundTrip), output.Files);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.DefaultValueRoot");
        Assert.NotNull(rootType);

        Assert.NotNull(rootType.GetProperty("OptionalStringWithDefault"));
        Assert.NotNull(rootType.GetProperty("OptionalIntWithDefault"));
        Assert.NotNull(rootType.GetProperty("RequiredStringWithDefault"));
    }

    private const string MixedContentDefaultValueXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:complexType name=""MixedStringType"" mixed=""true"">
        <xs:sequence>
            <xs:element name=""Sub"" type=""xs:string"" minOccurs=""0""/>
        </xs:sequence>
        <xs:attribute name=""lang"" type=""xs:language""/>
    </xs:complexType>
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""Label"" type=""MixedStringType"" default=""hello"" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestMixedContentTypeWithDefaultValueGeneratesCode()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
        };

        var contents = ConvertXml(nameof(TestMixedContentTypeWithDefaultValueGeneratesCode), MixedContentDefaultValueXsd, generator);
        var content = string.Join("\n", contents);

        Assert.Contains("MixedStringType", content);
        Assert.Contains("Label", content);
        Assert.Contains("Text = new string[] { \"hello\" }", content);
    }

    [Fact]
    public void TestMixedContentTypeWithDefaultValueCompilationRoundTrip()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
        };

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestMixedContentTypeWithDefaultValueCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(MixedContentDefaultValueXsd) });

        var assembly = Compiler.CompileFiles(nameof(TestMixedContentTypeWithDefaultValueCompilationRoundTrip), output.Files);
        Assert.NotNull(assembly);

        var mixedType = assembly.GetType("Test.MixedStringType");
        Assert.NotNull(mixedType);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);
        Assert.NotNull(rootType.GetProperty("Label"));
    }

    private const string SimpleContentRestrictionDefaultValueXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:complexType name=""BaseRefStructure"">
        <xs:simpleContent>
            <xs:extension base=""xs:normalizedString"">
                <xs:attribute name=""ref"" type=""xs:string"" use=""required""/>
            </xs:extension>
        </xs:simpleContent>
    </xs:complexType>
    <xs:complexType name=""DerivedRefStructure"">
        <xs:simpleContent>
            <xs:restriction base=""BaseRefStructure"">
                <xs:attribute name=""ref"" type=""xs:string"" use=""required""/>
            </xs:restriction>
        </xs:simpleContent>
    </xs:complexType>
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""DerivedRef"" type=""DerivedRefStructure"" default=""false"" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestSimpleContentRestrictionWithDefaultValueGeneratesCode()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
        };

        var contents = ConvertXml(nameof(TestSimpleContentRestrictionWithDefaultValueGeneratesCode), SimpleContentRestrictionDefaultValueXsd, generator);
        var content = string.Join("\n", contents);

        Assert.Contains("DerivedRefStructure", content);
        Assert.Contains("= \"false\"", content);
    }

    [Fact]
    public void TestSimpleContentRestrictionWithDefaultValueCompilationRoundTrip()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
        };

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestSimpleContentRestrictionWithDefaultValueCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(SimpleContentRestrictionDefaultValueXsd) });

        var assembly = Compiler.CompileFiles(nameof(TestSimpleContentRestrictionWithDefaultValueCompilationRoundTrip), output.Files);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);
        Assert.NotNull(rootType.GetProperty("DerivedRef"));
    }

    private const string SimpleContentExtensionDefaultValueXsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" elementFormDefault=""qualified"">
    <xs:complexType name=""MultilingualString"">
        <xs:simpleContent>
            <xs:extension base=""xs:normalizedString"">
                <xs:attribute name=""lang"" type=""xs:language""/>
            </xs:extension>
        </xs:simpleContent>
    </xs:complexType>
    <xs:complexType name=""Root"">
        <xs:sequence>
            <xs:element name=""Note"" type=""MultilingualString"" default=""false."" minOccurs=""0""/>
        </xs:sequence>
    </xs:complexType>
</xs:schema>";

    [Fact]
    public void TestSimpleContentExtensionWithDefaultValueGeneratesCode()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
        };

        var contents = ConvertXml(nameof(TestSimpleContentExtensionWithDefaultValueGeneratesCode), SimpleContentExtensionDefaultValueXsd, generator);
        var content = string.Join("\n", contents);

        Assert.Contains("MultilingualString", content);
        Assert.Contains("= \"false.\"", content);
    }

    [Fact]
    public void TestSimpleContentExtensionWithDefaultValueCompilationRoundTrip()
    {
        var generator = new Generator
        {
            NamespaceProvider = new NamespaceProvider { GenerateNamespace = key => "Test" },
            GenerateNullables = true,
        };

        var output = new FileWatcherOutputWriter(Path.Combine("output", nameof(TestSimpleContentExtensionWithDefaultValueCompilationRoundTrip)));
        generator.OutputWriter = output;
        output.Configuration = generator.Configuration;

        generator.Generate(new[] { new StringReader(SimpleContentExtensionDefaultValueXsd) });

        var assembly = Compiler.CompileFiles(nameof(TestSimpleContentExtensionWithDefaultValueCompilationRoundTrip), output.Files);
        Assert.NotNull(assembly);

        var rootType = assembly.GetType("Test.Root");
        Assert.NotNull(rootType);
        Assert.NotNull(rootType.GetProperty("Note"));
    }

}
