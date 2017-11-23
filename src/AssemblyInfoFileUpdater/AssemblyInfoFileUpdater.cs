using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AssemblyInfoFileUpdater
{
    internal static class InternalExtensions
    {
        public static void Dump(this string obj)
        {
            Console.WriteLine(obj);
        }
    }


    public class AssemblyInfoFileUpdater
    {
        public enum LogLevel
        {
            Trace = 1,
            Debug = 2,
            Info = 3,
            Minimal = 4,
            Errors = 5
        }

        public LogLevel LogVerbosity { get; set; } = LogLevel.Trace;


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
            Log(line);

            foreach (var ur in updateResults.ToList())
            {
                try
                {
                    ur.AssemblyInfoDictionary.SaveToFile(ur.File);
                    Log($"{ur.File} updated with {ur.UpdatedFromTo.Count} changes");
                    ur.FileUpdated = true;
                }
                catch (Exception ex)
                {
                    ur.Message = ex.Message;
                }
            }

            return updateResults;
        }

        #region [Helpers]

        private void Log(string message)
        {
            if (LogVerbosity <= LogLevel.Info)
            {
                message.Dump();
            }
        }

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