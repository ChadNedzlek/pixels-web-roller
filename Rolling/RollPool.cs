using System.Collections.Generic;
using Rolling.Utilities;

namespace Rolling;

public class RollPool
{
    private readonly Dictionary<int, Queue<RawRoll>> _pool = new();

    public RollPool()
    {
    }

    public RollPool(IEnumerable<RawRoll> rolls)
    {
        foreach (var roll in rolls)
        {
            _pool.GetOrAdd(roll.Size).Enqueue(roll);
        }
    }

    public Maybe<RawRoll> Take(int size)
    {
        if (!_pool.TryGetValue(size, out Queue<RawRoll> list))
        {
            return Maybe<RawRoll>.None;
        }

        RawRoll roll = list.Dequeue();
        if (list.Count == 0)
            _pool.Remove(size);
        return roll;
    }
}