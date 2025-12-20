using System.Text.Json.Serialization;

namespace Ai_Organizer.Models.Organizing;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlanAction
{
    Skip = 0,
    Move = 1,
    Copy = 2
}


