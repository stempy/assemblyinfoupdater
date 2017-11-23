using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AssemblyInfoFileUpdater
{
    public class AssemblyInfoFileDictionary : Dictionary<string, string>, ICloneable
    {
        public class AsmItem
        {
            public int LineIdx { get; set; }
            public bool IsAssemblyItem { get; set; }
            public string Line { get; set; }

            public string Key { get; set; }
            public bool ValueHasQuotes { get; set; }
            public string Value { get; set; }

            public override string ToString()
            {
                var strValue = ValueHasQuotes ? $"\"{Value}\"" : Value;

                return IsAssemblyItem ? $"[assembly: {Key}({strValue})]" : Line;
            }
        }

        private const string AsmLineRegex = @"\[(assembly\:)(.*)(\(.*\))\]";
        private List<AsmItem> dic = new List<AsmItem>();

        public AssemblyInfoFileDictionary() { }

        public AssemblyInfoFileDictionary(string file)
        {
            ParseFromFile(file);
        }

        private AssemblyInfoFileDictionary(IEnumerable<AsmItem> newDic, string filePath)
        {
            dic = newDic.Select(x => new AsmItem()
            {
                IsAssemblyItem = x.IsAssemblyItem,
                Key = x.Key,
                Line = x.Line,
                LineIdx = x.LineIdx,
                Value = x.Value,
                ValueHasQuotes = x.ValueHasQuotes
            }).ToList();

            RefreshBaseDictionary(filePath);
        }

        public object Clone()
        {
            var d = new AssemblyInfoFileDictionary(dic, "unknown");
            return d;
        }

        private int GetLastIdx()
        {
            var res = dic.Where(x => x.IsAssemblyItem).OrderByDescending(x => x.LineIdx).FirstOrDefault();
            if (res != null)
            {
                var idx = res.LineIdx;
                return idx;
            }
            return -1;
        }

        new public string this[string key]
        {
            get
            {
                return base[key];
            }
            set
            {
                var item = this.dic.FirstOrDefault(x => x.Key == key);
                if (item != null)
                {
                    // existing item update
                    item.Value = value;
                    base[key] = value;
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public new void Add(string key, string value)
        {
            var lastIdx = GetLastIdx();
            var hasQuotes = key != "Guid" && key != "ComVisible";

            var asmItem = new AsmItem()
            {
                LineIdx = lastIdx + 1,
                IsAssemblyItem = true,
                Key = key,
                Value = value,
                ValueHasQuotes = hasQuotes
            };


            if (lastIdx < dic.Count - 1)
            {
                dic.Insert(lastIdx, asmItem);
            }
            else
            {
                dic.Add(asmItem);
            }


            base.Add(key, value);
        }

        public new void Clear()
        {
            dic.Clear();
            base.Clear();
        }

        public void ParseFromFile(string filePath)
        {
            this.Clear();
            var lines = File.ReadAllLines(filePath).ToList();
            dic = Parse(lines);
            RefreshBaseDictionary(filePath);
        }

        private void RefreshBaseDictionary(string filePath)
        {
            foreach (var d in dic.Where(x => x.IsAssemblyItem))
            {
                if (this.ContainsKey(d.Key))
                {
                    var val = this[d.Key];
                    throw new Exception($"File: {filePath} contains duplicate keys: {d.Key}");
                }

                base.Add(d.Key, d.Value);
            }
        }


        public void SaveToFile(string filePath)
        {
            var fileStr = WriteString();
            File.WriteAllText(filePath, fileStr);
        }


        public string WriteString()
        {
            var sb = new StringBuilder();
            foreach (var l in dic)
            {
                sb.AppendLine(l.ToString());
            }
            return sb.ToString();
        }


        private AsmItem ParseAssemblyLine(string line, int idx)
        {
            if (!line.TrimStart().StartsWith("//") && line.Contains("[assembly:"))
            {
                //var key = 
                var regex = Regex.Match(line, AsmLineRegex);
                if (regex.Success)
                {
                    var asm = regex.Groups[1].Value;
                    var key = regex.Groups[2].Value.Trim();
                    var bracketedVal = regex.Groups[3].Value.Trim(new char[] { '(', ')' });
                    var hasQuotes = bracketedVal[0].Equals('"') && bracketedVal.EndsWith("\"");

                    AsmItem item = new AsmItem()
                    {
                        Key = key,
                        ValueHasQuotes = hasQuotes,
                        Value = bracketedVal.Trim(new[] { '"' }),
                        LineIdx = idx,
                        IsAssemblyItem = true
                    };
                    return item;
                }
                else
                {
                    return new AsmItem()
                    {
                        LineIdx = idx,
                        Line = line
                    };
                }
            }

            return new AsmItem()
            {
                LineIdx = idx,
                Line = line
            };
        }

        private List<AsmItem> Parse(IEnumerable<string> lines)
        {

            var d = new List<AsmItem>();
            // parse lines
            var idx = 0;
            foreach (var line in lines)
            {
                d.Add(ParseAssemblyLine(line, idx));
                idx++;
            }

            return d;
        }

    }

}
