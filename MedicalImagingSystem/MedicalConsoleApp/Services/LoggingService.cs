using Serilog;

namespace MedicalConsoleApp.Services
{
    public static class LoggingService
    {
        public static void Info(string message) => Log.Information(message);
        public static void Error(string message) => Log.Error(message);
    }
}