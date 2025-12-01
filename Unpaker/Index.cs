namespace Unpaker;

/// <summary>
/// Index containing all entries in a pak file
/// </summary>
internal class Index
{
    public ulong? PathHashSeed { get; set; }
    public SortedDictionary<string, Entry> Entries { get; } = new();

    public Index(ulong? pathHashSeed = null)
    {
        PathHashSeed = pathHashSeed;
    }

    public void AddEntry(string path, Entry entry)
    {
        Entries[path] = entry;
    }
}

