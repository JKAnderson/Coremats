namespace Coremats.PSP;

public class FAT : FileFormat
{
    public Directory RootDirectory { get; set; }

    public static FAT Read(string path) => ReadFile(path, br => new FAT(br));
    public static FAT Read(byte[] bytes) => ReadBytes(bytes, br => new FAT(br));

    private FAT(BexReader br)
    {
        br.AssertInt32(0x210);
        br.ReadInt32();
        br.AssertInt32(0);
        br.AssertInt32(0);
        RootDirectory = new(br) { Name = null };
    }

    public class Directory
    {
        public string Name { get; set; }
        public List<Directory> Directories { get; set; }
        public List<File> Files { get; set; }

        internal Directory(BexReader br)
        {
            Name = br.PeekShiftJis(br.ReadInt32());
            int directoriesOffset = br.ReadInt32();
            int filesOffset = br.ReadInt32();
            int directoryCount = br.ReadInt32();
            int fileCount = br.ReadInt32();

            br.JumpIn(directoriesOffset);
            {
                Directories = new(directoryCount);
                for (int i = 0; i < directoryCount; i++)
                    Directories.Add(new(br));
            }
            br.JumpOut();

            br.JumpIn(filesOffset);
            {
                Files = new(fileCount);
                for (int i = 0; i < fileCount; i++)
                    Files.Add(new(br));
            }
            br.JumpOut();
        }
    }

    public class File
    {
        public string Name { get; set; }
        public int Index { get; set; }

        internal File(BexReader br)
        {
            Name = br.PeekShiftJis(br.ReadInt32());
            Index = br.ReadInt32();
        }
    }
}
