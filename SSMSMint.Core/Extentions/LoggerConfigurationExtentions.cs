using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Text;

namespace SSMSMint.Core.Extentions;

public static class LoggerConfigurationExtentions
{
    public static void LoadCustomConfiguration(this ISetupBuilder builder)
    {
        var layout = "${date:format=yyyy-MM-dd HH\\:mm\\:ss.ff} | [${level}] | ${logger} | ${message} | ${exception}";
        // Логи пишем в %LocalAppData%\SSMSMint
        string logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SSMSMint",
            "log.txt");

        builder.LoadConfiguration(_builder =>
        {
            _builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile(
                fileName: logPath,
                layout: layout,
                archiveAboveSize: 1000000, // 1 МБ (логи ротируются при превышении)
                maxArchiveFiles: 5, // храним 5 последних логов
                concurrentWrites: true,
                encoding: Encoding.UTF8,
                lineEnding: LineEndingMode.LF,
                keepFileOpen: true
            );
        });
    }
}
