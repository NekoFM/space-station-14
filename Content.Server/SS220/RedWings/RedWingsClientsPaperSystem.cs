using Content.Shared.Paper;
using Content.Server.Paper;
using Robust.Shared.Random;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Content.Shared.Station;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Robust.Server.Player;
using Content.Shared.Mind;
using Robust.Shared.Enums;
using Robust.Shared.Collections;

namespace Content.Server.SS220.RedWings;

public sealed partial class RedWingsClientPaperSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RedWingsClientPaperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RedWingsClientPaperComponent component, MapInitEvent args)
    {
        SetupPaper(uid, component);
    }

    private void SetupPaper(EntityUid uid, RedWingsClientPaperComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (TryComp(uid, out PaperComponent? paperComp))
        {
            if (TryGetClientList(component, out var paperContent))
            {
                _paper.SetContent((uid, paperComp), paperContent);
            }
        }
    }

    private bool TryGetClientList(RedWingsClientPaperComponent component, [NotNullWhen(true)] out string? redWingsClientList)
    {
        redWingsClientList = null;
        var clientAmount = component.ClientAmount;
        var forbiddenDepartment = component.ForbiddenDepartment;
        var forbiddenSpecies = component.ForbiddenSpecies;
        var clientMessage = new FormattedMessage();

        if (_station.GetStations().FirstOrNull() is not { } station)
            return false;

        var allRecords = _stationRecords.GetRecordsOfType<GeneralStationRecord>(station).ToList();

        var forbiddenJobIds = new HashSet<string>();
        foreach (var deptId in forbiddenDepartment)
        {
            if (_prototypeManager.TryIndex<DepartmentPrototype>(deptId, out var dept))
            {
                foreach (var jobId in dept.Roles)
                {
                    forbiddenJobIds.Add(jobId);
                }
            }
        }

        var idToEntity = new Dictionary<uint, EntityUid>();
        var query = EntityQueryEnumerator<StationRecordKeyStorageComponent>();
        while (query.MoveNext(out var entityUid, out var keyStorage))
        {
            if (keyStorage.Key != null)
                idToEntity[keyStorage.Key.Value.Id] = entityUid;
        }

        var validRecords = new List<(uint, GeneralStationRecord)>();

        foreach (var record in allRecords)
        {
            var generalRecord = record.Item2;

            if (forbiddenJobIds.Contains(generalRecord.JobPrototype))
                continue;

            if (forbiddenSpecies.Contains(generalRecord.Species))
                continue;

            if (!idToEntity.TryGetValue(record.Item1, out var entityUid))
                continue;

            if (!_mind.TryGetMind(entityUid, out _, out var mindComp))
                continue;

            if (mindComp.UserId == null)
                continue;

            if (!_playerManager.TryGetSessionById(mindComp.UserId.Value, out var session))
                continue;

            if (session.Status != SessionStatus.Connected)
                continue;

            if (session.AttachedEntity == null || session.AttachedEntity.Value != entityUid)
                continue;

            validRecords.Add(record);
        }

        if (validRecords.Count == 0)
            return false;

        _random.Shuffle(validRecords);
        var selectedRecords = validRecords.Take(clientAmount).ToList();

        clientMessage.PushNewline();
        foreach (var record in selectedRecords)
        {
            var name = record.Item2.Name;
            var dna = record.Item2.DNA;

            clientMessage.PushNewline();
            clientMessage.AddMarkupPermissive(Loc.GetString("book-text-redwings-client-middle", ("dna", dna ?? ""), ("name", name)));
            clientMessage.PushNewline();
        }
        clientMessage.PushNewline();

        redWingsClientList = Loc.GetString("book-text-redwings-client-start") + clientMessage + Loc.GetString("book-text-redwings-client-end");

        return true;
    }
}
