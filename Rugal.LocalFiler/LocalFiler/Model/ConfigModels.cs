using Microsoft.AspNetCore.Http;

namespace Rugal.LocalFiler.Model
{
    public class PathConfig
    {
        public IEnumerable<string> Paths { get; set; }
        public string FullPaths => GetFullPath();
        public PathConfig()
        {
            Paths = new List<string>();
        }
        public virtual PathConfig AddPath(string Path)
        {
            if (Path is null)
                return this;

            Path = ClearPath(Path);
            Paths ??= new List<string>();
            var PathList = Paths is IList<string> IListPaths ? IListPaths : Paths.ToList();
            PathList.Add(Path);
            Paths = PathList;
            return this;
        }
        public virtual PathConfig AddPath(IEnumerable<string> Paths)
        {
            if (Paths is null)
                return this;

            foreach (var Item in Paths)
                AddPath(Item);
            return this;
        }
        public virtual PathConfig WithConfig(PathConfig Config)
        {
            AddPath(Config.Paths);
            return this;
        }
        public virtual PathConfig SkipLast(int SkipCount = 1)
        {
            if (Paths is null)
                return this;

            Paths = Paths.SkipLast(SkipCount)
                .ToList();

            return this;
        }
        public virtual PathConfig Clone()
        {
            var NewConfig = new PathConfig()
                .AddPath(Paths);
            return NewConfig;
        }
        public virtual string GetFullPath()
        {
            var Result = string.Join("/", Paths);
            return Result;
        }
        private static string ClearPath(string Path)
        {
            while (Path.Contains(@"//"))
                Path = Path.Replace(@"//", @"/");

            while (Path.Contains(@"\\"))
                Path = Path.Replace(@"\\", @"\");

            return Path;
        }
    }
    public class ReadConfig : PathConfig
    {
        public string FileName { get; set; }
        public string FullFileName => GetFullFileName();
        public ReadConfig() { }
        public ReadConfig(object _FileName)
        {
            FileName = _FileName?.ToString();
        }
        public ReadConfig(object _FileName, IEnumerable<string> _Paths = null) : this(_FileName)
        {
            AddPath(_Paths);
        }
        public ReadConfig WithFileName(string _FileName)
        {
            FileName = _FileName;
            return this;
        }
        public override ReadConfig WithConfig(PathConfig Config)
        {
            base.WithConfig(Config);
            return this;
        }
        public ReadConfig WithConfig(ReadConfig Config)
        {
            base.WithConfig(Config);
            FileName = Config.FileName;
            return this;
        }
        public override ReadConfig Clone()
        {
            var NewConfig = new ReadConfig()
                .AddPath(Paths)
                .WithFileName(FileName);

            return NewConfig;
        }
        public override ReadConfig AddPath(string Path)
        {
            base.AddPath(Path);
            return this;
        }
        public override ReadConfig AddPath(IEnumerable<string> Paths)
        {
            base.AddPath(Paths);
            return this;
        }
        public virtual string GetFullFileName()
        {
            var JoinPaths = Paths.ToList();
            JoinPaths.Add(FileName);
            var Result = string.Join("/", JoinPaths);
            return Result;
        }
    }
    public class SaveConfig : ReadConfig
    {
        public byte[] Buffer { get; set; }
        public IFormFile FormFile { get; set; }
        public string Extension { get; set; }
        public bool HasExtension => !string.IsNullOrWhiteSpace(Extension);
        public SaveByType SaveBy => GetSaveBy();
        public SaveConfig() { }
        public SaveConfig(object _FileName, byte[] _Buffer) : base(_FileName)
        {
            Buffer = _Buffer;
        }
        public SaveConfig(object _FileName, IFormFile _FormFile) : base(_FileName)
        {
            FormFile = _FormFile;
        }
        public SaveConfig UseFileExtension()
        {
            if (FormFile is null)
                return this;

            Extension = Path.GetExtension(FormFile.FileName);
            return this;
        }
        public SaveConfig WithFile(IFormFile File, bool UseExtension = true)
        {
            FormFile = File;
            if (UseExtension)
                UseFileExtension();
            return this;
        }
        public SaveConfig WithFile(IFormFile File)
        {
            FormFile = File;
            return this;
        }
        private SaveByType GetSaveBy()
        {
            if (Buffer is not null)
                return SaveByType.Buffer;

            if (FormFile is not null)
                return SaveByType.FormFile;

            return SaveByType.None;
        }
    }
    public enum SaveByType
    {
        None,
        Buffer,
        FormFile,
    }
}
