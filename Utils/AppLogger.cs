using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Shop.Utils;

public static class AppLogger
{
    public static void LogError(Exception ex, 
        string additionalInfo = "",
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var className = Path.GetFileNameWithoutExtension(filePath);
    
        Console.WriteLine($"""
                           ERROR [{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}]
                           Class: {className}
                           Method: {methodName}
                           Line: {lineNumber}
                           Add. info: {additionalInfo}
                           Exception: {ex.GetType().Name}
                           Message: {ex.Message}
                           StackTrace: {ex.StackTrace}
                           ---
                           """);
    }

}