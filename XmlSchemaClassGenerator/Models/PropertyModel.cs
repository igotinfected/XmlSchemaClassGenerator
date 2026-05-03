using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace XmlSchemaClassGenerator;

[DebuggerDisplay("{Name}")]
public class PropertyModel(GeneratorConfiguration configuration, string name, TypeModel type, TypeModel owningType) : GeneratorModel(configuration)
{
    private const string Value = nameof(Value);
    private const string Specified = nameof(Specified);
    private const string Namespace = nameof(XmlRootAttribute.Namespace);

    // ctor
    public List<DocumentationModel> Documentation { get; } = [];
    public List<Substitute> Substitutes { get; } = [];
    public TypeModel OwningType { get; } = owningType;
    public TypeModel Type { get; } = type;
    public string Name { get; set; } = name;

    // private
    public string OriginalPropertyName { get; private set; }
    public string RenamedFrom { get; set; }
    public string DefaultValue { get; private set; }
    public string FixedValue { get; private set; }
    public XmlSchemaForm Form { get; private set; }
    public string XmlNamespace { get; private set; }
    public XmlQualifiedName XmlSchemaName { get; private set; }
    public XmlSchemaParticle XmlParticle { get; private set; }
    public XmlSchemaObject XmlParent { get; private set; }
    public Particle Particle { get; private set; }

    // public
    public bool IsAttribute { get; set; }
    public bool IsRequired { get; set; }
    public bool IsNillable { get; set; }
    public bool IsCollection { get; set; }
    public bool IsDeprecated { get; set; }
    public bool IsAny { get; set; }
    public int? Order { get; set; }
    public bool IsKey { get; set; }

    /// <summary>
    /// Identifies which choice group this property belongs to, if any.
    /// Null means the property is not part of a choice.
    /// </summary>
    public int? ChoiceGroupId { get; set; }

    /// <summary>
    /// Identifies which arm within a choice group this property belongs to.
    /// Elements in the same arm (e.g. from a sequence within a choice) share the same arm ID.
    /// </summary>
    public int? ChoiceArmId { get; set; }

    public void SetFromNode(string originalName, bool useFixedIfNoDefault, IXmlSchemaNode xs)
    {
        OriginalPropertyName = originalName;

        DefaultValue = xs.DefaultValue ?? (useFixedIfNoDefault ? xs.FixedValue : null);
        FixedValue = xs.FixedValue;
        Form = xs.Form switch
        {
            XmlSchemaForm.None => xs.RefName?.IsEmpty == false ? XmlSchemaForm.Qualified : xs.FormDefault,
            _ => xs.Form,
        };
    }

    public void SetFromParticles(Particle particle, Particle item, bool isRequired)
    {
        Particle = item;
        XmlParticle = item.XmlParticle;
        XmlParent = item.XmlParent;

        IsRequired = isRequired;
        IsCollection = item.MaxOccurs > 1.0m || particle.MaxOccurs > 1.0m; // http://msdn.microsoft.com/en-us/library/vstudio/d3hx2s7e(v=vs.100).aspx

        ChoiceGroupId = item.ChoiceGroupId;
        ChoiceArmId = item.ChoiceArmId;
    }

    public void SetSchemaNameAndNamespace(TypeModel owningTypeModel, IXmlSchemaNode xs)
    {
        XmlSchemaName = xs.QualifiedName;
        XmlNamespace = string.IsNullOrEmpty(xs.QualifiedName.Namespace)
                       || xs.QualifiedName.Namespace == owningTypeModel.XmlSchemaName.Namespace ? null
                        : xs.QualifiedName.Namespace;
    }

    /// <summary>
    /// Cached reflection accessor for <see cref="CodeTypeReference"/>'s private <c>_baseType</c> field.
    /// Used by <see cref="CreateLiteralTypeRef"/> to bypass CodeDom's type name parsing.
    /// </summary>
    private static readonly FieldInfo BaseTypeField =
        typeof(CodeTypeReference).GetField("_baseType", BindingFlags.NonPublic | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Could not find CodeTypeReference._baseType field via reflection.");

    /// <summary>
    /// Creates a <see cref="CodeTypeReference"/> that renders as the given literal string,
    /// bypassing CodeDom's type name parsing. This is necessary for types that include
    /// syntax CodeDom cannot represent (e.g. <c>List&lt;string&gt;?</c>, <c>byte[]?</c>,
    /// or <c>required string</c>), because CodeDom's constructor splits on <c>&lt;</c>,
    /// <c>&gt;</c>, <c>[</c>, and <c>]</c>, losing any trailing <c>?</c> suffix or
    /// unrecognised prefix.
    /// </summary>
    internal static CodeTypeReference CreateLiteralTypeRef(string literalTypeName)
    {
        var typeRef = new CodeTypeReference();
        BaseTypeField.SetValue(typeRef, literalTypeName);
        return typeRef;
    }

    /// <summary>
    /// Renders a <see cref="CodeTypeReference"/> to its C# source representation
    /// (resolving aliases and generic arguments), then wraps the result with an
    /// optional <paramref name="prefix"/> and/or <paramref name="suffix"/> into
    /// a literal <see cref="CodeTypeReference"/> that CodeDom outputs verbatim.
    /// </summary>
    internal static CodeTypeReference WrapTypeRef(CodeTypeReference source, string prefix = "", string suffix = "")
    {
        var rendered = TypeModel.GetCSharpTypeOutput(source);
        return CreateLiteralTypeRef(prefix + rendered + suffix);
    }

    internal static string GetAccessors(CodeMemberField backingField = null, bool withDataBinding = false, PropertyValueTypeCode typeCode = PropertyValueTypeCode.Other, string setter = "set")
    {
        return backingField == null ? " { get; set; }" : CodeUtilities.NormalizeNewlines($@"
        {{
            get
            {{
                return {backingField.Name};
            }}
            {setter}
            {{{(typeCode, withDataBinding) switch
        {
            (PropertyValueTypeCode.ValueType, true) => $@"
                if ({checkEquality()}){assignAndNotify()}",
            (PropertyValueTypeCode.Other or PropertyValueTypeCode.Array, true) => $@"
                if ({backingField.Name} == value)
                    return;
                if ({backingField.Name} == null || value == null || {checkEquality()}){assignAndNotify()}",
            _ => assign(),
        }}
            }}
        }}");

        string assign() => $@"
                {backingField.Name} = value;";

        string assignAndNotify() => $@"
                {{{assign()}
                    {OnPropertyChanged}();
                }}";

        string checkEquality()
            => $"!{backingField.Name}.{(typeCode is PropertyValueTypeCode.Array ? nameof(Enumerable.SequenceEqual) : EqualsMethod)}(value)";
    }

    private ClassModel TypeClassModel => Type as ClassModel;

    /// <summary>
    /// A property is an array if it is a sequence containing a single element with maxOccurs > 1.
    /// </summary>
    public bool IsArray => Configuration.UseArrayItemAttribute
            && !IsCollection && !IsList && TypeClassModel != null
            && TypeClassModel.BaseClass == null
            && TypeClassModel.Properties.Count == 1
            && !TypeClassModel.Properties[0].IsAttribute && !TypeClassModel.Properties[0].IsAny
            && TypeClassModel.Properties[0].IsCollection;

    private bool IsEnumerable => IsCollection || IsArray || IsList;

    private TypeModel PropertyType => !IsArray ? Type : TypeClassModel.Properties[0].Type;

    private bool IsNullable => DefaultValue == null && !IsRequired;

    private bool IsValueType => PropertyType is EnumModel || (PropertyType is SimpleModel model && model.ValueType.IsValueType);

    private bool IsNullableValueType => IsNullable && !IsEnumerable && IsValueType;

    // A reference type is nullable when:
    // - the property is optional (IsNullable covers no-default + not-required), OR
    // - the property has a default value but is still optional (!IsRequired),
    //   because a reference type with a default can still legitimately be null (element absent from XML).
    //   IsNullable excludes defaults because value types use the Specified pattern instead of Nullable<T>,
    //   but that reasoning doesn't apply to reference types which are inherently nullable.
    //
    // This applies to both the EnableNullableDirective ('?' syntax) and
    // EnableNullableReferenceAttributes ([AllowNull]/[MaybeNull]) paths —
    // both consumers guard on their respective configuration flags.
    private bool IsNullableReferenceType =>
        (IsNullable || (DefaultValue != null && !IsRequired))
        && (!IsEnumerable || !IsPrivateSetter)
        && (PropertyType is ClassModel || (PropertyType is SimpleModel model && !model.ValueType.IsValueType));

    private bool IsNillableValueType => IsNillable && !IsEnumerable && IsValueType;

    private bool IsList => Type.XmlSchemaType?.Datatype?.Variety == XmlSchemaDatatypeVariety.List;

    private bool IsPrivateSetter => IsEnumerable && Configuration.CollectionSettersMode == CollectionSettersMode.Private;

    private CodeTypeReference TypeReference => PropertyType.GetReferenceFor(OwningType.Namespace, collection: IsEnumerable, attribute: IsAttribute);

    private void AddDocs(CodeTypeMember member)
    {
        var docs = new List<DocumentationModel>(Documentation);

        AddDescription(member.CustomAttributes, docs);

        if (PropertyType is SimpleModel simpleType && !IsEnumerable)
        {
            docs.AddRange(simpleType.Documentation);
            docs.AddRange(simpleType.Restrictions.Select(r => new DocumentationModel { Language = English, Text = r.Description }));
            member.CustomAttributes.AddRange(simpleType.GetRestrictionAttributes().ToArray());
        }

        member.Comments.AddRange(GetComments(docs).ToArray());

        if (RenamedFrom != null)
        {
            member.Comments.Add(new CodeCommentStatement("<remarks>", true));
            member.Comments.Add(new CodeCommentStatement($"This property was renamed from <c>{RenamedFrom}</c> to <c>{Name}</c> to avoid a collision with an existing member.", true));
            member.Comments.Add(new CodeCommentStatement("</remarks>", true));
        }
    }

    private CodeAttributeDeclaration CreateDefaultValueAttribute(CodeTypeReference typeReference, CodeExpression defaultValueExpression)
    {
        var defaultValueAttribute = AttributeDecl<DefaultValueAttribute>();

        defaultValueAttribute.Arguments.AddRange(typeReference.BaseType == typeof(decimal).FullName
            ? [new(new CodeTypeOfExpression(typeof(decimal))), new(new CodePrimitiveExpression(DefaultValue))]
            : new CodeAttributeArgument[] { new(defaultValueExpression) });

        return defaultValueAttribute;
    }

    public void AddInterfaceMembersTo(CodeTypeDeclaration typeDeclaration)
    {
        CodeTypeMember member;

        var propertyType = PropertyType;
        var isNullableValueType = IsNullableValueType;
        var isPrivateSetter = IsPrivateSetter;
        var typeReference = TypeReference;

        if ((isNullableValueType || IsNillableValueType) && Configuration.GenerateNullables)
            typeReference = NullableTypeRef(typeReference);

        // Apply nullable reference type syntax on interface members to match
        // the implementing class (which uses '?' when EnableNullableDirective is on).
        // WrapTypeRef renders via CSharpCodeProvider then creates a literal CodeTypeReference,
        // so it works for all types including generics (List<string>?) and arrays (byte[]?).
        if (IsNullableReferenceType && Configuration.EnableNullableDirective)
        {
            typeReference = WrapTypeRef(typeReference, suffix: "?");
        }

        member = new CodeMemberProperty
        {
            Name = Name,
            Type = typeReference,
            HasGet = true,
            HasSet = !isPrivateSetter && !(FixedValue != null && Configuration.GenerateStrictFixedValues)
        };

        if (DefaultValue != null && !IsRequired)
        {
            var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);

            if ((defaultValueExpression is CodePrimitiveExpression or CodeFieldReferenceExpression) && !CodeUtilities.IsXmlLangOrSpace(XmlSchemaName)
                && !(IsNullableReferenceType && Configuration.EnableNullableDirective))
            {
                var defaultValueAttribute = CreateDefaultValueAttribute(typeReference, defaultValueExpression);
                member.CustomAttributes.Add(defaultValueAttribute);
            }
        }

        typeDeclaration.Members.Add(member);

        AddDocs(member);
    }

    // ReSharper disable once FunctionComplexityOverflow
    public void AddMembersTo(CodeTypeDeclaration typeDeclaration, bool withDataBinding)
    {
        // Note: We use CodeMemberField because CodeMemberProperty doesn't allow for private set
        var member = new CodeMemberField() { Name = Name };

        var isArray = IsArray;
        var isEnumerable = IsEnumerable;
        var propertyType = PropertyType;
        var isNullableValueType = IsNullableValueType;
        var typeReference = TypeReference;

        CodeAttributeDeclaration ignoreAttribute = new(TypeRef<XmlIgnoreAttribute>());
        CodeAttributeDeclaration notMappedAttribute = new(CodeUtilities.CreateTypeReference(Attributes.NotMapped, Configuration));

        // When strict fixed values is on and this property has a fixed value,
        // we always need a backing field to hold the initialized value, even if
        // DefaultValue is null (which is the case for optional fixed elements).
        var needsStrictFixed = FixedValue != null && Configuration.GenerateStrictFixedValues;

        CodeMemberField backingField = null;
        if (withDataBinding || DefaultValue != null || isEnumerable || needsStrictFixed)
        {
            backingField = IsNillableValueType
                ? new CodeMemberField(NullableTypeRef(typeReference), OwningType.GetUniqueFieldName(this))
                : new CodeMemberField(typeReference, OwningType.GetUniqueFieldName(this)) { Attributes = MemberAttributes.Private };
            backingField.CustomAttributes.Add(ignoreAttribute);
            typeDeclaration.Members.Add(backingField);
        }

        if (needsStrictFixed)
        {
            // Fixed value under strict mode: emit a getter-only property initialized
            // to the fixed value. Callers cannot overwrite it at compile time.
            // XmlSerializer will still serialize it (reads the getter) but silently
            // skips it during deserialization (no setter).
            var fixedExpression = propertyType.GetDefaultValueFor(FixedValue, IsAttribute);
            backingField.InitExpression = fixedExpression;

            member.Type = IsNillableValueType ? NullableTypeRef(typeReference) : typeReference;
            member.Name += CodeUtilities.NormalizeNewlines($@"
        {{
            get
            {{
                return {backingField.Name};
            }}
        }}");
        }
        else if (DefaultValue == null || isEnumerable)
        {
            if (isNullableValueType && Configuration.GenerateNullables && !(Configuration.UseShouldSerializePattern && !IsAttribute))
                member.Name += Value;

            if (IsNillableValueType)
            {
                member.Type = NullableTypeRef(typeReference);
            }
            else if (isNullableValueType && !IsAttribute && Configuration.UseShouldSerializePattern)
            {
                member.Type = NullableTypeRef(typeReference);

                typeDeclaration.Members.Add(new CodeMemberMethod
                {
                    Attributes = MemberAttributes.Public,
                    Name = "ShouldSerialize" + member.Name,
                    ReturnType = new CodeTypeReference(typeof(bool)),
                    Statements = { new CodeSnippetExpression($"return {member.Name}.{HasValue}") }
                });
            }
            else
            {
                member.Type = typeReference;
            }

            var propertyValueTypeCode = IsCollection || isArray ? PropertyValueTypeCode.Array : propertyType.GetPropertyValueTypeCode();
            var setter = Configuration.CollectionSettersMode switch
            {
                CollectionSettersMode.Private when IsEnumerable => "private set",
                CollectionSettersMode.Init or CollectionSettersMode.InitWithoutConstructorInitialization when IsEnumerable => "init",
                _ => "set"
            };
            member.Name += GetAccessors(backingField, withDataBinding, propertyValueTypeCode, setter);
        }
        else
        {
            var defaultValueExpression = propertyType.GetDefaultValueFor(DefaultValue, IsAttribute);

            backingField?.InitExpression = defaultValueExpression;

            member.Type = IsNillableValueType ? NullableTypeRef(typeReference) : typeReference;

            member.Name += GetAccessors(backingField, withDataBinding, propertyType.GetPropertyValueTypeCode());

            if (!IsRequired && (defaultValueExpression is CodePrimitiveExpression or CodeFieldReferenceExpression) && !CodeUtilities.IsXmlLangOrSpace(XmlSchemaName)
                && !(IsNullableReferenceType && Configuration.EnableNullableDirective))
                member.CustomAttributes.Add(CreateDefaultValueAttribute(typeReference, defaultValueExpression));
        }

        // Emit the C# 11 'required' modifier for required properties.
        // CodeDom renders a CodeMemberField as: <access> <type> <name>;
        // By prepending "required " to the type name we get: public required <type> <name> { get; set; }
        // Skip for strict fixed values — read-only properties cannot be 'required'.
        if (IsRequired && !IsEnumerable && Configuration.GenerateRequiredModifier && !needsStrictFixed)
        {
            // Clone the type reference so we don't also modify the backing field's type
            // (member.Type may be the same object as backingField.Type when both point to typeReference).
            member.Type = WrapTypeRef(member.Type, prefix: "required ");
        }

        member.Attributes = MemberAttributes.Public;
        typeDeclaration.Members.Add(member);

        AddDocs(member);

        // Emit [Required] for DataAnnotations validation unless the C# 11 'required'
        // modifier is active, which provides strictly stronger compile-time enforcement.
        // Skip for strict fixed values — the value is immutable, so requiring it is meaningless.
        if (IsRequired && Configuration.DataAnnotationMode != DataAnnotationMode.None && !Configuration.GenerateRequiredModifier && !needsStrictFixed)
        {
            var requiredAttribute = new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.Required, Configuration));
            var noEmptyStrings = propertyType is SimpleModel simpleModel
                && simpleModel.Restrictions.Any(r => r is MinLengthRestrictionModel m && m.Value > 0);
            var allowEmptyStringsArgument = new CodeAttributeArgument("AllowEmptyStrings", new CodePrimitiveExpression(!noEmptyStrings));
            requiredAttribute.Arguments.Add(allowEmptyStringsArgument);
            member.CustomAttributes.Add(requiredAttribute);
        }

        if (IsDeprecated)
        {
            // From .NET 3.5 XmlSerializer doesn't serialize objects with [Obsolete] >(
        }

        if (isNullableValueType)
        {
            bool generateNullablesProperty = Configuration.GenerateNullables;
            bool generateSpecifiedProperty = true;

            if (generateNullablesProperty && Configuration.UseShouldSerializePattern && !IsAttribute)
            {
                generateNullablesProperty = false;
                generateSpecifiedProperty = false;
            }

            var specifiedName = generateNullablesProperty ? Name + Value : Name;
            CodeMemberField specifiedMember = null;
            if (generateSpecifiedProperty)
            {
                specifiedMember = new CodeMemberField(typeof(bool), specifiedName + Specified + GetAccessors());
                specifiedMember.CustomAttributes.Add(ignoreAttribute);
                if (Configuration.EntityFramework && generateNullablesProperty) { specifiedMember.CustomAttributes.Add(notMappedAttribute); }
                specifiedMember.Attributes = MemberAttributes.Public;
                var specifiedDocs = new DocumentationModel[] {
                    new() { Language = English, Text = $"Gets or sets a value indicating whether the {Name} property is specified." },
                    new() { Language = German, Text = $"Ruft einen Wert ab, der angibt, ob die {Name}-Eigenschaft spezifiziert ist, oder legt diesen fest." }
                };
                specifiedMember.Comments.AddRange(GetComments(specifiedDocs).ToArray());
                typeDeclaration.Members.Add(specifiedMember);

                var specifiedMemberPropertyModel = new PropertyModel(Configuration, specifiedName + Specified, null, null);

                Configuration.MemberVisitor(specifiedMember, specifiedMemberPropertyModel);
            }

            if (generateNullablesProperty)
            {
                var nullableMember = new CodeMemberProperty
                {
                    Type = NullableTypeRef(typeReference),
                    Name = Name,
                    HasSet = true,
                    HasGet = true,
                    Attributes = MemberAttributes.Public | MemberAttributes.Final,
                };
                nullableMember.CustomAttributes.Add(ignoreAttribute);
                nullableMember.Comments.AddRange(member.Comments);

                var specifiedExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), specifiedName + Specified);
                var valueExpression = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name + Value);
                var conditionStatement = new CodeConditionStatement(specifiedExpression,
                    [new CodeMethodReturnStatement(valueExpression)],
                    [new CodeMethodReturnStatement(new CodePrimitiveExpression(null))]);
                nullableMember.GetStatements.Add(conditionStatement);

                var getValueOrDefaultExpression = new CodeMethodInvokeExpression(new CodePropertySetValueReferenceExpression(), nameof(Nullable<int>.GetValueOrDefault));
                var setValueStatement = new CodeAssignStatement(valueExpression, getValueOrDefaultExpression);
                var hasValueExpression = new CodePropertyReferenceExpression(new CodePropertySetValueReferenceExpression(), HasValue);
                var setSpecifiedStatement = new CodeAssignStatement(specifiedExpression, hasValueExpression);

                var statements = new List<CodeStatement>();
                if (withDataBinding)
                {
                    var ifNotEquals = new CodeConditionStatement(
                        new CodeBinaryOperatorExpression(
                            new CodeBinaryOperatorExpression(
                                new CodeMethodInvokeExpression(valueExpression, EqualsMethod, getValueOrDefaultExpression),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodePrimitiveExpression(false)
                                ),
                            CodeBinaryOperatorType.BooleanOr,
                            new CodeBinaryOperatorExpression(
                                new CodeMethodInvokeExpression(specifiedExpression, EqualsMethod, hasValueExpression),
                                CodeBinaryOperatorType.ValueEquality,
                                new CodePrimitiveExpression(false)
                                )
                        ),
                        setValueStatement,
                        setSpecifiedStatement,
                        new CodeExpressionStatement(new CodeMethodInvokeExpression(null, OnPropertyChanged,
                            new CodePrimitiveExpression(Name)))
                        );
                    statements.Add(ifNotEquals);
                }
                else
                {
                    statements.Add(setValueStatement);
                    statements.Add(setSpecifiedStatement);
                }

                nullableMember.SetStatements.AddRange(statements.ToArray());

                typeDeclaration.Members.Add(nullableMember);

                var editorBrowsableAttribute = AttributeDecl<EditorBrowsableAttribute>();
                editorBrowsableAttribute.Arguments.Add(new(new CodeFieldReferenceExpression(TypeRefExpr<EditorBrowsableState>(), nameof(EditorBrowsableState.Never))));
                specifiedMember?.CustomAttributes.Add(editorBrowsableAttribute);
                member.CustomAttributes.Add(editorBrowsableAttribute);
                if (Configuration.EntityFramework) { member.CustomAttributes.Add(notMappedAttribute); }

                Configuration.MemberVisitor(nullableMember, this);
            }
        }
        else if (isEnumerable && !IsRequired)
        {
            var canBeNull = Configuration.CollectionSettersMode is CollectionSettersMode.PublicWithoutConstructorInitialization or CollectionSettersMode.Public or CollectionSettersMode.Init or CollectionSettersMode.InitWithoutConstructorInitialization;

            if (canBeNull || !Configuration.SerializeEmptyCollections)
            {
                var listReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
                var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;
                var countProperty = collectionType == typeof(Array) ? nameof(Array.Length) : nameof(List<int>.Count);
                var countReference = new CodePropertyReferenceExpression(listReference, countProperty);
                var notZeroExpression = new CodeBinaryOperatorExpression(countReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(0));
                var returnExpression = notZeroExpression;

                if (canBeNull)
                {
                    var notNullExpression = new CodeBinaryOperatorExpression(listReference, CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(null));
                    var notNullOrEmptyExpression = new CodeBinaryOperatorExpression(notNullExpression, CodeBinaryOperatorType.BooleanAnd, notZeroExpression);
                    returnExpression = Configuration.SerializeEmptyCollections ? notNullExpression : notNullOrEmptyExpression;
                }

                var returnStatement = new CodeMethodReturnStatement(returnExpression);

                if (Configuration.UseShouldSerializePattern)
                {
                    var shouldSerializeMethod = new CodeMemberMethod
                    {
                        Attributes = MemberAttributes.Public,
                        Name = "ShouldSerialize" + Name,
                        ReturnType = new CodeTypeReference(typeof(bool)),
                        Statements = { returnStatement }
                    };

                    Configuration.MemberVisitor(shouldSerializeMethod, this);

                    typeDeclaration.Members.Add(shouldSerializeMethod);
                }
                else
                {
                    var specifiedProperty = new CodeMemberProperty
                    {
                        Type = TypeRef<bool>(),
                        Name = Name + Specified,
                        HasSet = false,
                        HasGet = true,
                    };
                    specifiedProperty.CustomAttributes.Add(ignoreAttribute);
                    if (Configuration.EntityFramework) { specifiedProperty.CustomAttributes.Add(notMappedAttribute); }
                    specifiedProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                    specifiedProperty.GetStatements.Add(returnStatement);

                    var specifiedDocs = new DocumentationModel[] {
                    new() { Language = English, Text = $"Gets a value indicating whether the {Name} collection is empty." },
                    new() { Language = German, Text = $"Ruft einen Wert ab, der angibt, ob die {Name}-Collection leer ist." }
                };
                    specifiedProperty.Comments.AddRange(GetComments(specifiedDocs).ToArray());

                    Configuration.MemberVisitor(specifiedProperty, this);

                    typeDeclaration.Members.Add(specifiedProperty);
                }
            }
        }

        if (IsNullableReferenceType)
        {
            if (Configuration.EnableNullableDirective)
            {
                // Use native nullable reference type syntax: append ? to the type name.
                // WrapTypeRef renders via CSharpCodeProvider then creates a literal CodeTypeReference,
                // so it works for ALL types including generics (List<string>?) and arrays (byte[]?).
                member.Type = WrapTypeRef(member.Type, suffix: "?");

                // Also make the backing field nullable so the setter assignment is null-safe.
                if (backingField != null)
                {
                    backingField.Type = WrapTypeRef(backingField.Type, suffix: "?");
                }
            }
            else if (Configuration.EnableNullableReferenceAttributes)
            {
                member.CustomAttributes.Add(new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.AllowNull, Configuration)));
                member.CustomAttributes.Add(new CodeAttributeDeclaration(CodeUtilities.CreateTypeReference(Attributes.MaybeNull, Configuration)));
            }
        }

        var attributes = GetAttributes(isArray).ToArray();

        // For xsd:list element properties with EnumCollection, XmlSerializer cannot natively
        // serialize a collection as a single element with space-separated values. It would emit
        // one <Element> per item instead. Fix: make the typed collection [XmlIgnore] and add a
        // string proxy property with the [XmlElement] attribute that converts between the typed
        // collection and the space-separated string using the [XmlEnum] attribute names.
        var enumListItemType = IsList && !IsAttribute && Configuration.EnumCollection && propertyType is SimpleModel listSimpleModel
            ? listSimpleModel.EnumListItemType : null;

        if (enumListItemType != null)
        {
            // The typed collection member becomes [XmlIgnore] — it's the programmatic API.
            member.CustomAttributes.Add(ignoreAttribute);

            // The proxy setter always assigns null to the backing field when the input is
            // null/empty. Under #nullable enable both the backing field and the public
            // collection property must be nullable, even when IsNullableReferenceType is
            // false (e.g. required enum-list properties), because the getter returns the
            // backing field directly and the setter nulls it out.
            if (Configuration.EnableNullableDirective && backingField != null
                && !backingField.Type.BaseType.TrimEnd().EndsWith("?"))
            {
                backingField.Type = WrapTypeRef(backingField.Type, suffix: "?");
                member.Type = WrapTypeRef(member.Type, suffix: "?");
            }

            // Get the enum type name and values for the proxy accessor code.
            var enumTypeRef = enumListItemType.GetReferenceFor(OwningType.Namespace);
            var enumTypeName = TypeModel.GetCSharpTypeOutput(enumTypeRef);
            var enumValues = ((EnumModel)enumListItemType).Values;

            var backingFieldName = backingField != null ? backingField.Name : $"this.{Name}";
            var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;
            var isArrayCollection = collectionType == typeof(Array);
            var countMember = isArrayCollection ? "Length" : "Count";

            // Build switch arms: enum member → XML name, and XML name → enum member.
            var toStringArms = string.Join("\n                    ",
                enumValues.Select(v => $@"{enumTypeName}.{v.Name} => ""{v.Value}"","));
            var fromStringArms = string.Join("\n                    ",
                enumValues.Select(v => $@"""{v.Value}"" => {enumTypeName}.{v.Name},"));

            // Getter: convert each enum value to its XML name via a switch expression
            // and join them with spaces.
            var getterCode = $@"
                if ({backingFieldName} == null || {backingFieldName}.{countMember} == 0) return null;
                return string.Join("" "", System.Linq.Enumerable.Select({backingFieldName}, item => item switch
                {{
                    {toStringArms}
                    _ => item.ToString()
                }}));";

            // Setter: split space-separated XML names and convert each to an enum value
            // via a switch expression, then assign directly to the backing field.
            var collectionImplName = SimpleModel.GetCollectionImplementationName(enumTypeName, Configuration);

            // Build the assignment that materializes the parsed enumerable into the
            // configured collection type. List<T> and HashSet<T> accept IEnumerable<T>
            // directly, but Collection<T> requires IList<T>, so we materialize through
            // a List<T> intermediate for that case.
            var collectionImplType = collectionType.IsGenericType ? collectionType.GetGenericTypeDefinition() : collectionType;
            var listIntermediate = $"new System.Collections.Generic.List<{enumTypeName}>(parsed)";

            string setterAssignment;
            if (isArrayCollection)
                setterAssignment = $"{backingFieldName} = System.Linq.Enumerable.ToArray(parsed);";
            else if (collectionImplType == typeof(List<>))
                setterAssignment = $"{backingFieldName} = {listIntermediate};";
            else
                setterAssignment = $"{backingFieldName} = new {collectionImplName}({listIntermediate});";

            var setterCode = $@"
                if (string.IsNullOrEmpty(value)) {{ {backingFieldName} = null; return; }}
                var parsed = System.Linq.Enumerable.Select(value.Split(' '), part => part switch
                {{
                    {fromStringArms}
                    _ => throw new System.ArgumentException($""Unknown value '{{part}}' for {enumTypeName}"")
                }});
                {setterAssignment}";

            var proxyAccessors = CodeUtilities.NormalizeNewlines($@"
        {{
            get
            {{{getterCode}
            }}
            set
            {{{setterCode}
            }}
        }}");

            // Use string? when #nullable enable is active since the getter returns null for empty collections.
            CodeTypeReference proxyTypeRef = Configuration.EnableNullableDirective
                ? CreateLiteralTypeRef("string?")
                : new CodeTypeReference(typeof(string));

            var proxyMember = new CodeMemberField(proxyTypeRef, Name + "Xml" + proxyAccessors)
            {
                Attributes = MemberAttributes.Public
            };
            proxyMember.CustomAttributes.AddRange(attributes);

            // Hide the proxy from IntelliSense
            var editorBrowsableAttr = AttributeDecl<EditorBrowsableAttribute>();
            editorBrowsableAttr.Arguments.Add(new(new CodeFieldReferenceExpression(TypeRefExpr<EditorBrowsableState>(), nameof(EditorBrowsableState.Never))));
            proxyMember.CustomAttributes.Add(editorBrowsableAttr);

            typeDeclaration.Members.Add(proxyMember);
            Configuration.MemberVisitor(proxyMember, this);
        }
        else
        {
            member.CustomAttributes.AddRange(attributes);
        }

        // initialize List<>
        if (isEnumerable && (Configuration.CollectionSettersMode != CollectionSettersMode.PublicWithoutConstructorInitialization)
            && (Configuration.CollectionSettersMode != CollectionSettersMode.InitWithoutConstructorInitialization))
        {
            var constructor = typeDeclaration.Members.OfType<CodeConstructor>().FirstOrDefault();

            if (constructor == null)
            {
                constructor = new CodeConstructor { Attributes = MemberAttributes.Public | MemberAttributes.Final };
                var constructorDocs = new DocumentationModel[] {
                    new() { Language = English, Text = $@"Initializes a new instance of the <see cref=""{typeDeclaration.Name}"" /> class." },
                    new() { Language = German, Text = $@"Initialisiert eine neue Instanz der <see cref=""{typeDeclaration.Name}"" /> Klasse." }
                };
                constructor.Comments.AddRange(GetComments(constructorDocs).ToArray());
                typeDeclaration.Members.Add(constructor);
            }

            CodeExpression listReference = backingField != null
                ? new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), backingField.Name)
                : new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), Name);
            var collectionType = Configuration.CollectionImplementationType ?? Configuration.CollectionType;

            CodeExpression initExpression;

            if (collectionType == typeof(Array))
            {
                var initTypeReference = propertyType.GetReferenceFor(OwningType.Namespace, collection: false, forInit: true, attribute: IsAttribute);
                initExpression = new CodeMethodInvokeExpression(new(TypeRefExpr<Array>(), nameof(Array.Empty), initTypeReference));
            }
            else
            {
                var initTypeReference = propertyType.GetReferenceFor(OwningType.Namespace, collection: true, forInit: true, attribute: IsAttribute);
                initExpression = new CodeObjectCreateExpression(initTypeReference);
            }

            constructor.Statements.Add(new CodeAssignStatement(listReference, initExpression));
        }

        if (isArray)
        {
            var arrayItemProperty = TypeClassModel.Properties[0];

            // HACK: repackage as ArrayItemAttribute
            foreach (var propertyAttribute in arrayItemProperty.GetAttributes(false, OwningType).ToList())
            {
                var arrayItemAttribute = AttributeDecl<XmlArrayItemAttribute>(
                    [.. propertyAttribute.Arguments.Cast<CodeAttributeArgument>().Where(x => !string.Equals(x.Name, nameof(Order), StringComparison.Ordinal))]);
                var namespacePresent = arrayItemAttribute.Arguments.OfType<CodeAttributeArgument>().Any(a => a.Name == Namespace);
                if (!namespacePresent && !arrayItemProperty.XmlSchemaName.IsEmpty && !string.IsNullOrEmpty(arrayItemProperty.XmlSchemaName.Namespace))
                    arrayItemAttribute.Arguments.Add(new(Namespace, new CodePrimitiveExpression(arrayItemProperty.XmlSchemaName.Namespace)));
                member.CustomAttributes.Add(arrayItemAttribute);
            }
        }

        if (IsKey)
            member.CustomAttributes.Add(new(CodeUtilities.CreateTypeReference(Attributes.Key, Configuration)));

        if (IsAny && Configuration.EntityFramework)
            member.CustomAttributes.Add(notMappedAttribute);

        if (ChoiceGroupId.HasValue && Configuration.GenerateChoiceGroupAttributes)
        {
            var attrTypeRef = new CodeTypeReference("XmlChoiceGroupAttribute");
            var choiceAttr = new CodeAttributeDeclaration(
                attrTypeRef,
                new CodeAttributeArgument(new CodePrimitiveExpression(ChoiceGroupId.Value)),
                new CodeAttributeArgument(new CodePrimitiveExpression(ChoiceArmId ?? 0)));
            member.CustomAttributes.Add(choiceAttr);
        }

        Configuration.MemberVisitor(member, this);
    }

    private IEnumerable<CodeAttributeDeclaration> GetAttributes(bool isArray, TypeModel owningType = null)
    {
        var attributes = new List<CodeAttributeDeclaration>();

        if (IsKey && XmlSchemaName == null)
        {
            attributes.Add(AttributeDecl<XmlIgnoreAttribute>());
            return attributes;
        }

        if (IsAttribute)
        {
            if (IsAny)
            {
                var anyAttribute = AttributeDecl<XmlAnyAttributeAttribute>();
                attributes.Add(anyAttribute);
            }
            else
            {
                attributes.Add(AttributeDecl<XmlAttributeAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name))));
            }
        }
        else if (!isArray)
        {
            if (IsAny)
            {
                var anyAttribute = AttributeDecl<XmlAnyElementAttribute>();
                if (Order != null)
                    anyAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
                attributes.Add(anyAttribute);
            }
            else
            {
                if (!Configuration.SeparateSubstitutes && Substitutes.Count > 0)
                {
                    owningType ??= OwningType;

                    foreach (var substitute in Substitutes)
                    {
                        var substitutedAttribute = AttributeDecl<XmlElementAttribute>(
                            new(new CodePrimitiveExpression(substitute.Element.QualifiedName.Name)),
                            new(nameof(XmlElementAttribute.Type), new CodeTypeOfExpression(substitute.Type.GetReferenceFor(owningType.Namespace))),
                            new(nameof(XmlElementAttribute.Namespace), new CodePrimitiveExpression(substitute.Element.QualifiedName.Namespace)));

                        if (Order != null)
                            substitutedAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));

                        attributes.Add(substitutedAttribute);
                    }
                }

                var attribute = AttributeDecl<XmlElementAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
                if (Order != null)
                    attribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
                attributes.Add(attribute);
            }
        }
        else
        {
            var arrayAttribute = AttributeDecl<XmlArrayAttribute>(new CodeAttributeArgument(new CodePrimitiveExpression(XmlSchemaName.Name)));
            if (Order != null)
                arrayAttribute.Arguments.Add(new(nameof(Order), new CodePrimitiveExpression(Order.Value)));
            attributes.Add(arrayAttribute);
        }

        foreach (var args in attributes.Select(a => a.Arguments))
        {
            bool namespacePrecalculated = args.OfType<CodeAttributeArgument>().Any(a => a.Name == Namespace);
            if (!namespacePrecalculated)
            {
                if (XmlNamespace != null)
                    args.Add(new(Namespace, new CodePrimitiveExpression(XmlNamespace)));

                if (Form == XmlSchemaForm.Qualified && IsAttribute)
                {
                    if (XmlNamespace == null)
                        args.Add(new(Namespace, new CodePrimitiveExpression(OwningType.XmlSchemaName.Namespace)));

                    args.Add(new(nameof(Form), new CodeFieldReferenceExpression(TypeRefExpr<XmlSchemaForm>(), nameof(XmlSchemaForm.Qualified))));
                }
                else if ((Form == XmlSchemaForm.Unqualified || Form == XmlSchemaForm.None) && !IsAttribute && !IsAny && XmlNamespace == null)
                {
                    args.Add(new(nameof(Form), new CodeFieldReferenceExpression(TypeRefExpr<XmlSchemaForm>(), nameof(XmlSchemaForm.Unqualified))));
                }
            }

            if (IsNillable && !(IsCollection && Type is SimpleModel m && m.ValueType.IsValueType) && (IsRequired || !Configuration.DoNotForceIsNullable))
                args.Add(new("IsNullable", new CodePrimitiveExpression(true)));

            if (Type is SimpleModel simpleModel && simpleModel.UseDataTypeAttribute)
            {
                // walk up the inheritance chain to find DataType if the simple type is derived (see #18)
                var xmlSchemaType = Type.XmlSchemaType;
                while (xmlSchemaType != null)
                {
                    var qualifiedName = xmlSchemaType.GetQualifiedName();

                    if (qualifiedName.Namespace == XmlSchema.Namespace && qualifiedName.Name != "anySimpleType")
                    {
                        args.Add(new("DataType", new CodePrimitiveExpression(qualifiedName.Name)));
                        break;
                    }
                    else
                    {
                        xmlSchemaType = xmlSchemaType.BaseXmlSchemaType;
                    }
                }
            }
        }

        return attributes;
    }
}
