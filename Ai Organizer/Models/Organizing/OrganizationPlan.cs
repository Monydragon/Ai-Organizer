using System.Collections.Generic;

namespace Ai_Organizer.Models.Organizing;

public sealed class OrganizationPlan
{
    public List<PlanItem> Items { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}


