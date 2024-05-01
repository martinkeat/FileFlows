using FileFlows.ServerShared.Models;

namespace FileFlows.Managers;

/// <summary>
/// Access control entry manager
/// </summary>
public class AccessControlManager : CachedManager<AccessControlEntry>
{
    /// <summary>
    /// Access control entries  do not need to update the configuration
    /// as they do not effect configuration on a Flow Runner
    /// </summary>
    public override bool IncrementsConfiguration => false;

    /// <summary>
    /// Moves access control entries
    /// </summary>
    /// <param name="uids">The UIDs to move</param>
    /// <param name="type">The type of entries being moved</param>
    /// <param name="up">If the items are being moved up or down</param>
    /// <param name="auditDetails">the audit details</param>
    public async Task Move(Guid[] uids, AccessControlType type, bool up, AuditDetails auditDetails)
    {
        if (uids?.Any() != true)
            return; // nothing to do

        var existing = (await GetData()).Where(x => x.Type == type).OrderBy(x => x.Order).ToList();

        List<AccessControlEntry> updating = new();

        // ensure these all have orders set and in the ascending order
        for (int i = 0; i < existing.Count; i++)
        {
            int order = i + 1;
            var item = existing[i];
            if (item.Order == order)
                continue;
            item.Order = order;
            updating.Add(item);
        }

        var moving = uids.Select(x => existing.FirstOrDefault(y => y.Uid == x))
            .Where(x => x != null)
            .Distinct()
            .Select(x => x!)
            .ToList();
        if (moving.Any() == false)
            return;

        if (up == false)
            moving.Reverse();

        // up we move the first first
        foreach (var item in moving)
        {
            int order = item.Order;
            var index = existing.IndexOf(item);
            if (up && index == 0)
                continue; // can not move
            if (up == false && index >= existing.Count - 1)
                continue; // can not move down

            var newIndex = up ? index - 1 : index + 1;
            int newOrder = up ? order - 1 : order + 1;
            var replacing = existing[newIndex];
            if (moving.Contains(replacing))
                continue; // we dont swap ones we are moving
            replacing.Order = order;
            item.Order = newOrder;
            existing[index] = replacing;
            existing[newIndex] = item;
            if (updating.Contains(item) == false)
                updating.Add(item);
            if (updating.Contains(replacing) == false)
                updating.Add(replacing);
        }

        foreach (var item in updating)
            await DatabaseAccessManager.Instance.FileFlowsObjectManager.AddOrUpdateObject(item, auditDetails);

        // refreshes the data if needed
        await Refresh();

    }
}