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
    /// <summary>The choice group ID, or null if not inside a choice.</summary>
    public int? GroupId { get; private set; }

    /// <summary>The current arm ID within the choice group.</summary>
    public int CurrentArmId { get; private set; }

    /// <summary>Whether we are currently inside a choice compositor.</summary>
    public bool IsInsideChoice => GroupId.HasValue;

    /// <summary>
    /// Whether the immediate parent compositor is a choice (as opposed to a sequence/all
    /// that is itself inside a choice). Only directly nested choices should be flattened.
    /// </summary>
    public bool IsDirectlyInsideChoice { get; private set; }

    /// <summary>
    /// Enter a new choice group (top-level choice, not nested).
    /// Returns a new context with the given group ID and arm counter starting at 0.
    /// </summary>
    public ChoiceContext EnterChoice(int groupId)
    {
        return new ChoiceContext { GroupId = groupId, CurrentArmId = 0, IsDirectlyInsideChoice = true };
    }

    /// <summary>
    /// Create a context that preserves choice group/arm metadata but marks that we've
    /// entered a non-choice compositor (sequence/all/group ref). This prevents nested
    /// choices from being flattened into the outer group.
    /// </summary>
    public ChoiceContext EnterNonChoiceCompositor()
    {
        return new ChoiceContext { GroupId = GroupId, CurrentArmId = CurrentArmId, IsDirectlyInsideChoice = false };
    }

    /// <summary>
    /// Create a context for expanding a group ref that is inside a choice.
    /// The elements inside the group inherit the choice metadata, but any choices
    /// within the group are independent (not flattened).
    /// </summary>
    public static ChoiceContext ForGroupRef(int groupId, int armId)
    {
        return new ChoiceContext { GroupId = groupId, CurrentArmId = armId, IsDirectlyInsideChoice = false };
    }

    /// <summary>
    /// Advance the arm counter (called after processing each non-choice child of a choice).
    /// </summary>
    public void AdvanceArm()
    {
        CurrentArmId++;
    }
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
    public int? ChoiceGroupId { get; set; }

    /// <summary>
    /// Identifies which arm within a choice group this particle belongs to.
    /// Elements in the same arm (e.g. from a sequence within a choice) share the same arm ID.
    /// </summary>
    public int? ChoiceArmId { get; set; }
}
