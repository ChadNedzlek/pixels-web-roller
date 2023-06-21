using System.Collections.Generic;
using Rolling.Utilities;

namespace Rolling;

public class RollPool
{
    private readonly Dictionary<int, Queue<DieRoll>> _pool = new();

    public RollPool()
    {
    }

    public RollPool(IEnumerable<DieRoll> rolls)
    {
        foreach (var roll in rolls)
        {
            _pool.GetOrAdd(roll.Size).Enqueue(roll);
        }
    }

    public Maybe<DieRoll> Take(int size)
    {
        if (!_pool.TryGetValue(size, out Queue<DieRoll> list))
        {
            return Maybe<DieRoll>.None;
        }

        DieRoll roll = list.Dequeue();
        if (list.Count == 0)
            _pool.Remove(size);
        return roll;
    }
}