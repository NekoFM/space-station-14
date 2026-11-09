using Robust.Shared.Prototypes;

namespace Content.Server.SS220.RedWings;

[RegisterComponent]
public sealed partial class RedWingsClientPaperComponent : Component
{
    private const int DefaultClientAmount = 4;
    private static readonly string[] DefaultForbiddenDepartment = ["Command", "Security"];
    private static readonly string[] DefaultForbiddenSpecies = ["Ipc"];

    [DataField]
    public int ClientAmount = DefaultClientAmount;

    [DataField]
    public string[] ForbiddenDepartment = DefaultForbiddenDepartment;

    [DataField]
    public string[] ForbiddenSpecies = DefaultForbiddenSpecies;
}