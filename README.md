XmlSchemaClassGenerator
=======================

[![Nuget](https://img.shields.io/nuget/v/XmlSchemaClassGenerator-beta)](https://www.nuget.org/packages/XmlSchemaClassGenerator-beta/)
[![Build status](https://ci.appveyor.com/api/projects/status/yhxiw0stmv5y7f6n/branch/master?svg=true)](https://ci.appveyor.com/project/mganss/xmlschemaclassgenerator/branch/master)
[![codecov.io](https://codecov.io/github/mganss/XmlSchemaClassGenerator/coverage.svg?branch=master)](https://codecov.io/github/mganss/XmlSchemaClassGenerator?branch=master)
[![netstandard2.0](https://img.shields.io/badge/netstandard-2.0-brightgreen.svg)](https://img.shields.io/badge/netstandard-2.0-brightgreen.svg)
[![net462](https://img.shields.io/badge/net-462-brightgreen.svg)](https://img.shields.io/badge/net-462-brightgreen.svg)

A console program and library to generate 
[XmlSerializer](http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlserializer.aspx) compatible C# classes
from [XML Schema](http://en.wikipedia.org/wiki/XML_Schema_(W3C)) files.

Features
--------

* Map XML namespaces to C# namespaces, either explicitly or through a (configurable) function
* Generate C# XML comments from schema annotations
* Generate [DataAnnotations](http://msdn.microsoft.com/en-us/library/system.componentmodel.dataannotations.aspx) attributes 
from schema restrictions
* Generate custom attributes for schema restrictions that aren't covered by standard DataAnnotations (see [below](#restriction-attributes))
* Use [`Collection<T>`](http://msdn.microsoft.com/en-us/library/ms132397.aspx) properties 
(initialized in constructor and with private setter)
* Map xs:integer and derived types to the closest possible .NET type, if not possible - fall back to string. Can be overriden by explicitly defined type (int, long, or decimal)
* Automatic properties
* Pascal case for classes and properties
* Generate nullable adapter properties for optional elements and attributes without default values (see [below](#nullables))
* Optional support for PCL
* Optional support for [`INotifyPropertyChanged`](http://msdn.microsoft.com/en-us/library/system.componentmodel.inotifypropertychanged)
* Optional support for Entity Framework Code First (automatically generate key properties)
* Optionally generate interfaces for groups and attribute groups
* Optionally generate one file per class
* Support for nullable reference types (NRTs) through [`AllowNullAttribute`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.allownullattribute) and [`MaybeNullAttribute`](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.codeanalysis.maybenullattribute)
* Optional `#nullable enable` directive with native nullable reference type syntax (`string?`) instead of attributes
* Optional C# 11 `required` modifier on properties corresponding to required XSD elements and attributes
* Optionally generate a common specific type for union member types

Unsupported:

* Some restriction types
* Recursive choices and choices whose elements have minOccurs > 0 or nillable="true" (see [below](#choice))
* Possible name clashes and invalid identifiers when names contain non-alphanumeric characters
* Groups with maxOccurs > 0

Usage
-----

For command line use, choose your preferred installation:
- Binary zips included in the [releases on GitHub](https://github.com/mganss/XmlSchemaClassGenerator/releases)
- Binaries in the tools folder in the [console application NuGet package](https://www.nuget.org/packages/XmlSchemaClassGenerator.Console/)
- .NET Core CLI tool available in the [dotnet-xscgen NuGet package](https://www.nuget.org/packages/dotnet-xscgen/)
- CI Builds are available at the NuGet feed https://ci.appveyor.com/nuget/xmlschemaclassgenerator-0f1t3r6ti475

```
Usage: xscgen [OPTIONS]+ xsdFile...
Generate C# classes from XML Schema files.
xsdFiles may contain globs, e.g. "content\{schema,xsd}\**\*.xsd", and URLs.
Append - to option to disable it, e.g. --interface-.
```

| Option | Description |
| ------ | ----------- |
| `-h`, `--help` | Show help and exit |
| `-n`, `--namespace=VALUE` | Map an XML namespace to a C# namespace. Separate XML namespace and C# namespace by `=`. A single value (no `=`) is taken as the C# namespace the empty XML namespace is mapped to. One option must be given for each namespace to be mapped. A file name may be given by appending a pipe sign (`\|`) followed by a file name (like `schema.xsd`) to the XML namespace. If no mapping is found for an XML namespace, a name is generated automatically (may fail). |
| `--nf`, `--namespaceFile=VALUE` | File containing namespace mappings (one per line: `XML namespace = C# namespace [file name]`). Lines starting with `#` and empty lines are ignored. |
| `--tns`, `--typeNameSubstitute=VALUE` | Substitute a generated type/member name. Separate type/member name and substitute name by `=`. Prefix with a kind ID as [documented here](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d42-id-string-format). Prefix with `A:` to substitute any type/member. |
| `--tnsf`, `--typeNameSubstituteFile=VALUE` | File containing type/member name substitutions (one per line: `prefixed name = substitute`). Lines starting with `#` and empty lines are ignored. |
| `-o`, `--output=FOLDER` | The folder to write the resulting `.cs` files to |
| `-d`, `--datetime-offset` | Map `xs:datetime` and derived types to `System.DateTimeOffset` instead of `System.DateTime` |
| `--do`, `--dateOnly` | Map `xs:date` to `System.DateOnly` and `xs:time` to `System.TimeOnly` |
| `-i`, `--integer=TYPE` | Map `xs:integer` and derived types to TYPE instead of automatic approximation. TYPE can be `sb[yte]`, `b[yte]`, `sh[ort]`, `us[hort]`, `i[nt]`, `ui[nt]`, `l[ong]`, `ul[ong]`, `ni[nt]`, `nui[nt]`, or `d[ecimal]` |
| `--fb`, `--fallback` | Use integer type specified via `-i` only if no type can be deduced |
| `-e`, `--edb`, `--enable-data-binding` | Enable `INotifyPropertyChanged` data binding |
| `-r`, `--order` | Emit order for all class members stored as XML element |
| `-c`, `--pcl` | PCL compatible output |
| `-p`, `--prefix=PREFIX` | The prefix to prepend to auto-generated namespace names |
| `-v`, `--verbose` | Print generated file names on stdout |
| `-0`, `--nullable` | Generate nullable adapter properties for optional elements/attributes without default values |
| `-f`, `--ef` | Generate Entity Framework Code First compatible classes |
| `-t`, `--interface` | Generate interfaces for groups and attribute groups (default: enabled) |
| `-a`, `--pascal` | Use Pascal case for class and property names (default: enabled) |
| `--av`, `--assemblyVisible` | Use the `internal` visibility modifier (default: false) |
| `-u`, `--enableUpaCheck` | Check for Unique Particle Attribution (UPA) in `XmlSchemaSet` (default: enabled) |
| `--ct`, `--collectionType=VALUE` | Collection type to use (default: `System.Collections.ObjectModel.Collection`1`) |
| `--cit`, `--collectionImplementationType=VALUE` | Collection type implementation to use (default: null) |
| `--csm`, `--collectionSettersMode=VALUE` | Collection setter mode: `Private`, `Public`, `PublicWithoutConstructorInitialization`, `Init`, `InitWithoutConstructorInitialization` (default: `Private`) |
| `--ctro`, `--codeTypeReferenceOptions=VALUE` | `CodeTypeReferenceOptions` flags: `GlobalReference`, `GenericTypeParameter` (default: unset) |
| `--tvpn`, `--textValuePropertyName=VALUE` | Name of the property that holds the text value of an element (default: `Value`) |
| `--dst`, `--debuggerStepThrough` | Generate `DebuggerStepThroughAttribute` (default: enabled) |
| `--dc`, `--disableComments` | Do not include comments from XSD |
| `--nu`, `--noUnderscore` | Do not generate underscore in private member name (default: false) |
| `--da`, `--description` | Generate `DescriptionAttribute` (default: true) |
| `--cc`, `--complexTypesForCollections` | Generate complex types for collections (default: true) |
| `-s`, `--useShouldSerialize` | Use `ShouldSerialize` pattern instead of `Specified` pattern (default: false) |
| `--sf`, `--separateFiles` | Generate a separate file for each class (default: false) |
| `--nh`, `--namespaceHierarchy` | Generate a separate folder for namespace hierarchy; implies `--separateFiles` (default: false) |
| `--sg`, `--separateSubstitutes` | Generate a separate property for each element of a substitution group (default: false) |
| `--dnfin`, `--doNotForceIsNullable` | Do not force `IsNullable = true` in `XmlElement` annotation for nillable elements when element is nullable (default: false) |
| `--cn`, `--compactTypeNames` | Use type names without namespace qualifier for types in the using list (default: false) |
| `--cl`, `--commentLanguages=VALUE` | Comment languages to use (default: `en`; supported: `en`, `de`) |
| `--un`, `--uniqueTypeNames` | Generate type names that are unique across namespaces (default: false) |
| `--gc`, `--generatedCodeAttribute` | Add version information to `GeneratedCodeAttribute` (default: true) |
| `--nc`, `--netCore` | Generate .NET Core specific code that might not work with .NET Framework (default: false) |
| `--nr`, `--nullableReferenceAttributes` | Generate `[AllowNull]`/`[MaybeNull]` attributes for nullable reference types (default: false) |
| `--st`, `--strict` | Enable all strict compile-time enforcement options: `--nd`, `--rm`, `--cg`, `--ecl`, `--fv`, `--rb` (default: false). Individual flags placed after `--strict` override it, e.g. `--strict --rm-` |
| `--nd`, `--nullableDirective` | Emit `#nullable enable` and use native nullable reference type syntax (`string?`) instead of attributes (default: false) |
| `--rm`, `--requiredModifier` | Emit C# 11 `required` modifier on required properties, replacing `[Required]` attribute (default: false) |
| `--cg`, `--choiceGroupAttributes` | Emit `[XmlChoiceGroup]` attributes on choice element properties for Roslyn analyzer enforcement (default: false) |
| `--fv`, `--fixedValues` | Generate read-only (getter-only) properties for fixed-value elements and attributes (default: false) |
| `--rb`, `--rangeBounds` | Emit `[Range]` even when only one bound (`minInclusive` or `maxInclusive`) is present, filling the missing bound from the CLR type (default: false) |
| `--ar`, `--useArrayItemAttribute` | Use `ArrayItemAttribute` for sequences with single elements (default: true) |
| `--es`, `--enumAsString` | Use `string` instead of `enum` for enumerations |
| `--dmb`, `--disableMergeRestrictionsWithBase` | Disable merging of simple type restrictions with base type restrictions |
| `--ca`, `--commandArgs` | Generate a comment with the exact command line arguments used to generate the source code (default: true) |
| `--uc`, `--unionCommonType` | Generate a common type for unions if possible (default: false) |
| `--ec`, `--serializeEmptyCollections` | Serialize empty collections (default: false) |
| `--dtd`, `--allowDtdParse` | Allow DTD parsing (default: false) |
| `--oxi`, `--omitXmlIncludeAttribute` | Omit generation of `XmlIncludeAttribute` for derived types (default: false) |
| `--ecl`, `--enumCollection` | Generate typed enum collections for `xs:list` types instead of string collections (default: false) |
| `--ns`, `--namingScheme=VALUE` | Naming scheme for class and property names: `Direct`, `Pascal`, `Legacy` (default: `Pascal`) |
| `--fu`, `--forceUriScheme=VALUE` | Force URI scheme when resolving URLs (default: `none`; can be: `none`, `same`, or any scheme like `https`) |
| `--ema`, `--emitMetadataAttributes` | Emit metadata helper attributes (default: `false`) |
| `--mn`, `--metadataNamespace=VALUE` | Namespace for generated metadata helper attributes (default: `XmlSchemaClassGenerator.Metadata`) |

For use from code use the [library NuGet package](https://www.nuget.org/packages/XmlSchemaClassGenerator-beta/):

```C#
var generator = new Generator
{
    OutputFolder = outputFolder,
    Log = s => Console.Out.WriteLine(s),
    GenerateNullables = true,
    NamespaceProvider = new Dictionary<NamespaceKey, string> 
    { 
        { new NamespaceKey("http://wadl.dev.java.net/2009/02"), "Wadl" } 
    }
    .ToNamespaceProvider(new GeneratorConfiguration { NamespacePrefix = "Wadl" }.NamespaceProvider.GenerateNamespace)
};

generator.Generate(files);
```

Specifying the `NamespaceProvider` is optional. If you don't provide one, C# namespaces will be generated automatically. The example above shows how to create a custom `NamespaceProvider` that has a dictionary for a number of specific namespaces as well as a generator function for XML namespaces that are not in the dictionary. In the example the generator function is the default function but with a custom namespace prefix. You can also use a custom generator function, e.g.

```C#
var generator = new Generator
{
    NamespaceProvider = new NamespaceProvider
    {
        GenerateNamespace = key => ...
    }
};
```

### Mapping xsd files to C# namespaces

Using the optional `|` syntax of the `-n` command line option you can map individual xsd files to C# namespaces. If you have several input files using the same XML namespace you can still generate an individual C# namespace for the types defined within a single xsd file. For example, if you have two input files `a.xsd` and `b.xsd` both of which have the same `targetNamespace` of `http://example.com/namespace` you can generate the C# namespaces `Example.NamespaceA` and `Example.NamespaceB`:

```
xscgen -n "|a.xsd=Example.NamespaceA" -n "|b.xsd=Example.NamespaceB" a.xsd b.xsd
```

#### Mapping empty XML namespaces

In order to provide a C# namespace name for an empty XML namespace you can specify it on the command line like this:

```
xscgen -n Example example.xsd
```

An alternative form that is also valid is `-n =Example`. Note the space between `-n` and `=Example`.

#### Using mapping files

Instead of specifying the namespace mappings on the command line you can also use a mapping file which should contain one mapping per line in the following format:

```
# Comment

http://example.com = Example.NamespaceA a.xsd
http://example.com = Example.NamespaceB b.xsd
Empty
# or alternatively
= Empty
```

Use the `--nf` option to specify the mapping file.

### Substituting generated C# type and member names

If a xsd file specifies obscure names for their types (classes, enums) or members (properties), you can substitute these using the `--tns`/`--typeNameSubstitute=` parameter:

```
xscgen --tns T:Example_RootType=Example --tns T:Example_RootTypeExampleScope=ExampleScope --tns P:StartDateDateTimeValue=StartDate example.xsd
```

The syntax for substitution is: `{kindId}:{generatedName}={substituteName}`

The `{kindId}` is a single character identifier based on [documentation/analysis ID format](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/documentation-comments#d42-id-string-format), where valid values are:

| ID | Scope |
| -  | - |
| `P` | Property |
| `T` | Type: `class`, `enum`, `interface` |
| `A` | Any property and/or type |

#### Using substitution files

Instead of specifying the substitutions on the command line you can also use a substitution file which should contain one substitution per line in the following format:

```
# Comment
T:Example_RootType = Example
T:Example_RootTypeExampleScope = ExampleScope
P:StartDateDateTimeValue = StartDate
```

Use the `--tnsf`/`--typeNameSubstituteFile` option to specify the substitution file.

Nullables<a name="nullables"></a>
---------------------------------

XmlSerializer has been present in the .NET Framework since version 1.1 
and has never been updated to provide support for nullables
which are a natural fit for the problem of signaling the absence or presence of a value type
but have only been present since .NET Framework 2.0.

Instead XmlSerializer has support for a pattern where you provide an additional bool property
with "Specified" appended to the name to signal if the original property should be serialized. 
For example:

```xml
<xs:attribute name="id" type="xs:int" use="optional">...</xs:attribute>
```

```C#
[System.Xml.Serialization.XmlAttributeAttribute("id", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="int")]
public int Id { get; set; }

[System.Xml.Serialization.XmlIgnoreAttribute()]
public bool IdSpecified { get; set; }
```

XmlSchemaClassGenerator can optionally generate an additional nullable property that works as an adapter to both properties:

```C#
[System.Xml.Serialization.XmlAttributeAttribute("id", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="int")]
public int IdValue { get; set; }
        
[System.Xml.Serialization.XmlIgnoreAttribute()]
public bool IdValueSpecified { get; set; }

[System.Xml.Serialization.XmlIgnoreAttribute()]
public System.Nullable<int> Id
{
    get
    {
        if (this.IdValueSpecified)
        {
            return this.IdValue;
        }
        else
        {
            return null;
        }
    }
    set
    {
        this.IdValue = value.GetValueOrDefault();
        this.IdValueSpecified = value.HasValue;
    }
}
```

Strict mode and compile-time enforcement<a name="nullable-directive"></a>
-----------------------------------------

The `--strict` flag (`--st`) enables all strict compile-time enforcement options at once: `--nd`, `--rm`, `--cg`, `--ecl`, `--fv`, and `--rb`. Individual flags placed **after** `--strict` on the command line override it, e.g. `--strict --rm-` enables everything except the `required` modifier. This is the recommended mode for new projects.

The following options control the individual strict features:

| Option | C# version | What it does |
| ------ | ---------- | ------------ |
| `--nr` / `--nullableReferenceAttributes` | C# 8+ | Adds `[AllowNull]` and `[MaybeNull]` attributes to optional reference-type properties |
| `--nd` / `--nullableDirective` | C# 8+ | Emits `#nullable enable` at the top of each file and uses native `?` syntax (`string?`) instead of attributes. Also suppresses `[DefaultValueAttribute]` for optional nullable reference-type properties (see below) |
| `--rm` / `--requiredModifier` | C# 11+ | Adds the `required` modifier to properties corresponding to required XSD elements (`minOccurs >= 1`) or attributes (`use="required"`), replacing the `[Required]` attribute |
| `--cg` / `--choiceGroupAttributes` | Any | Emits `[XmlChoiceGroup(groupId, armId)]` attributes on choice element properties, enabling the companion Roslyn analyzer to enforce mutual exclusivity at compile time |
| `--ecl` / `--enumCollection` | Any | Generates typed enum collections for `xs:list` types, ensuring enum values are matched exactly during serialization |
| `--fv` / `--fixedValues` | Any | Generates read-only (getter-only) properties for elements and attributes with `fixed` values, preventing accidental overwrite at compile time |
| `--rb` / `--rangeBounds` | Any | Emits `[Range]` attributes even when only one bound (`minInclusive`/`maxInclusive`) is specified; the missing bound is filled from the CLR type's min/max. Also considers `minExclusive`/`maxExclusive` bounds |

`--nd` supersedes `--nr`: when `--nd` is active, optional reference-type properties use `string?` directly and the `[AllowNull]`/`[MaybeNull]` attributes are not emitted (except for array types where the `?` suffix cannot be applied through CodeDom). Similarly, `--rm` supersedes `[Required]`: the `required` keyword provides strictly stronger compile-time enforcement, so the `[Required]` attribute is no longer emitted.

`--nd` also suppresses `[DefaultValueAttribute]` for optional nullable reference-type properties. Without this, `XmlSerializer` compares the property value to the `[DefaultValue]` and **omits** the element from the XML when they match -- meaning a property initialized to its default can never serialize that default. Under `#nullable enable`, the nullable contract replaces `[DefaultValue]` for controlling serialization: `null` means the element is absent, and any non-null value (including the default) is serialized. The backing field is still initialized to the XSD default value, so newly constructed objects start with the correct default. Value-type properties are unaffected and continue to use `[DefaultValueAttribute]` normally.

Using `--nd` and `--rm` together gives the strongest compile-time safety: the compiler will warn on uninitialized non-nullable properties and error on missing `required` properties in object initializers.

```C#
#nullable enable

[XmlTypeAttribute("PublicationDeliveryStructure", Namespace="http://www.netex.org.uk/netex")]
[XmlRootAttribute("PublicationDelivery", Namespace="http://www.netex.org.uk/netex")]
public partial class PublicationDeliveryStructure
{
    [XmlElementAttribute("PublicationTimestamp", Order=0)]
    public required DateTimeOffset PublicationTimestamp { get; set; }

    [XmlElementAttribute("ParticipantRef", Order=1)]
    public required string ParticipantRef { get; set; }

    [XmlElementAttribute("PublicationRequest", Order=2)]
    public PublicationRequestStructure? PublicationRequest { get; set; }

    [XmlElementAttribute("Description", Order=3)]
    public MultilingualString? Description { get; set; }
}
```

Choice Elements<a name="choice"></a>
------------------------------------

The support for choice elements differs from that [provided by xsd.exe](http://msdn.microsoft.com/en-us/library/sa6z5baz).
Xsd.exe generates a property called `Item` of type `object` and, if not all choices have a distinct type, 
another enum property that selects the chosen element.
Besides being non-typesafe and non-intuitive, this approach breaks apart if the choices have a more complicated structure (e.g. sequences),
resulting in possibly schema-invalid XML.

XmlSchemaClassGenerator currently simply pretends choices are sequences.
This means you'll have to take care only to set a schema-valid combination of these properties to non-null values.

Interfaces<a name="interfaces"></a>
-----------------------------------

Groups and attribute groups in XML Schema are reusable components that can be included in multiple type definitions. XmlSchemaClassGenerator can optionally generate interfaces from these groups to make it easier to access common properties on otherwise unrelated classes. So

```XML
<xs:attributeGroup name="Common">
  <xs:attribute name="name" type="xs:string"></xs:attribute>
</xs:attributeGroup>

<xs:complexType name="A">
  <xs:attributeGroup ref="Common"/>
</xs:complexType>

<xs:complexType name="B">
  <xs:attributeGroup ref="Common"/>
</xs:complexType>
```

becomes

```C#
public partial interface ICommon
{
  string Name { get; set; }
}

public partial class A: ICommon
{
  public string Name { get; set; }
}

public partial class B: ICommon
{
  public string Name { get; set; }
}
```

Collection types
----------------

Values for the `--collectionType` and `--collectionImplementationType` options have to be given in the format accepted by
the [`Type.GetType()`](https://docs.microsoft.com/en-us/dotnet/api/system.type.gettype) method. For the `System.Collections.Generic.List<T>` class this means ``System.Collections.Generic.List`1``.
Make sure to escape the backtick character (`` ` ``) to prevent it from being interpreted by the shell.

Integer and derived types
---------------------
Not all numeric types defined by XML Schema can be safely and accurately mapped to .NET numeric data types, however, it's possible to approximate the mapping based on the integer bounds and restrictions such as `totalDigits`.  
If an explicit integer type mapping is specified via `--integer=TYPE`, that type will be used, otherwise an approximation will be made based on the table below. If you additionally specify `--fallback`, the type specified via `--integer=TYPE` will be used only if no type can be deduced by applying the rules below.

If the restrictions `minInclusive` and `maxInclusive` are present on the integer element, then the smallest CLR type that fully encompasses the specified range will be used. Unsigned types are given precedence over signed types. The following table shows the possible ranges and their corresponding CLR type, in the order they will be applied.

<table>
  <tr>
    <th>Minimum (Inclusive)</th>
	<th>Maximum (Inclusive)</th>
	<th>C# type</th>
	<tr><td>sbyte.MinValue</td><td>sbyte.MaxValue</td><td>sbyte</td></tr>
	<tr><td>byte.MinValue</td><td>byte.MaxValue</td><td>byte</td></tr>
	<tr><td>ushort.MinValue</td><td>ushort.MaxValue</td><td>ushort</td></tr>
	<tr><td>short.MinValue</td><td>short.MaxValue</td><td>short</td></tr>
	<tr><td>uint.MinValue</td><td>uint.MaxValue</td><td>uint</td></tr>
	<tr><td>int.MinValue</td><td>int.MaxValue</td><td>int</td></tr>
	<tr><td>ulong.MinValue</td><td>ulong.MaxValue</td><td>ulong</td></tr>
	<tr><td>long.MinValue</td><td>long.MaxValue</td><td>long</td></tr>
	<tr><td>decimal.MinValue</td><td>decimal.MaxValue</td><td>decimal</td></tr>
  </tr>
</table>

If the range specified by `minInclusive` and `maxInclusive` does not fit in any CLR type, or if those restrictions are not present, then the `totalDigits` restriction will be used, as shown in the following table.

<table>
  <tr>
    <th>XML Schema type</th>
    <th>totalDigits</th>
    <th>C# type</th>
  </tr>
  <tr><td rowspan="6">xs:positiveInteger<br>xs:nonNegativeInteger</td><td>&lt;3</td><td>byte</td></tr>
  <tr><td>&lt;5</td><td>ushort</td></tr>
  <tr><td>&lt;10</td><td>uint</td></tr>
  <tr><td>&lt;20</td><td>ulong</td></tr>
  <tr><td>&lt;30</td><td>decimal</td></tr>
  <tr><td>&gt;=30</td><td>string</td></tr>
  <tr><td rowspan="6">xs:integer<br>xs:nonPositiveInteger<br>xs:negativeInteger</td><td>&lt;3</td><td>sbyte</td></tr>
  <tr><td>&lt;5</td><td>short</td></tr>
  <tr><td>&lt;10</td><td>int</td></tr>
  <tr><td>&lt;19</td><td>long</td></tr>
  <tr><td>&lt;29</td><td>decimal</td></tr>
  <tr><td>&gt;=29</td><td>string</td></tr>
</table>

Unions
------

If you specify `--unionCommonType`, XmlSchemaClassGenerator will try to determine a common type for a union's member types. If, for example, the member types
are all integer types, then the narrowest integer type will be used that can fit all member types.

Note that semantic issues might arise with this approach. For example, `DateTime` values are serialized with both date and time information included. See discussion at [#397](https://github.com/mganss/XmlSchemaClassGenerator/issues/397).

Restriction attributes
----------------------

When `EmitMetadataAttributes` is enabled, the generator emits custom attributes for XML schema restrictions that aren't covered by standard DataAnnotations:

<table>
  <tr>
    <th>XML Schema facet</th>
    <th>Generated attribute</th>
  </tr>
  <tr><td>xs:fractionDigits</td><td><code>FractionDigitsAttribute</code></td></tr>
  <tr><td>xs:maxLength / xs:minLength on a repeating element</td><td><code>CollectionItemStringLengthAttribute</code></td></tr>
</table>

The attribute definitions are automatically generated in the namespace specified through `--metadataNamespace`. If not specified, the default namespace is `XmlSchemaClassGenerator.Metadata`.

Contributing
------------

Contributions are welcome. Here are some guidelines:

- If it's not a trivial fix, please submit an issue first
- Try and blend new code with the existing code's style
- Add unit tests
