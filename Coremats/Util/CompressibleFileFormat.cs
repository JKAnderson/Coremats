namespace Coremats;

public abstract class CompressibleFileFormat
{
    public DCX.Type Compression { get; set; } = DCX.Type.None;

    protected static T ReadFile<T>(string path, Func<BexReader, T> read) where T : CompressibleFileFormat
    {
        using var fs = File.OpenRead(path);
        var br = new BexReader(fs, false);
        DCX.Type compression = DCX.Type.None;
        if (DCX.Is(br))
        {
            byte[] decompressed = DCX.Decompress(br, out compression);
            br = new BexReader(decompressed, false);
        }

        br.Position = 0;
        br.BigEndian = false;
        var file = read(br);
        file.Compression = compression;
        return file;
    }

    protected static T ReadBytes<T>(byte[] bytes, Func<BexReader, T> read) where T : CompressibleFileFormat
    {
        var br = new BexReader(bytes, false);
        DCX.Type compression = DCX.Type.None;
        if (DCX.Is(br))
        {
            byte[] decompressed = DCX.Decompress(br, out compression);
            br = new BexReader(decompressed, false);
        }

        br.Position = 0;
        br.BigEndian = false;
        var file = read(br);
        file.Compression = compression;
        return file;
    }

    protected void WriteFile(string path, Action<BexWriter> write) => WriteFile(path, write, Compression);

    protected static void WriteFile(string path, Action<BexWriter> write, DCX.Type compression)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        if (compression == DCX.Type.None)
        {
            using var fs = File.Create(path);
            var bw = new BexWriter(fs, false);
            write(bw);
            bw.Finish();
        }
        else
        {
            var bw = new BexWriter(false);
            write(bw);
            byte[] bytes = bw.FinishBytes();
            DCX.Compress(bytes, compression, path);
        }
    }

    protected byte[] WriteBytes(Action<BexWriter> write) => WriteBytes(write, Compression);

    protected static byte[] WriteBytes(Action<BexWriter> write, DCX.Type compression)
    {
        var bw = new BexWriter(false);
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
