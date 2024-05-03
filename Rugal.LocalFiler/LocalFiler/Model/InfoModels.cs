using Rugal.LocalFiler.Service;

namespace Rugal.LocalFiler.Model
{
    public class BaseInfo
    {
        public readonly FilerService Filer;
        public SortByType SortBy { get; set; } = SortByType.Length;
        public static IEnumerable<FolderInfo> SortFolders(IEnumerable<FolderInfo> Folders, SortByType SortBy)
        {
            Folders = SortBy switch
            {
                SortByType.Length => Folders
                    .OrderBy(Item => Item.TotalLength)
                    .ThenBy(Item => Item.FolderName),
                SortByType.Name => Folders
                    .OrderBy(Item => Item.FolderName),
                _ => Folders,
            };

            return Folders;
        }
        public static IEnumerable<FilerInfo> SortFiles(IEnumerable<FilerInfo> Files, SortByType SortBy)
        {
            Files = SortBy switch
            {
                SortByType.Length => Files
                    .OrderBy(Item => Item.Length)
                    .ThenBy(Item => Item.FileName),
                SortByType.Name => Files.OrderBy(Item => Item.FileName),
                _ => Files,
            };

            return Files;
        }
        public BaseInfo(FilerService _Filer)
        {
            Filer = _Filer;
        }
    }
    public class FilerInfo : BaseInfo
    {
        #region Lazy Property
        private readonly Lazy<FolderInfo> _Folder;
        #endregion

        #region Private Property
        private FolderInfo UpperSetFolder;
        #endregion

        #region Public Property
        public ReadConfig Config { get; set; }
        public FileInfo BaseInfo => GetBaseInfo();
        public FolderInfo Folder => _Folder.Value;
        public string FileName => BaseInfo.Name;
        public string Extension => BaseInfo.Extension;
        public bool IsExist => BaseInfo.Exists;
        public long Length => IsExist ? BaseInfo.Length : -1;
        public bool IsVerifyFileName { get; set; }
        #endregion

        #region Constructor
        public FilerInfo(FilerService _Filer, ReadConfig _Config, bool _IsVerifyFileName) : base(_Filer)
        {
            Config = _Config;
            IsVerifyFileName = _IsVerifyFileName;
            _Folder = new Lazy<FolderInfo>(() => GetFolder());
        }
        public FilerInfo(FilerService _Filer, ReadConfig _Config) : this(_Filer, _Config, false) { }
        #endregion

        #region With Method
        public FilerInfo WithSort(SortByType _SortBy)
        {
            if (_SortBy == SortByType.Nono)
                return this;

            SortBy = _SortBy;
            return this;
        }
        public FilerInfo WithFolder(FolderInfo _UpperSetFolder)
        {
            UpperSetFolder = _UpperSetFolder;
            return this;
        }
        #endregion

        #region Public Method
        public FilerInfo NextFile(SortByType NextBy = SortByType.Nono)
        {
            if (NextBy == SortByType.Nono)
                NextBy = SortBy;

            var Result = NextFileBy(NextBy);
            return Result;
        }
        public FilerInfo PreviousFile(SortByType PreviousBy)
        {
            if (PreviousBy == SortByType.Nono)
                PreviousBy = SortBy;

            var Result = PreviousFileBy(PreviousBy);
            return Result;
        }
        public FilerInfo Clone()
        {
            var NewInfo = new FilerInfo(Filer, Config.Clone())
                .WithSort(SortBy)
                .WithFolder(Folder);
            return NewInfo;
        }
        public FilerWriter ToWriter()
        {
            var Writer = new FilerWriter(this);
            return Writer;
        }
        public FilerInfo ToTemp(string TempExtension = null)
        {
            var TempInfo = Filer.WithTempInfo(this, TempExtension);
            return TempInfo;
        }
        public FilerInfo RemoveTempInfo(string TepmExtension = null)
        {
            var Result = Filer.RemoveTempInfo(this, TepmExtension);
            return Result;
        }
        public FilerInfo RemoveTempFile(string TepmExtension = null)
        {
            var Reuslt = Filer.RemoveTempFile(this, TepmExtension);
            return Reuslt;
        }
        public bool HasTemp(string TempExtension = null)
        {
            var Result = Filer.HasTemp(this, TempExtension);
            return Result;
        }
        public bool IsTemp(string TempExtension = null)
        {
            var Result = Filer.IsTemp(this, TempExtension);
            return Result;
        }
        #endregion

        #region Public Process
        public int IndexOfBy(IEnumerable<FilerInfo> Files, SortByType IndexSortBy)
        {
            if (IndexSortBy == SortByType.Nono)
                IndexSortBy = SortBy;

            var FindFile = Files
                .Select((Item, Index) => new
                {
                    Info = Item,
                    Index
                })
                .FirstOrDefault(Item => Item.Info.FileName == FileName);

            if (FindFile is null)
                return -1;

            return FindFile.Index;
        }
        #endregion

        #region Private Process
        private FolderInfo GetFolder()
        {
            if (UpperSetFolder is not null)
                return UpperSetFolder;

            var Result = new FolderInfo(Filer, Config)
                .WithSort(SortBy);
            return Result;
        }
        private FilerInfo NextFileBy(SortByType SortBy)
        {
            var Folders = SortFiles(Folder.Files, SortBy);
            var Index = IndexOfBy(Folders, SortBy);
            Index++;

            if (Index >= Folders.Count())
                return null;

            var Result = Folders
                .Skip(Index)
                .First();

            return Result;
        }
        private FilerInfo PreviousFileBy(SortByType SortBy)
        {
            var Files = Folder.Files
               .OrderBy(Item => Item.FileName)
               .ToArray();

            var Index = IndexOfBy(Files, SortBy);
            Index--;

            if (Index < 0)
                return null;

            var Result = Files[Index];
            return Result;
        }
        private FileInfo GetBaseInfo()
        {
            var FullFileName = Filer.CombineRootFileName(Config, IsVerifyFileName);
            var InfoResult = new FileInfo(FullFileName);
            return InfoResult;
        }
        #endregion
    }
    public class FolderInfo : BaseInfo
    {
        #region Lazy Property
        protected Lazy<IEnumerable<FilerInfo>> _Files { get; private set; }
        protected Lazy<long> _TotalLength { get; private set; }
        protected Lazy<IEnumerable<FolderInfo>> _Folders { get; private set; }
        protected Lazy<FolderInfo> _ParentFolder { get; private set; }
        #endregion

        #region Private Property
        private FolderInfo UpperSetParentFolder;
        #endregion

        #region Public ReadOnly Property
        public readonly DirectoryInfo Info;
        public readonly PathConfig Config;
        #endregion

        #region Public Property
        public bool IsRoot => !Config.Paths.Any();
        public string FolderName => Info.Name;
        public bool IsExist => Info.Exists;
        public FolderInfo ParentFolder => _ParentFolder.Value;
        public IEnumerable<FilerInfo> Files => _Files.Value;
        public IEnumerable<FolderInfo> Folders => _Folders.Value;
        public long TotalLength => _TotalLength.Value;
        public FolderModeType FolderMode { get; set; } = FolderModeType.Static;
        public string IgnoreTempExtension { get; set; }
        #endregion

        #region Constructor
        public FolderInfo(FilerService _Filer, PathConfig _Config) : base(_Filer)
        {
            Config = _Config;

            var FullPath = Filer.CombineRootPaths(Config);
            Info = new DirectoryInfo(FullPath);

            _Files = new Lazy<IEnumerable<FilerInfo>>(() => GetFiles());
            _Folders = new Lazy<IEnumerable<FolderInfo>>(() => GetFolders());
            _ParentFolder = new Lazy<FolderInfo>(() => GetParentFolder());
            _TotalLength = new Lazy<long>(() => GetTotalLength());
        }
        #endregion

        #region Public Method
        public FolderInfo NextFolder(SortByType NextBy = SortByType.Nono)
        {
            if (NextBy == SortByType.Nono)
                NextBy = SortBy;

            var Result = NextFolderBy(NextBy);
            return Result;
        }
        public FolderInfo PreviousFolder(SortByType PreviousBy = SortByType.Nono)
        {
            if (PreviousBy == SortByType.Nono)
                PreviousBy = SortBy;

            var Result = PreviousFolderBy(PreviousBy);
            return Result;
        }
        public FilerInfo InfoFile(string FileName)
        {
            var FileConfig = new ReadConfig()
                .WithConfig(Config)
                .WithFileName(FileName);

            var NewInfo = new FilerInfo(Filer, FileConfig)
                .WithSort(SortBy)
                .WithFolder(this);
            return NewInfo;
        }
        public FolderInfo ReQuery()
        {
            ReQueryFile(false);
            ReQueryFolder(false);
            ReQueryLength();
            return this;
        }
        public FolderInfo ReQueryFile(bool IsRequeryLength = true)
        {
            _Files = new Lazy<IEnumerable<FilerInfo>>(GetFiles());
            _ = _Files.Value;

            if (IsRequeryLength)
                ReQueryLength();
            return this;
        }
        public FolderInfo ReQueryFolder(bool IsRequeryLength = true)
        {
            _Folders = new Lazy<IEnumerable<FolderInfo>>(GetFolders());
            _ = _Folders.Value;

            if (IsRequeryLength)
                ReQueryLength();
            return this;
        }
        public FolderInfo ReQueryLength()
        {
            var GetFolder = this;
            while (GetFolder is not null)
            {
                GetFolder._TotalLength = new Lazy<long>(GetFolder.GetTotalLength());
                _ = GetFolder._TotalLength.Value;
                GetFolder = GetFolder.ParentFolder;
            }

            return this;
        }
        #endregion

        #region With Method
        public FolderInfo WithSet(FolderInfo Source)
        {
            WithMode(Source.FolderMode);
            WithSort(Source.SortBy);
            WithIgnoreTemp(Source.IgnoreTempExtension);
            return this;
        }
        public FolderInfo WithMode(FolderModeType _FolderMode)
        {
            FolderMode = _FolderMode;
            return this;
        }
        public FolderInfo WithSort(SortByType _SortBy)
        {
            if (_SortBy == SortByType.Nono)
                return this;

            SortBy = _SortBy;
            return this;
        }
        public FolderInfo WithParentFolder(FolderInfo _UpperSetParentFolder)
        {
            UpperSetParentFolder = _UpperSetParentFolder;
            return this;
        }
        public FolderInfo WithIgnoreTemp(string TempExtension = null)
        {
            TempExtension = Filer.ConvertExtension(TempExtension);
            IgnoreTempExtension = TempExtension;
            return this;
        }
        #endregion

        #region Public Process
        public int IndexOfBy(IEnumerable<FolderInfo> Folders, SortByType IndexSortBy = SortByType.Nono)
        {
            if (IndexSortBy == SortByType.Nono)
                IndexSortBy = SortBy;

            var FindFolder = Folders
                .Select((Item, Index) => new
                {
                    Info = Item,
                    Index
                })
                .FirstOrDefault(Item => Item.Info.FolderName == FolderName);

            if (FindFolder is null)
                return -1;

            return FindFolder.Index;
        }
        #endregion

        #region Private Process
        private IEnumerable<FilerInfo> GetFiles()
        {
            try
            {
                if (!Info.Exists)
                    return Array.Empty<FilerInfo>();

                var Files = Info.GetFiles()
                    .Select(FileInfo =>
                    {
                        var FileConfig = new ReadConfig()
                            .WithConfig(Config)
                            .WithFileName(FileInfo.Name);

                        var GetInfo = new FilerInfo(Filer, FileConfig)
                            .WithSort(SortBy)
                            .WithFolder(this);
                        return GetInfo;
                    });

                if (!string.IsNullOrWhiteSpace(IgnoreTempExtension))
                    Files = Files.Where(Item => !Item.IsTemp(IgnoreTempExtension));

                Files = SortFiles(Files, SortBy);
                if (FolderMode == FolderModeType.Static)
                {
                    Files = Files.ToArray();
                    return Files;
                }

                return Files;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"{Info.FullName} access not allowed");
                return Array.Empty<FilerInfo>();
            }
        }
        private IEnumerable<FolderInfo> GetFolders()
        {
            try
            {
                if (!Info.Exists)
                    return Array.Empty<FolderInfo>();

                var Folders = Info
                    .EnumerateDirectories()
                    .Select(FolderInfo =>
                    {
                        var FolderConfig = new PathConfig()
                            .AddPath(Config.Paths)
                            .AddPath(FolderInfo.Name);

                        var GetInfo = new FolderInfo(Filer, FolderConfig)
                            .WithSet(this)
                            .WithParentFolder(this);
                        return GetInfo;
                    });

                Folders = SortFolders(Folders, SortBy);
                if (FolderMode == FolderModeType.Static)
                {
                    Folders = Folders.ToArray();
                    return Folders;
                }

                return Folders;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"{Info.FullName} access not allowed");
                return Array.Empty<FolderInfo>();
            }
        }
        private long GetTotalLength()
        {
            var FilesSum = Files.Sum(Item => Item.Length);
            var FoldersSum = Folders.Sum(Item => Item.TotalLength);
            var Result = FilesSum + FoldersSum;
            return Result;
        }
        private FolderInfo GetParentFolder()
        {
            if (IsRoot)
                return null;

            if (UpperSetParentFolder is not null)
                return UpperSetParentFolder;

            var FolderConfig = new PathConfig()
                .AddPath(Config.Paths)
                .SkipLast();

            var Result = new FolderInfo(Filer, FolderConfig)
                .WithSort(SortBy);
            return Result;
        }
        private FolderInfo NextFolderBy(SortByType SortBy)
        {
            if (ParentFolder is null)
                return null;

            var Folders = SortFolders(ParentFolder.Folders, SortBy);
            var Index = IndexOfBy(Folders, SortBy);
            Index++;

            if (Index >= Folders.Count())
                return null;

            var Result = Folders
                .Skip(Index)
                .First();

            return Result;
        }
        private FolderInfo PreviousFolderBy(SortByType SortBy)
        {
            if (ParentFolder is null)
                return null;

            var Folders = SortFolders(ParentFolder.Folders, SortBy);
            var Index = IndexOfBy(Folders, SortBy);
            Index--;

            if (Index < 0)
                return null;

            var Result = Folders
                .Skip(Index)
                .First();
            return Result;
        }
        #endregion
    }
    public enum SortByType
    {
        Nono,
        Name,
        Length,
    }
    public enum FolderModeType
    {
        Static,
        Dynamic,
    }
}