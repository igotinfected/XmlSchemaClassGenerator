using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator;

public class SimpleModel(GeneratorConfiguration configuration) : TypeModel(configuration)
{
    public Type ValueType { get; set; }
    public List<RestrictionModel> Restrictions { get; } = [];
    public bool UseDataTypeAttribute { get; set; } = true;

    /// <summary>
    /// Represents the item type of a list of enums if this simple type is generated as a collection of enums.
    /// </summary>
    public TypeModel EnumListItemType { get; set; }

    public static string GetCollectionDefinitionName(string typeName, GeneratorConfiguration configuration)
    {
        var type = configuration.CollectionType;
        var typeRef = CodeUtilities.CreateTypeReference(type, configuration);
        return GetFullTypeName(typeName, typeRef, type);
    }

    public static string GetCollectionImplementationName(string typeName, GeneratorConfiguration configuration)
    {
        var type = configuration.CollectionImplementationType ?? configuration.CollectionType;
        var typeRef = CodeUtilities.CreateTypeReference(type, configuration);
        return GetFullTypeName(typeName, typeRef, type);
    }

    private static string GetFullTypeName(string typeName, CodeTypeReference typeRef, Type type)
    {
        if (type.IsGenericTypeDefinition)
        {
            typeRef.TypeArguments.Add(typeName);
        }
        else if (type == typeof(Array))
        {
            typeRef.ArrayElementType = new CodeTypeReference(typeName);
            typeRef.ArrayRank = 1;
        }
        var typeOfExpr = new CodeTypeOfExpression(typeRef)
        {
            Type = { Options = CodeTypeReferenceOptions.GenericTypeParameter }
        };
        var fullTypeName = GenerateCSharpCodeFromExpression(typeOfExpr);
        Debug.Assert(fullTypeName.StartsWith("typeof(") && fullTypeName.EndsWith(")"), $"Expected typeof expression, got: {fullTypeName}");
        return fullTypeName.Substring(7, fullTypeName.Length - 8);
    }

    public override CodeTypeDeclaration Generate()
    {
        return null;
    }

    public override CodeTypeReference GetReferenceFor(NamespaceModel referencingNamespace, bool collection = false, bool forInit = false, bool attribute = false)
    {
        // if this simpleType is a collection of enums, return the list item type
        if (Configuration.EnumCollection && EnumListItemType != null)
        {
            return EnumListItemType.GetReferenceFor(referencingNamespace, collection: collection, forInit: forInit, attribute: attribute);
        }

        var type = ValueType;

        if (XmlSchemaType != null)
        {
            // some types are not mapped in the same way between XmlSerializer and XmlSchema >(
            // http://msdn.microsoft.com/en-us/library/aa719879(v=vs.71).aspx
            // http://msdn.microsoft.com/en-us/library/system.xml.serialization.xmlelementattribute.datatype(v=vs.110).aspx
            // XmlSerializer is inconsistent: maps xs:decimal to decimal but xs:integer to string,
            // even though xs:integer is a restriction of xs:decimal
            type = XmlSchemaType.Datatype.GetEffectiveType(Configuration, Restrictions, XmlSchemaType, attribute);
            UseDataTypeAttribute = XmlSchemaType.Datatype.IsDataTypeAttributeAllowed(Configuration) ?? UseDataTypeAttribute;
        }

        if (collection)
        {
            var collectionType = forInit ? (Configuration.CollectionImplementationType ?? Configuration.CollectionType) : Configuration.CollectionType;

            if (collectionType.IsGenericType)
            {
                type = collectionType.MakeGenericType(type);
            }
            else
            {
                if (collectionType == typeof(Array))
                {
                    type = type.MakeArrayType();
                }
                else
                {
                    type = collectionType;
                }
            }
        }

        return CodeUtilities.CreateTypeReference(type, Configuration);
    }

    public override CodeExpression GetDefaultValueFor(string defaultString, bool attribute)
    {
        var type = ValueType;

        if (XmlSchemaType != null)
        {
            type = XmlSchemaType.Datatype.GetEffectiveType(Configuration, Restrictions, XmlSchemaType, attribute);
        }

        if (type == typeof(XmlQualifiedName))
        {
            if (defaultString.StartsWith("xs:", StringComparison.OrdinalIgnoreCase))
            {
                var rv = new CodeObjectCreateExpression(typeof(XmlQualifiedName),
                    new CodePrimitiveExpression(defaultString.Substring(3)),
                    new CodePrimitiveExpression(XmlSchema.Namespace));
                rv.CreateType.Options = Configuration.CodeTypeReferenceOptions;
                return rv;
            }
            throw new NotSupportedException(string.Format("Resolving default value {0} for QName not supported.", defaultString));
        }
        else if (type == typeof(DateTime))
        {
            return new CodeMethodInvokeExpression(TypeRefExpr<DateTime>(), nameof(DateTime.Parse), new CodePrimitiveExpression(defaultString));
        }
        else if (type == typeof(DateOnly))
        {
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.DateOnly"), "Parse", new CodePrimitiveExpression(defaultString));
        }
        else if (type == typeof(TimeOnly))
        {
            return new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("System.TimeOnly"), "Parse", new CodePrimitiveExpression(defaultString));
        }
        else if (type == typeof(TimeSpan))
        {
            return new CodeMethodInvokeExpression(TypeRefExpr<XmlConvert>(), nameof(XmlConvert.ToTimeSpan), new CodePrimitiveExpression(defaultString));
        }
        else if (type == typeof(bool) && !string.IsNullOrWhiteSpace(defaultString))
        {
            var val = defaultString switch
            {
                "0" => false,
                "1" => true,
                _ => Convert.ChangeType(defaultString, ValueType)
            };
            return new CodePrimitiveExpression(val);
        }
        else if (type == typeof(byte[]) && defaultString != null)
        {
            int numberChars = defaultString.Length;
            var byteValues = new CodePrimitiveExpression[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                byteValues[i / 2] = new CodePrimitiveExpression(Convert.ToByte(defaultString.Substring(i, 2), 16));

            // For whatever reason, CodeDom will not generate a semicolon for the assignment statement if CodeArrayCreateExpression
            //  is used alone. Casting the value to the same type to work around this issue.
            return new CodeCastExpression(typeof(byte[]), new CodeArrayCreateExpression(typeof(byte), byteValues));
        }
        else if (type == typeof(double) && !string.IsNullOrWhiteSpace(defaultString))
        {
            if (defaultString.Equals("inf", StringComparison.OrdinalIgnoreCase))
                return new CodePrimitiveExpression(double.PositiveInfinity);
            else if (defaultString.Equals("-inf", StringComparison.OrdinalIgnoreCase))
                return new CodePrimitiveExpression(double.NegativeInfinity);
        }

        return new CodePrimitiveExpression(Convert.ChangeType(defaultString, ValueType, CultureInfo.InvariantCulture));
    }

    public IEnumerable<CodeAttributeDeclaration> GetRestrictionAttributes()
    {
        foreach (var attribute in Restrictions.Where(x => x.IsSupported).Select(r => r.GetAttribute()).Where(a => a != null))
            yield return attribute;

        var minInclusive = Restrictions.OfType<MinInclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported);
        var maxInclusive = Restrictions.OfType<MaxInclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported);

        // When strict range bounds is on, also consider exclusive bounds and solo bounds.
        var minExclusive = Configuration.GenerateStrictRangeBounds
            ? Restrictions.OfType<MinExclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported)
            : null;
        var maxExclusive = Configuration.GenerateStrictRangeBounds
            ? Restrictions.OfType<MaxExclusiveRestrictionModel>().FirstOrDefault(x => x.IsSupported)
            : null;

        // Determine effective bounds. Inclusive takes precedence over exclusive.
        var hasMin = minInclusive != null || minExclusive != null;
        var hasMax = maxInclusive != null || maxExclusive != null;
        var effectiveType = minInclusive?.Type ?? minExclusive?.Type ?? maxInclusive?.Type ?? maxExclusive?.Type;

        var emittedExplicitRange = false;

        if (minInclusive != null && maxInclusive != null)
        {
            // Original path: both inclusive bounds present (works without --strict).
            yield return BuildRangeAttribute(minInclusive.Value, maxInclusive.Value, minInclusive.Type);
            emittedExplicitRange = true;
        }
        else if (Configuration.GenerateStrictRangeBounds && (hasMin || hasMax) && effectiveType != null)
        {
            // Strict path: fill in the missing bound from the CLR type's min/max.
            var minValue = minInclusive?.Value
                ?? minExclusive?.Value // [Range] only supports inclusive; the exclusive value is a conservative approximation
                ?? GetTypeBoundary(effectiveType, min: true);
            var maxValue = maxInclusive?.Value
                ?? maxExclusive?.Value
                ?? GetTypeBoundary(effectiveType, min: false);

            if (minValue != null && maxValue != null)
            {
                yield return BuildRangeAttribute(minValue, maxValue, effectiveType);
                emittedExplicitRange = true;
            }
        }

        // When strict range bounds is on and no explicit [Range] was emitted,
        // derive a range from totalDigits/fractionDigits restrictions.
        // totalDigits=n means at most n digits total; fractionDigits=f means f of those
        // are after the decimal point. The maximum value is (10^n - 1) / 10^f.
        if (Configuration.GenerateStrictRangeBounds && !emittedExplicitRange)
        {
            var totalDigits = Restrictions.OfType<TotalDigitsRestrictionModel>().FirstOrDefault(x => x.IsSupported);
            if (totalDigits != null && totalDigits.Value > 0)
            {
                var fractionDigits = Restrictions.OfType<FractionDigitsRestrictionModel>().FirstOrDefault(x => x.IsSupported);
                var fraction = fractionDigits?.Value ?? 0;

                // Compute the maximum absolute value: (10^totalDigits - 1) / 10^fractionDigits
                // Use decimal arithmetic to avoid floating-point precision issues.
                // Guard against unreasonably large digit counts that would overflow decimal.
                if (totalDigits.Value <= 28) // decimal has ~28-29 significant digits
                {
                    var maxAbsolute = DecimalPow10(totalDigits.Value) - 1m;
                    if (fraction > 0)
                        maxAbsolute /= DecimalPow10(fraction);

                    var maxStr = maxAbsolute.ToString(CultureInfo.InvariantCulture);
                    var minStr = (-maxAbsolute).ToString(CultureInfo.InvariantCulture);

                    yield return BuildRangeAttribute(minStr, maxStr, ValueType ?? typeof(decimal));
                }
            }
        }
    }

    private static decimal DecimalPow10(int exponent)
    {
        var result = 1m;
        for (int i = 0; i < exponent; i++)
            result *= 10m;
        return result;
    }

    private CodeAttributeDeclaration BuildRangeAttribute(string minValue, string maxValue, Type type)
    {
        var rangeAttribute = new CodeAttributeDeclaration(
            CodeUtilities.CreateTypeReference(Attributes.Range, Configuration),
            new(new CodeTypeOfExpression(GetReferenceFor(Namespace))),
            new(new CodePrimitiveExpression(minValue)),
            new(new CodePrimitiveExpression(maxValue)));

        // see https://github.com/mganss/XmlSchemaClassGenerator/issues/268
        if (Configuration.NetCoreSpecificCode)
        {
            if (minValue.Contains(".") || maxValue.Contains("."))
                rangeAttribute.Arguments.Add(new("ParseLimitsInInvariantCulture", new CodePrimitiveExpression(true)));

            if (type != typeof(int) && type != typeof(double))
                rangeAttribute.Arguments.Add(new("ConvertValueInInvariantCulture", new CodePrimitiveExpression(true)));
        }

        return rangeAttribute;
    }

    /// <summary>
    /// Gets the string representation of a CLR type's minimum or maximum value,
    /// suitable for use as a <c>[Range]</c> attribute bound.
    /// Returns <c>null</c> if the type is not recognized.
    /// </summary>
    private static string GetTypeBoundary(Type type, bool min)
    {
        if (type == typeof(byte)) return min ? "0" : "255";
        if (type == typeof(sbyte)) return min ? "-128" : "127";
        if (type == typeof(short)) return min ? "-32768" : "32767";
        if (type == typeof(ushort)) return min ? "0" : "65535";
        if (type == typeof(int)) return min ? "-2147483648" : "2147483647";
        if (type == typeof(uint)) return min ? "0" : "4294967295";
        if (type == typeof(long)) return min ? "-9223372036854775808" : "9223372036854775807";
        if (type == typeof(ulong)) return min ? "0" : "18446744073709551615";
        if (type == typeof(float)) return (min ? float.MinValue : float.MaxValue).ToString(CultureInfo.InvariantCulture);
        if (type == typeof(double)) return (min ? double.MinValue : double.MaxValue).ToString(CultureInfo.InvariantCulture);
        if (type == typeof(decimal)) return (min ? decimal.MinValue : decimal.MaxValue).ToString(CultureInfo.InvariantCulture);
        return null;
    }
}
