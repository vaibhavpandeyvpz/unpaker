namespace Unpaker;

/// <summary>
/// Compression block with start/end offsets
/// </summary>
public class Block
{
    public ulong Start { get; set; }
    public ulong End { get; set; }

    public Block() { }

    public Block(ulong start, ulong end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Read block from binary reader
    /// </summary>
    public static Block Read(BinaryReader reader)
    {
        return new Block
        {
            Start = reader.ReadUInt64(),
            End = reader.ReadUInt64(),
        };
    }

    /// <summary>
    /// Write block to binary writer
    /// </summary>
    public void Write(BinaryWriter writer)
    {
        writer.Write(Start);
        writer.Write(End);
    }

    public Block Clone() => new Block(Start, End);
}

