namespace IntegrationTests.Infrastructure.LeadFlow;

public sealed class InMemoryMessageQueue<T>
{
    private readonly Queue<T> _items = new();

    public int Count
    {
        get
        {
            lock (_items)
            {
                return _items.Count;
            }
        }
    }

    public void Enqueue(T item)
    {
        lock (_items)
        {
            _items.Enqueue(item);
        }
    }

    public bool TryDequeue(out T item)
    {
        lock (_items)
        {
            if (_items.Count == 0)
            {
                item = default!;
                return false;
            }

            item = _items.Dequeue();
            return true;
        }
    }

    public IReadOnlyList<T> Snapshot()
    {
        lock (_items)
        {
            return _items.ToList();
        }
    }
}
