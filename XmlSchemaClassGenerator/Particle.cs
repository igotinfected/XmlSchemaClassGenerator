using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace XmlSchemaClassGenerator;

/// <summary>
/// Tracks choice group context during the recursive flattening of XSD compositors
/// (sequence/choice/all) into a flat list of particles.
/// </summary>
public class ChoiceContext
{
    public List<ChoiceGroupMembership> Memberships { get; private set; } = [];

    /// <summary>The choice group ID, or null if not inside a choice.</summary>
    public int? GroupId => Memberships.LastOrDefault()?.GroupId;

    /// <summary>The current arm ID within the choice group.</summary>
    public int CurrentArmId => Memberships.LastOrDefault()?.ArmId ?? 0;

    /// <summary>Whether we are currently inside a choice compositor.</summary>
    public bool IsInsideChoice => Memberships.Count > 0;

    /// <summary>
    /// Whether the immediate parent compositor is a choice (as opposed to a sequence/all
    /// that is itself inside a choice). Only directly nested choices should be flattened.
    /// </summary>
    public bool IsDirectlyInsideChoice { get; private set; }

    /// <summary>Whether all parent particles on the current path are required.</summary>
    public bool EffectiveIsRequired { get; private set; } = true;

    public bool IsRepeated { get; private set; }

    /// <summary>
    /// Enter a new choice group (top-level choice, not nested).
    /// Returns a new context with the given group ID and arm counter starting at 0.
    /// </summary>
    public ChoiceContext EnterChoice(int groupId, bool isRequired)
    {
        var effectiveIsRequired = EffectiveIsRequired && isRequired;
        return new ChoiceContext
        {
            Memberships = [.. Memberships, new ChoiceGroupMembership(groupId, 0, effectiveIsRequired)],
            IsDirectlyInsideChoice = true,
            EffectiveIsRequired = effectiveIsRequired,
            IsRepeated = IsRepeated,
        };
    }

    /// <summary>
    /// Create a context that preserves choice group/arm metadata but marks that we've
    /// entered a non-choice compositor (sequence/all/group ref). This prevents nested
    /// choices from being flattened into the outer group.
    /// </summary>
    public ChoiceContext EnterNonChoiceCompositor(bool isRequired, bool isRepeated = false)
    {
        var effectiveIsRequired = EffectiveIsRequired && isRequired;
        return new ChoiceContext
        {
            Memberships = [.. Memberships.Select(m => new ChoiceGroupMembership(m.GroupId, m.ArmId, m.IsRequired && effectiveIsRequired))],
            IsDirectlyInsideChoice = false,
            EffectiveIsRequired = effectiveIsRequired,
            IsRepeated = IsRepeated || isRepeated,
        };
    }

    /// <summary>
    /// Create a context for expanding a group ref that is inside a choice.
    /// The elements inside the group inherit the choice metadata, but any choices
    /// within the group are independent (not flattened).
    /// </summary>
    public static ChoiceContext ForGroupRef(IReadOnlyCollection<ChoiceGroupMembership> memberships, bool isRequired)
    {
        var effectiveIsRequired = memberships.All(m => m.IsRequired) && isRequired;
        return new ChoiceContext
        {
            Memberships = [.. memberships.Select(m => new ChoiceGroupMembership(m.GroupId, m.ArmId, m.IsRequired && effectiveIsRequired))],
            IsDirectlyInsideChoice = false,
            EffectiveIsRequired = effectiveIsRequired,
        };
    }

    /// <summary>
    /// Advance the arm counter (called after processing each non-choice child of a choice).
    /// </summary>
    public void AdvanceArm()
    {
        if (Memberships.Count == 0)
        {
            return;
        }

        var lastIndex = Memberships.Count - 1;
        var membership = Memberships[lastIndex];
        Memberships[lastIndex] = new ChoiceGroupMembership(membership.GroupId, membership.ArmId + 1, membership.IsRequired);
    }
}

public sealed class ChoiceGroupMembership
{
    public ChoiceGroupMembership(int groupId, int armId, bool isRequired)
    {
        GroupId = groupId;
        ArmId = armId;
        IsRequired = isRequired;
    }

    public int GroupId { get; }
    public int ArmId { get; }
    public bool IsRequired { get; }
}

public class Particle(XmlSchemaParticle particle, XmlSchemaObject parent)
{
    public XmlSchemaParticle XmlParticle { get; set; } = particle;
    public XmlSchemaObject XmlParent { get; } = parent;
    public decimal MaxOccurs { get; set; } = particle?.MaxOccurs ?? 1;
    public decimal MinOccurs { get; set; } = particle?.MinOccurs ?? 1;

    /// <summary>
    /// Identifies which choice group this particle belongs to, if any.
    /// Null means the particle is not part of a choice.
    /// </summary>
    public List<ChoiceGroupMembership> ChoiceGroupMemberships { get; set; } = [];

    public int? ChoiceGroupId
    {
        get => ChoiceGroupMemberships.LastOrDefault()?.GroupId;
        set
        {
            if (value.HasValue)
            {
                ChoiceGroupMemberships = [new ChoiceGroupMembership(value.Value, ChoiceArmId ?? 0, true)];
            }
            else
            {
                ChoiceGroupMemberships = [];
            }
        }
    }

    /// <summary>
    /// Identifies which arm within a choice group this particle belongs to.
    /// Elements in the same arm (e.g. from a sequence within a choice) share the same arm ID.
    /// </summary>
    public int? ChoiceArmId
    {
        get => ChoiceGroupMemberships.LastOrDefault()?.ArmId;
        set
        {
            if (ChoiceGroupMemberships.Count == 0)
            {
                return;
            }

            var lastIndex = ChoiceGroupMemberships.Count - 1;
            var membership = ChoiceGroupMemberships[lastIndex];
            ChoiceGroupMemberships[lastIndex] = new ChoiceGroupMembership(membership.GroupId, value ?? 0, membership.IsRequired);
        }
    }
}
