using LivingDocumentation;
using LivingDocumentation.Uml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pitstop.LivingDocumentation
{
    public class CommandsRenderer
    {
        public StringBuilder Render()
        {
            var stringBuilder = new StringBuilder();

            AsciiDocHelper.BeginSection(stringBuilder, "commands");

            var commands = Program.Types.Where(t => t.IsCommand()
            //&& t.Name == "FinishMaintenanceJob"
            ).ToList();
            foreach (var groupedType in commands.GroupBy(e => e.DisplayName()))
            {
                var type = groupedType.First();

                stringBuilder.AppendLine($"== {AsciiDocHelper.FormatChapter(type.DisplayName())}");
                stringBuilder.AppendLine();

                if (!string.IsNullOrWhiteSpace(type.DocumentationComments?.Summary))
                {
                    stringBuilder.AppendLine(type.DocumentationComments?.Summary);
                    stringBuilder.AppendLine();
                }

                if (type.IsDeprecated(out var message))
                {
                    stringBuilder.AppendLine("[IMPORTANT]");
                    stringBuilder.AppendLine("====");
                    stringBuilder.AppendLine("*Command is deprecated* + ");
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        stringBuilder.AppendLine($" {message}");
                    }
                    stringBuilder.AppendLine("====");
                    stringBuilder.AppendLine();
                }

                //if (groupedType.Any(t => !t.HasReceiverInSameNamespace()))
                //{
                //    stringBuilder.AppendLine(".Command sent by");
                //    foreach (var t in groupedType.Where(t => !t.HasReceiverInSameNamespace()))
                //    {
                //        stringBuilder.AppendLine($"* {t.Namespace.Split('.').Reverse().Skip(1).First().SplitCamelCase()}");
                //    }
                //    stringBuilder.AppendLine();
                //}

                //if (groupedType.Any(t => t.HasReceiverInSameNamespace()))
                //{
                //    stringBuilder.AppendLine(".Command handled by");
                //    foreach (var t in groupedType.Where(t => t.HasReceiverInSameNamespace()))
                //    {
                //        stringBuilder.AppendLine($"* {t.Namespace.Split('.').Skip(1).First().SplitCamelCase()}");
                //    }
                //    stringBuilder.AppendLine();
                //}

                if (groupedType.SelectMany(t => t.Fields).Any())
                {
                    stringBuilder.AppendLine("[caption=]");
                    stringBuilder.AppendLine(".Payload fields");
                    stringBuilder.AppendLine("[%header,cols=\"s,1,3\"]");
                    stringBuilder.AppendLine("|===");
                    stringBuilder.AppendLine("|Attribute|Type|Description");

                    foreach (var argument in groupedType.SelectMany(t => t.Fields).GroupBy(f => (f.Type, f.Name)).Select(g => g.First()).OrderBy(f => f.Name))
                    {
                        var argumentType = argument.Type.ToTypeDescription(Program.Types);

                        stringBuilder.Append('|');
                        stringBuilder.AppendLine(argument.Name);

                        stringBuilder.Append('|');
                        stringBuilder.AppendLine(argument.Type.ForDiagram());

                        stringBuilder.Append('|');
                        stringBuilder.AppendLine(argument.DocumentationComments?.Summary);
                        stringBuilder.AppendLine();
                    }

                    stringBuilder.AppendLine("|===");
                    stringBuilder.AppendLine();
                }

                foreach (var handler in Program.Types.CommandHandlersFor(type))
                {
                    AsciiDocHelper.BeginTag(stringBuilder, $"commands-{type.DisplayName().ToLowerInvariant()}");
                    RenderCommandDiagram(stringBuilder, type);
                    AsciiDocHelper.EndTag(stringBuilder, $"commands-{type.DisplayName().ToLowerInvariant()}");
                }
            }

            AsciiDocHelper.EndSection(stringBuilder, "commands");

            return stringBuilder;
        }

        private void RenderCommandDiagram(StringBuilder stringBuilder, TypeDescription type)
        {
            var services = new List<string>();
            var interactionTraverser = new InteractionTraverser();

            var interactions = interactionTraverser.ExtractConcequences(type, services);

            var innerBuilder = new StringBuilder();

            var commandArrow = interactions.Fragments.First();
            var callingServices = Program.Types.GetSourceCommands(type);
            if (callingServices.Count == 1)
            {
                interactions.Fragments.OfType<Arrow>().First().Source = callingServices[0].Service();
            }

            UmlFragmentRenderer.RenderTree(innerBuilder, interactions.Fragments, interactions);

            stringBuilder.AppendLine($".{AsciiDocHelper.FormatChapter(type.DisplayName())} Command");
            stringBuilder.AppendLine($"[plantuml]");
            stringBuilder.AppendLine("....");
            stringBuilder.AppendLine("@startuml");
            if (Program.Options.Experimental) stringBuilder.AppendLine("!pragma teoz true");
            AsciiDocHelper.DefaultSequenceDiagramStyling(stringBuilder);
            stringBuilder.AppendLine("scale max 4096 height");
            AsciiDocHelper.Legend(stringBuilder);
            foreach (var callingService in callingServices)
            {
                stringBuilder.AppendLine($"participant \"{callingService.Service().AsServiceDisplayName()}\" as {callingService.Service()}");
            }
            stringBuilder.AppendLine("box \"Services\" #Ivory");
            foreach (var service in services)
            {
                //stringBuilder.AppendLine($"participant \"{service.AsServiceDisplayName()}\" as {service}");
                if (service.EndsWith("Entity"))
                {
                    stringBuilder.AppendLine($"entity \"{service.Split('_').Skip(1).First()}\" as {service}");
                }
                else
                {
                    stringBuilder.AppendLine($"participant \"{service.AsServiceDisplayName()}\" as {service}");
                }
            }
            stringBuilder.AppendLine("end box");

            stringBuilder.Append(innerBuilder);

            stringBuilder.AppendLine($"@enduml");
            stringBuilder.AppendLine("....");
        }
    }
}