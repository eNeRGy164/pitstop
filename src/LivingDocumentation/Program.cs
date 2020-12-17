using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using LivingDocumentation;
using Newtonsoft.Json;

namespace Pitstop.LivingDocumentation
{
    public class Program
    {
        internal static RuntimeOptions Options;
        internal static List<TypeDescription> Types;

        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<RuntimeOptions>(args)
                .WithParsed(RunApplication)
                .WithNotParsed(e => Environment.Exit(1));
        }

        private static void RunApplication(RuntimeOptions options)
        {
            Options = options;

            if (!File.Exists(Options.InputPath))
            {
                throw new FileNotFoundException("Input file is not found");
            }

            string directory;
            if (Path.GetExtension(Options.OutputPath).Length == 0)
            {
                directory = Path.GetFullPath(Options.OutputPath);
            }
            else
            {
                directory = Directory.GetParent(Path.GetFullPath(Options.OutputPath)).FullName;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Read analysis
            var fileContents = File.ReadAllText(Options.InputPath);

            Types = JsonConvert.DeserializeObject<List<TypeDescription>>(fileContents, JsonDefaults.DeserializerSettings()).Where(t => !t.Namespace.Contains("LivingDocumentation", StringComparison.Ordinal)).ToList();

            Types.PopulateInheritedBaseTypes();
            Types.PopulateInheritedMembers();

            new AsciiDocRenderer().Render();
        }
    }
}