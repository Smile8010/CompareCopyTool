using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PublishCli.Models
{
    public class PublishOptions
    {
        public string SourcePath { get; set; }
        public string PublishPath { get; set; }

        public string DistPath { get; set; }

        public bool ClearDist { get; set; }

        public List<string> ExcludeNames { get; set; }

        public List<string> ExcludeFolders { get; set; }


        public bool ReplacePublishFile { get; set; } = false;
        public void ChkValid()
        {
            if (string.IsNullOrWhiteSpace(SourcePath)) throw new ArgumentNullException(nameof(SourcePath));
            if (string.IsNullOrWhiteSpace(PublishPath)) PublishPath = Path.Combine(Directory.GetCurrentDirectory(),".publish");
            if (string.IsNullOrWhiteSpace(DistPath)) DistPath = Path.Combine(Directory.GetCurrentDirectory(),"dist");

            if (ExcludeNames == null) ExcludeNames = new List<string>();
            if (ExcludeFolders == null) ExcludeFolders = new List<string>();
        }
    }
}
