namespace Coremats;

public abstract class FileFormat
{
    protected static bool IsFile(string path, Func<BexReader, bool> is_)
    {
        using var fs = File.OpenRead(path);
        var br = new BexReader(fs, false);
        return is_(br);
    }

    protected static bool IsBytes(byte[] bytes, Func<BexReader, bool> is_)
    {
        var br = new BexReader(bytes, false);
        return is_(br);
    }

    protected static T ReadFile<T>(string path, Func<BexReader, T> read)
    {
        using var fs = File.OpenRead(path);
        var br = new BexReader(fs, false);
        return read(br);
    }

    protected static T ReadBytes<T>(byte[] bytes, Func<BexReader, T> read)
    {
        var br = new BexReader(bytes, false);
        return read(br);
    }

    protected static void WriteFile(string path, Action<BexWriter> write)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var fs = File.Create(path);
        var bw = new BexWriter(false, fs);
        write(bw);
        bw.Finish();
    }

    protected static byte[] WriteBytes(Action<BexWriter> write)
    {
        var bw = new BexWriter(false);
        write(bw);
        return bw.FinishBytes();
    }
}
