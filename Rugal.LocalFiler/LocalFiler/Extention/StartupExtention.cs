using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rugal.LocalFiler.Model;
using Rugal.LocalFiler.Service;

namespace Rugal.LocalFiler.Extention
{
    public static class StartupExtention
    {
        public static IServiceCollection AddLocalFiler(this IServiceCollection Services, IConfiguration Configuration)
        {
            var Setting = NewSetting(Configuration);
            AddLocalFiler_Setting(Services, Setting);
            AddLocalFiler_Service(Services);
            return Services;
        }
        public static IServiceCollection AddLocalFiler(this IServiceCollection Services, IConfiguration Configuration,
           Action<FilerSetting, IServiceProvider> SettingFunc)
        {
            var Setting = NewSetting(Configuration);
            AddLocalFiler_Setting(Services, Setting, SettingFunc);
            AddLocalFiler_Service(Services);
            return Services;
        }
        public static IServiceCollection AddLocalFiler_Setting(this IServiceCollection Services, FilerSetting Setting)
        {
            Services.AddSingleton(Setting);
            return Services;
        }
        public static IServiceCollection AddLocalFiler_Setting(this IServiceCollection Services, FilerSetting Setting, Action<FilerSetting, IServiceProvider> SettingFunc)
        {
            Services.AddSingleton(Provider =>
            {
                SettingFunc.Invoke(Setting, Provider);
                return Setting;
            });
            return Services;
        }
        public static IServiceCollection AddLocalFiler_Service(this IServiceCollection Services)
        {
            Services.AddSingleton<FilerService>();
            return Services;
        }
        private static FilerSetting NewSetting(IConfiguration Configuration)
        {
            var GetSetting = Configuration.GetSection("LocalFiler");
            _ = bool.TryParse(GetSetting["DefaultExtensionFromFile"], out var DefaultExtensionFromFile);
            _ = bool.TryParse(GetSetting["UseExtension"], out var UseExtension);
            _ = int.TryParse(GetSetting["ReadPerKb"], out var ReadPerKb);
            var Setting = new FilerSetting()
            {
                RootPath = GetSetting["RootPath"],
                SaveFileNameReplace = GetSetting["SaveFileNameReplace"],
                TempExtention = GetSetting["TempExtention"] ?? "tmp",
                ReadPerKb = ReadPerKb == 0 ? 1024 : ReadPerKb,
                DefaultExtensionFromFile = DefaultExtensionFromFile,
                UseExtension = UseExtension,
            };
            return Setting;
        }
    }
}