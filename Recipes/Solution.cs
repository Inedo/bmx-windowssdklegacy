using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    internal sealed class Solution
    {
        private static readonly Regex ProjectRegex = new Regex(@"^\s*Project\(""[^""]+""\)\s*=\s*""(?<n>[^""]+)"",\s*""(?<p>[^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline);

        private List<SolutionProject> projects = new List<SolutionProject>();

        private Solution()
        {
        }

        public IList<SolutionProject> Projects
        {
            get { return this.projects; }
        }

        public static Solution Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            using (var stream = File.OpenRead(fileName))
            {
                return Load(stream);
            }
        }
        public static Solution Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            return Load(new StreamReader(stream));
        }
        public static Solution Load(TextReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            return LoadText(reader.ReadToEnd());
        }
        public static Solution LoadText(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException("text");

            var solution = new Solution();

            foreach (Match match in ProjectRegex.Matches(text))
            {
                var name = match.Groups["n"].Value;
                var path = match.Groups["p"].Value;

                try
                {
                    var extension = Path.GetExtension(path) ?? string.Empty;
                    if (extension.EndsWith("proj", StringComparison.OrdinalIgnoreCase))
                        solution.projects.Add(new SolutionProject(name, path));
                }
                catch
                {
                }
            }

            return solution;
        }
    }

    internal sealed class SolutionProject
    {
        public SolutionProject(string name, string projectPath)
        {
            this.Name = name;
            this.ProjectPath = projectPath;
        }

        public string Name { get; private set; }
        public string ProjectPath { get; private set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
