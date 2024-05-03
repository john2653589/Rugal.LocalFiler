namespace Rugal.LocalFiler.Model
{
    public class FilerSetting
    {
        public bool DefaultExtensionFromFile { get; set; }
        public bool UseExtension { get; set; }
        public string TempExtention { get; set; }
        public string SaveFileNameReplace { get; set; }
        public int ReadPerKb { get; set; }
        public FileNameCaseType FileNameCase { get; set; } = FileNameCaseType.None;
        public string FormatRootPath { get; private set; }
        public string RootPath
        {
            get => GetRootPath();
            set => FormatRootPath = value;
        }
        public Dictionary<string, object> Paths { get; set; }
        public void AddPath(string Key, object Path)
        {
            Paths ??= new Dictionary<string, object>();
            Key = Key.ToLower();
            if (Paths.ContainsKey(Key))
                Paths[Key] = Path;
            else
                Paths.TryAdd(Key, Path);
        }
        private string GetRootPath()
        {
            var PathArray = FormatRootPath
                .Split('/')
                .Select(Item =>
                {
                    if (!Item.Contains('{') && !Item.Contains('}'))
                        return Item;

                    if (Paths is null)
                        return "null";

                    var PathKey = Item
                        .TrimStart('{')
                        .TrimEnd('}')
                        .ToLower();

                    if (!Paths.TryGetValue(PathKey, out var Path))
                        return "null";

                    var PathString = Path.ToString();
                    return PathString;
                });

            var GetRootPath = string.Join('/', PathArray);
            return GetRootPath;
        }
    }

    public enum FileNameCaseType
    {
        None,
        Upper,
        Lower,
    }
}