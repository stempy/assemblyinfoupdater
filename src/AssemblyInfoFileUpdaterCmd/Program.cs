using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;

namespace AssemblyInfoFileUpdaterCmd
{
    class Options
    {
        public string DirOrFilePath { get; set; }

        [Option('s')]
        public bool SilentMode { get; set; }

        [Option('p',Separator = ';')]
        public IEnumerable<string> Properties { get; set; }
    }

   


    class Program
    {
        private static Options options;


        static void Main(string[] args)
        {
            if (!args.Any())
            {
                Usage();
                return;
            }

            var path = args[0];


            var result = Parser.Default.ParseArguments<Options>(args);
            result.WithParsed(o =>
            {
                Console.WriteLine(o.DirOrFilePath);
                Console.WriteLine(string.Join("--",o.Properties));
                options = o;
            });


            var updateDic = options.Properties.ToDictionary(k => k.Split('=')[0], y => y.Split('=')[1]);

            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Directory {path} does not exist");
                Environment.ExitCode = 1;
                return;
            }


            var assemblyInfoFiles = Directory.GetFiles(path, "AssemblyInfo.cs", SearchOption.AllDirectories);

            var logLevel = options.SilentMode
                ? LogLevel.Minimal
                : LogLevel.Info;


            var asmUpdater = new AssemblyInfoFileUpdater(logLevel);
            var filesToUpdate = asmUpdater.GetUpdateResult(assemblyInfoFiles, updateDic);
            var updated = asmUpdater.UpdateAssemblyInfoFiles(filesToUpdate);

            if (!options.SilentMode)
            {
                foreach (var updateFileResult in updated)
                {
                    Console.WriteLine(
                        $"{updateFileResult.File} {updateFileResult.FileUpdated} {updateFileResult.Message}");
                }
            }


            if (Debugger.IsAttached)
            {
                Console.ReadKey();
            }

        }

        static void Usage()
        {
            Console.WriteLine("AssemblyInfoFileUpdaterCmd dir|file \"AssemblyDescription=somedesc;AssemblyCopyright=some copyright\"");
        }
    }
}
