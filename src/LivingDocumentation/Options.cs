using CommandLine;

namespace Pitstop.LivingDocumentation
{
    public class RuntimeOptions
    {
        [Option("input", Required = true, HelpText = "The analyzed solution output.")]
        public string InputPath { get; set; }

        [Option("output", Required = true, HelpText = "The location of the output.")]
        public string OutputPath { get; set; }

        //[Option("solutionPath", Required = true, HelpText = "The path to the solution to find feature files.")]
        //public string SolutionPath { get; set; }

        [Option('x', "experimental", HelpText = "Use experimental PlantUML features")]
        public bool Experimental { get; set; }
    }
}
