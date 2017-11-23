using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssemblyInfoFileUpdaterCmd
{
    internal static class InternalExtensions
    {
        public static void Dump(this string obj)
        {
            Console.WriteLine(obj);
        }
    }


    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message);
        void LogError(Exception ex, string message);
        void LogWarn(string message);
        void LogDebug(string message);
    }

    public class Logger : ILogger
    {
        public void LogInfo(string message)
        {
            
        }

        public void LogError(string message)
        {
            
        }

        public void LogError(Exception ex, string message)
        {
            
        }

        public void LogWarn(string message)
        {
            
        }

        public void LogDebug(string message)
        {
            throw new NotImplementedException();
        }

        public void Log(LogLevel logLevel, string message)
        {
            
        }
    }

    public enum LogLevel
    {
        Trace = 1,
        Debug = 2,
        Info = 3,
        Minimal = 4,
        Errors = 5
    }

    public class AssemblyInfoFileUpdater
    {
        

        public LogLevel LogVerbosity { get; set; } = LogLevel.Trace;
        private readonly ILogger _log=new Logger();

        public AssemblyInfoFileUpdater(LogLevel logLevel)
        {
            LogVerbosity = logLevel;
        }

        public AssemblyInfoFileUpdater() { }

        // Get update result but dont actually update files
        public UpdateFileResult GetUpdateResult(string file, IDictionary<string, string> updateItems)
        {
            var asmInfoDic = new AssemblyInfoFileDictionary(file);
            var newDic = UpdateItems(asmInfoDic, updateItems);
            var cmp = CompareDic(asmInfoDic, newDic);
            return new UpdateFileResult()
            {
                File = file,
                UpdatedFromTo = cmp,
                AssemblyInfoDictionary = newDic
            };
        }

        // get update results for multiple files but dont actually update files
        public IEnumerable<UpdateFileResult> GetUpdateResult(IEnumerable<string> files, IDictionary<string, string> updateItems)
        {
            var fr = new List<UpdateFileResult>();
            foreach (var f in files)
            {
                var result = GetUpdateResult(f, updateItems);
                fr.Add(result);
            }
            return fr;
        }


        // perform actual updates on files
        public IEnumerable<UpdateFileResult> UpdateAssemblyInfoFiles(IEnumerable<UpdateFileResult> updateResults)
        {
            var line = $"Files: {updateResults.Count()}";
            _log.LogInfo(line);

            foreach (var ur in updateResults.ToList())
            {
                try
                {
                    ur.AssemblyInfoDictionary.SaveToFile(ur.File);
                    _log.LogDebug($"{ur.File} updated with {ur.UpdatedFromTo.Count} changes");
                    ur.FileUpdated = true;
                }
                catch (Exception ex)
                {
                    ur.Message = ex.Message;
                }
            }

            var countCompleted = updateResults.Count(x => x.FileUpdated);
            var count = updateResults.Count();
            var logResults = $"{countCompleted}/{count} AssemblyInfo.cs files updated";

            _log.LogInfo(logResults);
            return updateResults;
        }

        #region [Helpers]

      

        private AssemblyInfoFileDictionary UpdateItems(AssemblyInfoFileDictionary dic, IDictionary<string, string> updateItems)
        {
            var dic2 = (AssemblyInfoFileDictionary)dic.Clone();
            const string regexTpl = @"{{([^}]*)}}";
            string getReplaceValue(string value)
            {
                value.Replace("{{Date}}", DateTime.Now.ToString());
                var matches = Regex.Matches(value, regexTpl);
                foreach (var m in matches)
                {
                    //m.Dump();
                }

                return value;
            }


            foreach (var di in updateItems)
            {
                dic2[di.Key] = getReplaceValue(di.Value);
            }
            return dic2;
        }



        private IDictionary<string, string> CompareDic(AssemblyInfoFileDictionary d1, AssemblyInfoFileDictionary d2)
        {
            var d = new Dictionary<string, string>();

            // existing
            foreach (var pair in d1)
            {
                var d2pair = d2.FirstOrDefault(x => x.Key == pair.Key);
                if (d2pair.Value != null && pair.Value != d2pair.Value)
                {
                    // compare
                    d.Add($"{pair.Key}==>{pair.Value}", $"{d2pair.Key}==>{d2pair.Value}");
                }
            }

            // new
            var d1Keys = d1.Select(z => z.Key).ToArray();
            var d2New = d2.Where(x => !d1Keys.Contains(x.Key));
            foreach (var newD2Pair in d2New)
            {
                var key = newD2Pair.Key;
                d.Add("", $"{key}==>{newD2Pair.Value}");
            }

            return d;
        }



        #endregion
    }
}