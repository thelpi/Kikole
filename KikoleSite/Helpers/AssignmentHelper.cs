using System;
using System.Collections.Generic;
using System.Linq;

namespace KikoleSite.Helpers;

/// <summary>
/// Affectation d'elements a des "places" : chaque element occupe au plus une place parmi
/// celles qui lui sont admissibles, chaque place a une capacite. Sert aux badges dont un
/// joueur peut remplir plusieurs roles (poste principal ou alternatif, pays principal ou
/// alternatif) sans jamais en remplir deux a la fois. C'est un couplage maximum dans un
/// graphe biparti (polynomial, pas NP-complet) : chemins augmentants de Kuhn, sur des
/// volumes minuscules (quelques centaines de joueurs, deux places admissibles au plus).
/// </summary>
internal static class AssignmentHelper
{
    /// <summary>
    /// Nombre maximal d'elements affectables. Une place absente de
    /// <paramref name="capacities"/> n'existe pas (ignoree) ; une capacite nulle revient au
    /// meme.
    /// </summary>
    internal static int MaxAssignments<TItem, TSlot>(
        IEnumerable<TItem> items,
        Func<TItem, IEnumerable<TSlot>> eligibleSlots,
        IReadOnlyDictionary<TSlot, int> capacities)
        where TSlot : notnull
    {
        var list = items.ToList();
        var eligible = list
            .Select(i => eligibleSlots(i).Where(capacities.ContainsKey).Distinct().ToList())
            .ToList();

        var assigned = capacities.Keys.ToDictionary(s => s, _ => new List<int>());

        var matched = 0;
        for (var i = 0; i < list.Count; i++)
        {
            if (TryAssign(i, eligible, capacities, assigned, []))
                matched++;
        }

        return matched;
    }

    private static bool TryAssign<TSlot>(
        int item,
        List<List<TSlot>> eligible,
        IReadOnlyDictionary<TSlot, int> capacities,
        Dictionary<TSlot, List<int>> assigned,
        HashSet<TSlot> visited)
        where TSlot : notnull
    {
        foreach (var slot in eligible[item])
        {
            if (!visited.Add(slot))
                continue;

            var occupants = assigned[slot];
            if (occupants.Count < capacities[slot])
            {
                occupants.Add(item);
                return true;
            }

            // place pleine : un occupant peut-il aller ailleurs pour liberer la place ?
            for (var k = 0; k < occupants.Count; k++)
            {
                if (TryAssign(occupants[k], eligible, capacities, assigned, visited))
                {
                    occupants[k] = item;
                    return true;
                }
            }
        }

        return false;
    }
}
