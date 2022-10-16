using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoonSharp.Interpreter;

namespace HotReloadDemo
{
    public struct TScript
    {
        public string context;
        public DynValue dynValue;
        public string[] globals;
    }

    public static class Logger
    {
        public static void Error(object str)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.ResetColor();
        }
        public static void Sucess(object str)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(str);
            Console.ResetColor();
        }
        public static void Warn(object str)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(str);
            Console.ResetColor();
        }
    }

    internal class Program
    {
        private static Script _script;
        private static string path = @"C:\Projects\HotReloadDemo\Sample";

        private static Dictionary<string, TScript> _scriptMap;

        private static DynValue DoString(string code, string fileName = "")
        {
            try
            {
                return _script.DoString(code);
            }
            catch (ScriptRuntimeException e)
            {
                if (fileName != "")
                    Console.WriteLine($"{fileName}: {e.DecoratedMessage}");
                Console.WriteLine(e.DecoratedMessage);
                return null;
            }
            catch (SyntaxErrorException e)
            {
                if (fileName != "")
                    Console.WriteLine($"{fileName}: {e.DecoratedMessage}");
                Console.WriteLine(e.DecoratedMessage);
                return null;
            }
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Logger.Warn($"file chanced: {e.FullPath}");
            OnFileDeleted(sender, e);
            OnFileCreated(sender, e);
        }

        private static void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            var newFile = File.ReadAllText(e.FullPath);
            var dyn = DoString(newFile, newFile);
            _scriptMap.Add(e.FullPath, new TScript()
            {
                context = newFile,
                dynValue = dyn
            });
        }
        
        private static void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            if (!_scriptMap.TryGetValue(e.FullPath, out var dynValue)) 
                return;
            
            if (dynValue.globals.Length > 0)
            {
                foreach (var globalKey in dynValue.globals)
                {
                    _script.Globals.Remove(globalKey);
                }
            }

            _scriptMap.Remove(e.FullPath);
        }

        private static FileSystemSafeWatcher _watcher;
        
        public static void Main(string[] args)
        {
            _watcher = new FileSystemSafeWatcher();
            _watcher.Path = path;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.EnableRaisingEvents = true;
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileCreated;
            _watcher.Deleted += OnFileDeleted;
            
            //
            Script.WarmUp();
            _script = new Script();
            _scriptMap = new Dictionary<string, TScript>();
            
            foreach (var s in Directory.GetFiles(path))
            {
                var raw = _script.Globals.Keys.Select(dynValue => dynValue.CastToString()).ToArray();
                
                //
                var code = File.ReadAllText(s);
                var res = DoString(code, s);
                var globals = _script.Globals.Keys.Select(dynValue => dynValue.CastToString()).ToArray();

                var temp = globals.Where(s1 => !raw.Contains(s1)).ToArray();
                
                _scriptMap.Add(s, new TScript()
                {
                    context = code,
                    dynValue = res,
                    globals = temp
                });
            }
            
            Console.ReadKey();
        }
    }
}