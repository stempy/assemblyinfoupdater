using System.Collections.Generic;

namespace AssemblyInfoFileUpdaterCmd
{
    public class UpdateFileResult
    {
        public string File { get; set; }
        public IDictionary<string, string> UpdatedFromTo { get; set; }
        public AssemblyInfoFileDictionary AssemblyInfoDictionary { get; set; }
        public bool FileUpdated { get; set; }
        public string Message { get; set; }
    }
}