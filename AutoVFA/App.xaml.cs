using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using OfficeOpenXml;

namespace AutoVFA
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            Current.DispatcherUnhandledException += (s, e) =>
            {
                DirectoryInfo di = Directory.CreateDirectory("Unhandled");
                var fileName = Path.Combine(di.FullName,
                    $"auto-vfa.unhandled-{DateTime.Now:yyyyMMddTHHmmss}.log");
                using FileStream fs = File.Create(fileName);
                fs.Write(JsonSerializer.SerializeToUtf8Bytes(new
                {
                    Message = e.Exception.Message.ToString(),
                    Stacktrace = e.Exception.StackTrace,
                    MessageInner = e.Exception.InnerException?.Message.ToString(),
                    StacktraceInner = e.Exception.InnerException?.StackTrace
                }, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
            };
        }
    }
}