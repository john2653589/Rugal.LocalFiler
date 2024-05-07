using Microsoft.AspNetCore.Http;
using Rugal.LocalFiler.Model;
using System.Text.RegularExpressions;

namespace Rugal.LocalFiler.Service
{
    public partial class FilerService
    {
        public readonly FilerSetting Setting;
        public FilerService(FilerSetting _Setting)
        {
            Setting = _Setting;
        }

        #region Public Method

        #region Transfer
        public virtual FilerService TransferSave<TData>(IEnumerable<TData> Datas, Func<TData, byte[]> ExtractBuffer, Func<TData, object> GetFileName, Action<TData, string> SetFileNameFunc)
        {
            foreach (var Item in Datas)
            {
                var GetBuffer = ExtractBuffer.Invoke(Item);
                var FileName = GetFileName.Invoke(Item).ToString();
                var SetFileName = SaveFile<TData>(FileName, GetBuffer);
                SetFileNameFunc.Invoke(Item, SetFileName);
            }
            return this;
        }
        #endregion

        #region File Save
        protected virtual string LocalSave(SaveConfig Config)
        {
            if (!string.IsNullOrWhiteSpace(Setting.SaveFileNameReplace))
            {
                var RegexFileName = Regex.Replace(Config.FileName, Setting.SaveFileNameReplace, "");
                Config.WithFileName(RegexFileName);
            }

            var FullFileName = ProcessFileNameExtension(Config, out var SetFileName);
            if (Config.SaveBy == SaveByType.FormFile)
            {
                using var Ms = new MemoryStream();
                Config.FormFile.CopyTo(Ms);
                Config.Buffer = Ms.ToArray();
            }

            BaseWriteFile(FullFileName, Config.Buffer);
            return SetFileName;
        }
        public virtual string SaveFile(SaveConfig Config, Action<SaveConfig> ConfigFunc = null)
        {
            ConfigFunc?.Invoke(Config);
            var Result = LocalSave(Config);
            return Result;
        }
        public virtual string SaveFile(Action<SaveConfig> ConfigFunc)
        {
            var Config = new SaveConfig();
            var Result = SaveFile(Config, ConfigFunc);
            return Result;
        }
        public virtual string SaveFile(object FileName, byte[] Buffer, Action<SaveConfig> ConfigFunc = null)
        {
            var Config = new SaveConfig(FileName, Buffer);
            var Result = SaveFile(Config, ConfigFunc);
            return Result;
        }
        public virtual string SaveFile<TData>(object FileName, byte[] Buffer, Action<SaveConfig> ConfigFunc = null)
        {
            var Config = new SaveConfig(FileName, Buffer);
            ConfigFunc?.Invoke(Config);
            Config.AddPath(typeof(TData).Name);

            var Result = SaveFile(Config);
            return Result;
        }
        public virtual string SaveFile(object FileName, IFormFile File, Action<SaveConfig> ConfigFunc = null)
        {
            var Config = new SaveConfig(FileName, File);
            var Result = SaveFile(Config, ConfigFunc);
            return Result;
        }
        public virtual string SaveFile<TData>(object FileName, IFormFile File, Action<SaveConfig> ConfigFunc = null)
        {
            var Config = new SaveConfig(FileName, File);
            ConfigFunc?.Invoke(Config);
            Config.AddPath(typeof(TData).Name);
            var Result = SaveFile(Config);
            return Result;
        }
        #endregion

        #region File Read
        private byte[] LocalRead(ReadConfig Config)
        {
            if (Config.FileName is null)
                return Array.Empty<byte>();

            var FullFileName = CombineRootFileName(Config.FileName, Config.Paths);
            if (!File.Exists(FullFileName))
                return Array.Empty<byte>();

            var FileBuffer = File.ReadAllBytes(FullFileName);
            return FileBuffer;
        }
        public virtual byte[] ReadFile<TData>(object FileName, Action<ReadConfig> ConfigFunc = null)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            Config.AddPath(typeof(TData).Name);
            var FileBuffer = LocalRead(Config);
            return FileBuffer;
        }
        public virtual byte[] ReadFile(object FileName, Action<ReadConfig> ConfigFunc = null)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            var FileBuffer = LocalRead(Config);
            return FileBuffer;
        }

        private Task<byte[]> LocalReadAsync(ReadConfig Config)
        {
            if (Config.FileName is null)
                return Task.FromResult(Array.Empty<byte>());

            var FullFileName = CombineRootFileName(Config.FileName, Config.Paths);
            if (!File.Exists(FullFileName))
                return Task.FromResult(Array.Empty<byte>());

            var FileBuffer = File.ReadAllBytesAsync(FullFileName);
            return FileBuffer;
        }
        public virtual Task<byte[]> ReadFileAsync<TData>(object FileName, Action<ReadConfig> ConfigFunc = null)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            Config.AddPath(typeof(TData).Name);

            var FileBuffer = LocalReadAsync(Config);
            return FileBuffer;
        }
        public virtual Task<byte[]> ReadFileAsync(object FileName, Action<ReadConfig> ConfigFunc = null)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            var FileBuffer = LocalReadAsync(Config);
            return FileBuffer;
        }
        #endregion

        #region File Delete
        public virtual bool DeleteFile(IEnumerable<string> FileNames, Action<ReadConfig> ConfigFunc = null)
        {
            var IsDelete = true;
            foreach (var Item in FileNames)
            {
                var Config = new ReadConfig(Item);
                ConfigFunc?.Invoke(Config);
                IsDelete = IsDelete && DeleteFile(Config);
            }
            return IsDelete;
        }
        public virtual bool DeleteFile<TData>(IEnumerable<string> FileNames, Action<ReadConfig> ConfigFunc = null)
        {
            var IsDelete = true;
            foreach (var Item in FileNames)
            {
                var Config = new ReadConfig(Item);
                ConfigFunc?.Invoke(Config);
                Config.AddPath(typeof(TData).Name);

                IsDelete = IsDelete && DeleteFile(Config);
            }
            return IsDelete;
        }
        public virtual bool DeleteFile(object FileName, Action<ReadConfig> ConfigFunc = null)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            var IsDelete = DeleteFile(Config);
            return IsDelete;
        }
        public virtual bool DeleteFile<TData>(object FileName, Action<ReadConfig> ConfigFunc = null)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            Config.AddPath(typeof(TData).Name);

            var IsDelete = DeleteFile(Config);
            return IsDelete;
        }
        public virtual bool DeleteFile<TData, TColumn>(IEnumerable<TData> FileDatas, Func<TData, TColumn> GetColumnFunc, Action<ReadConfig> ConfigFunc = null)
        {
            var IsDelete = true;
            foreach (var Item in FileDatas)
            {
                var GetFileName = GetColumnFunc(Item);
                var Config = new ReadConfig(GetFileName);
                ConfigFunc?.Invoke(Config);
                Config.AddPath(typeof(TData).Name);
                IsDelete = IsDelete && DeleteFile(Config);
            }
            return IsDelete;
        }
        public bool DeleteFile(ReadConfig Config)
        {
            if (!IsFileExist(Config, out var FullFileName))
                return false;

            File.Delete(FullFileName);
            var IsDelete = !File.Exists(FullFileName);
            return IsDelete;
        }
        #endregion

        #region File Info
        public virtual FilerInfo InfoFile(ReadConfig Config)
        {
            if (Config.FileName is null)
                throw new Exception("file name can not be null");

            var Result = new FilerInfo(this, Config);
            return Result;
        }
        public virtual FilerInfo InfoFile(Action<ReadConfig> ConfigFunc)
        {
            var Config = new ReadConfig();
            ConfigFunc.Invoke(Config);
            var Result = InfoFile(Config);
            return Result;
        }
        public virtual FolderInfo InfoFolder()
        {
            var Config = new PathConfig();
            var Result = new FolderInfo(this, Config);
            return Result;
        }
        public virtual FolderInfo InfoFolder(PathConfig Config)
        {
            var Result = new FolderInfo(this, Config);
            return Result;
        }
        public virtual FolderInfo InfoFolder(Action<PathConfig> ConfigFunc)
        {
            var Config = new PathConfig();
            ConfigFunc.Invoke(Config);
            var Result = InfoFolder(Config);
            return Result;
        }
        #endregion

        #region File Control
        public virtual FilerInfo ReNameInfo(FilerInfo File, string NewFileName)
        {
            var NewConfig = File.Config
                .Clone()
                .WithFileName(NewFileName);

            var NewInfo = new FilerInfo(this, NewConfig)
                .WithSort(File.SortBy)
                .WithFolder(File.Folder);
            return NewInfo;
        }
        public virtual FolderInfo ReNameFolderInfo(FolderInfo Folder, string NewFolderName)
        {
            if (Folder.IsRoot)
                throw new Exception("Cant ReName root folder");

            var NewConfig = Folder.Config
                .Clone()
                .SkipLast(1)
                .AddPath(NewFolderName);

            var NewFolder = new FolderInfo(this, NewConfig)
                .WithSet(Folder)
                .WithParentFolder(Folder.ParentFolder);

            return NewFolder;
        }
        public virtual FilerInfo WithTempInfo(FilerInfo File, string TempExtension = null)
        {
            TempExtension = ConvertExtension(TempExtension);
            var NewFileName = $"{File.FileName}{TempExtension}";
            var TempInfo = ReNameInfo(File, NewFileName);
            return TempInfo;
        }
        public virtual FilerInfo RemoveTempInfo(FilerInfo TempFile, string TempExtension = null)
        {
            TempExtension = ConvertExtension(TempExtension);
            var NewFileName = Regex.Replace(TempFile.FileName, $"{TempExtension}$", "");
            var NewInfo = ReNameInfo(TempFile, NewFileName);
            return NewInfo;
        }
        public virtual FilerInfo RemoveTempFile(FilerInfo TempFile, string TempExtension = null)
        {
            TempExtension = ConvertExtension(TempExtension);
            var NewFileName = Regex.Replace(TempFile.FileName, $"{TempExtension}$", "");
            var NewInfo = ReNameFile(TempFile, NewFileName);
            return NewInfo;
        }
        public virtual FilerInfo ReNameFile(FilerInfo File, string NewFileName)
        {
            var NewInfo = ReNameInfo(File, NewFileName);
            var NewFullFileName = CombineRootFileName(NewFileName, File.Config.Paths);
            File.BaseInfo.MoveTo(NewFullFileName, true);
            File.Folder.ReQueryFile();
            return NewInfo;
        }
        public virtual FolderInfo ReNameFolder(FolderInfo Folder, string NewFolderName)
        {
            var NewFolder = ReNameFolderInfo(Folder, NewFolderName);
            var NewFullPath = CombineRootPaths(NewFolder.Config);
            if (Directory.Exists(NewFullPath))
            {
                Console.WriteLine($"Folder {NewFullPath} is exist");
                return NewFolder;
            }

            Folder.Info.MoveTo(NewFullPath);
            Folder.ParentFolder.ReQueryFolder();
            return NewFolder;
        }
        #endregion

        #region File Exist
        public virtual bool IsFileExist(ReadConfig Config, out string FullFileName)
        {
            FullFileName = null;
            if (Config.FileName is null)
                return false;

            FullFileName = CombineRootFileName(Config.FileName, Config.Paths);
            if (!File.Exists(FullFileName))
                return false;

            return true;
        }
        public virtual bool IsFileExist(ReadConfig Config) => IsFileExist(Config, out _);
        public virtual bool IsFileExist(ReadConfig Config, Action<ReadConfig> ConfigFunc, out string FullFileName)
        {
            ConfigFunc?.Invoke(Config);
            return IsFileExist(Config, out FullFileName);
        }
        public virtual bool IsFileExist(ReadConfig Config, Action<ReadConfig> ConfigFunc) => IsFileExist(Config, ConfigFunc, out _);
        public virtual bool IsFileExist(object FileName, Action<ReadConfig> ConfigFunc, out string FullFileName)
        {
            var Config = new ReadConfig(FileName);
            ConfigFunc?.Invoke(Config);
            return IsFileExist(Config, out FullFileName);
        }
        public virtual bool IsFileExist(object FileName, Action<ReadConfig> ConfigFunc) => IsFileExist(FileName, ConfigFunc, out _);
        public virtual bool IsFileExist(object FileName, out string FullFileName)
        {
            var Config = new ReadConfig(FileName);
            return IsFileExist(Config, out FullFileName);
        }
        public virtual bool IsFileExist(object FileName) => IsFileExist(FileName, out _);
        #endregion

        #region Temp File Check
        public virtual bool IsTemp(FilerInfo File, string TempExtension = null)
        {
            var GetExtension = ConvertExtension(TempExtension);
            if (GetExtension == File.Extension)
                return true;
            return false;
        }
        public virtual bool HasTemp(FilerInfo File, string TempExtension = null)
        {
            var TempInfo = WithTempInfo(File, TempExtension);
            return TempInfo.IsExist;
        }
        #endregion

        #region File Or Folder Visit
        public FilerInfo RCS_ToNextFile(FilerInfo File = null, SortByType SortBy = SortByType.Nono)
        {
            var NextFile = File?
                .WithSort(SortBy)?
                .NextFile();

            if (NextFile is not null)
                return NextFile;

            var Folder = File?.Folder;
            Folder ??= InfoFolder().WithSort(SortBy);

            while (NextFile is null)
            {
                NextFile = Folder.Files
                  .FirstOrDefault();

                if (NextFile is not null)
                    return NextFile;

                Folder = RCS_ToNextFolder(Folder, SortBy);
                if (Folder is null)
                    return null;
            }
            return NextFile;
        }
        public FolderInfo RCS_ToNextFolder(FolderInfo Folder, SortByType SortBy = SortByType.Nono, bool IsSearchUnder = true)
        {
            if (IsSearchUnder)
            {
                var UnderFolder = Folder
                    .WithSort(SortBy).Folders
                    .FirstOrDefault();

                if (UnderFolder is not null)
                    return UnderFolder;
            }

            if (Folder.IsRoot)
                return null;

            var NextFolder = Folder
                .WithSort(SortBy)
                .NextFolder();

            if (NextFolder is not null)
                return NextFolder;

            return RCS_ToNextFolder(Folder.ParentFolder, SortBy, false);
        }
        public FolderInfo RCS_FindToFolder(FolderInfo RootFolder, Action<PathConfig> ConfigFunc)
        {
            var TargetConfig = new PathConfig();
            ConfigFunc.Invoke(TargetConfig);
            var TargetPaths = TargetConfig.Paths.ToList();
            var SourcePaths = RootFolder.Config.Paths.ToList();

            if (SourcePaths.Count > TargetPaths.Count)
                return RCS_FindToFolder(RootFolder.ParentFolder, ConfigFunc);

            var IsEqualsCount = TargetPaths.Count == SourcePaths.Count;
            var IsDiff = false;
            for (var i = 0; i < SourcePaths.Count; i++)
            {
                var Target = TargetPaths[i];
                var Source = SourcePaths[i];
                if (Target != Source)
                {
                    IsDiff = true;
                    break;
                }
            }

            if (IsDiff)
                return RCS_FindToFolder(RootFolder.ParentFolder, ConfigFunc);

            if (IsEqualsCount)
                return RootFolder;

            var FindIndex = SourcePaths.Count;
            var FindPath = TargetPaths[FindIndex];

            var FindFolder = RootFolder.Folders
                .FirstOrDefault(Item => Item.Config.Paths.Last() == FindPath);

            if (FindFolder is null)
                return null;

            return RCS_FindToFolder(FindFolder, ConfigFunc);
        }
        public FilerInfo RCS_FindToFile(FolderInfo RootFolder, Action<ReadConfig> ConfigFunc)
        {
            var Config = new ReadConfig();
            ConfigFunc.Invoke(Config);

            var FindFolder = RCS_FindToFolder(RootFolder, Item => Item.AddPath(Config.Paths));
            if (FindFolder is null)
                return null;

            var FindFile = FindFolder.Files
                .FirstOrDefault(Item => Item.FileName == Config.FileName);

            return FindFile;
        }
        #endregion

        #region Folder Access
        public bool IsFolderExist(PathConfig Config)
        {
            var IsExist = InfoFolder(Config).IsExist;
            return IsExist;
        }
        public bool IsFolderAnyFile(PathConfig Config)
        {
            var Info = InfoFolder(Config);
            if (!Info.IsExist)
                return false;

            var IsAnyFile = Info.Files.Any();
            return IsAnyFile;
        }
        #endregion

        #endregion

        #region Convert File Name And Root File Name
        public virtual string CombineRootFileName(string FileName, out string SetFileName, IEnumerable<string> Paths = null, bool IsVerifyFileName = true)
        {
            SetFileName = ConvertFileName(FileName);

            if (IsVerifyFileName)
                VerifyFileName(SetFileName);

            Paths ??= new List<string> { };
            var PathList = Paths.ToList();
            PathList.Add(SetFileName);

            var FullFileName = CombineRootPaths(PathList);
            return FullFileName;
        }
        public virtual string CombineRootFileName(string FileName, IEnumerable<string> Paths = null, bool IsVerifyFileName = false)
        {
            var FullFileName = CombineRootFileName(FileName, out _, Paths, IsVerifyFileName);
            return FullFileName;
        }
        public virtual string CombineRootFileName(ReadConfig Config, bool IsVerifyFileName = false)
        {
            var FullFileName = CombineRootFileName(Config.FileName, Config.Paths, IsVerifyFileName);
            return FullFileName;
        }
        public virtual string CombineExtension(string FileName, string Extension)
        {
            var ClearFileName = FileName.TrimEnd('.');
            if (string.IsNullOrWhiteSpace(Extension))
                return FileName;

            var CombineFileName = $"{ClearFileName}.{Extension.ToLower().TrimStart('.')}";
            return CombineFileName;
        }
        public virtual string ConvertExtension(string Extension)
        {
            Extension ??= Setting.TempExtention;
            Extension = $".{Extension.Replace(".", "")}";
            return Extension;
        }
        #endregion

        #region Root Path Process
        public virtual string CombineRootPaths(IEnumerable<string> Paths)
        {
            var AllPaths = new[]
            {
                Setting.RootPath,
            }.ToList();

            var ConvertPaths = Paths?
                .Select(Item => Item?.ToString().TrimStart('/').TrimEnd('/').Split('/'))
                .Where(Item => Item is not null)
                .SelectMany(Item => Item)
                .ToList();

            if (ConvertPaths is not null)
            {
                foreach (var Item in ConvertPaths)
                    VerifyPath(Item);

                AllPaths.AddRange(ConvertPaths);
            }

            var FullPath = Path.Combine(AllPaths.ToArray()).Replace(@"\", "/");
            return FullPath;
        }
        public virtual string CombineRootPaths(PathConfig Config)
        {
            var Result = CombineRootPaths(Config.Paths);
            return Result;
        }
        #endregion

        #region Private Method
        private string ConvertFileName(string FileName)
        {
            if (FileName is null)
                return null;

            FileName = Setting.FileNameCase switch
            {
                FileNameCaseType.None => FileName,
                FileNameCaseType.Upper => FileName.ToUpper(),
                FileNameCaseType.Lower => FileName.ToLower(),
                _ => FileName,
            };

            return FileName;
        }
        private string ProcessFileNameExtension(SaveConfig Config, out string SetFileName)
        {
            var FileName = Config.FileName;

            if (Setting.DefaultExtensionFromFile && Config.SaveBy == SaveByType.FormFile && !Config.HasExtension)
                Config.UseFileExtension();

            if (Setting.UseExtension && Config.HasExtension)
                FileName = CombineExtension(FileName, Config.Extension);

            var FullFileName = CombineRootFileName(FileName, out SetFileName, Config.Paths);
            return FullFileName;
        }
        private static void BaseWriteFile(string FullFileName, byte[] WriteBuffer)
        {
            var Info = new FileInfo(FullFileName);
            if (!Info.Directory.Exists)
                Info.Directory.Create();

            File.WriteAllBytes(FullFileName, WriteBuffer);
        }
        public static bool IsVerifyFileName(string FileName, out string ErrorMessage)
        {
            ErrorMessage = null;

            var WhiteList = new Regex(@"^[a-zA-Z0-9_.-]+$");
            if (!WhiteList.IsMatch(FileName))
            {
                ErrorMessage = "file name verification failed";
                return false;
            }

            var BlackList = new[] { ".." };
            foreach (var Item in BlackList)
            {
                if (FileName.Contains(Item))
                {
                    ErrorMessage = "file name verification failed";
                    return false;
                }
            }
            return true;
        }
        public static void VerifyFileName(string FileName)
        {
            if (!IsVerifyFileName(FileName, out var ErrorMessage))
                throw new Exception(ErrorMessage);
        }
        public static void VerifyPath(string FilePath)
        {
            if (Path.IsPathRooted(FilePath))
                throw new Exception("not allowed path");
        }
        #endregion
    }
}