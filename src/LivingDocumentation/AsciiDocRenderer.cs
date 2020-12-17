using LivingDocumentation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Pitstop.LivingDocumentation
{
    public class AsciiDocRenderer
    {
        public void Render()
        {
            var stringBuilder = new StringBuilder();

            RenderFileHeader(stringBuilder);

            stringBuilder.Append(new AggregateRenderer().Render());
            stringBuilder.Append(new EventsRenderer().Render());
            stringBuilder.Append(new CommandsRenderer().Render());

            var outputPath = Program.Options.OutputPath;
            File.WriteAllText(outputPath, stringBuilder.ToString());
            Console.WriteLine($"Generated documentation {outputPath}");
        }

        private static void RenderFileHeader(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("= This file contains generated content");
            stringBuilder.AppendLine("Edwin van Wijk <Edwin.van.Wijk@InfoSupport.com>");
            stringBuilder.AppendLine($"{Assembly.GetEntryAssembly().GetName().Version.ToString(3)}, {DateTime.Today:yyyy-MM-dd}");
            stringBuilder.AppendLine(":toc: left");
            stringBuilder.AppendLine(":toc-level: 3");
            stringBuilder.AppendLine(":sectnums:");
            stringBuilder.AppendLine(":icons: font");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("NOTE: This document has been automatically generated");
            stringBuilder.AppendLine();
        }
    }
}