namespace Coremats;
public abstract class CompressibleFileFormat
{
    public DCX.Type Compression { get; set; } = DCX.Type.None;

    protected static T ReadFile<T>(string path, Func<BinaryReaderEx, T> read) where T : CompressibleFileFormat
    {
        using var fs = File.OpenRead(path);
        var br = new BinaryReaderEx(false, fs);
        DCX.Type compression = DCX.Type.None;
        if (DCX.Is(br))
        {
            byte[] decompressed = DCX.Decompress(br, out compression);
            br = new BinaryReaderEx(false, decompressed);
        }

        br.Position = 0;
        br.BigEndian = false;
        var file = read(br);
        file.Compression = compression;
        return file;
    }

    protected static T ReadBytes<T>(byte[] bytes, Func<BinaryReaderEx, T> read) where T : CompressibleFileFormat
    {
        var br = new BinaryReaderEx(false, bytes);
        DCX.Type compression = DCX.Type.None;
        if (DCX.Is(br))
        {
            byte[] decompressed = DCX.Decompress(br, out compression);
            br = new BinaryReaderEx(false, decompressed);
        }

        br.Position = 0;
        br.BigEndian = false;
        var file = read(br);
        file.Compression = compression;
        return file;
    }

    protected void WriteFile(string path, Action<BinaryWriterEx> write) => WriteFile(path, write, Compression);

    protected static void WriteFile(string path, Action<BinaryWriterEx> write, DCX.Type compression)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (compression == DCX.Type.None)
        {
            using var fs = File.Create(path);
            var bw = new BinaryWriterEx(false, fs);
            write(bw);
            bw.Finish();
        }
        else
        {
            var bw = new BinaryWriterEx(false);
            write(bw);
            byte[] bytes = bw.FinishBytes();
            DCX.Compress(bytes, compression, path);
        }
    }

    protected byte[] WriteBytes(Action<BinaryWriterEx> write) => WriteBytes(write, Compression);

    protected static byte[] WriteBytes(Action<BinaryWriterEx> write, DCX.Type compression)
    {
        var bw = new BinaryWriterEx(false);
        write(bw);
        byte[] bytes = bw.FinishBytes();

        if (compression == DCX.Type.None)
        {
            return bytes;
        }
        else
        {
            return DCX.Compress(bytes, compression);
        }
    }
}
