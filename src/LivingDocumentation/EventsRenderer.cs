using LivingDocumentation;
using LivingDocumentation.Uml;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pitstop.LivingDocumentation
{
    public class EventsRenderer
    {
        public StringBuilder Render()
        {
            var stringBuilder = new StringBuilder();

            AsciiDocHelper.BeginSection(stringBuilder, "events");

            var events = Program.Types.Where(t => t.IsEvent()
            // && t.Name == "MaintenanceJobFinished"
            ).ToList();
            foreach (var groupedType in events.GroupBy(e => e.DisplayName()))
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
                    stringBuilder.AppendLine("*Event is deprecated* + ");
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        stringBuilder.AppendLine($" {message}");
                    }
                    stringBuilder.AppendLine("====");
                    stringBuilder.AppendLine();
                }

                if (groupedType.Any(t => !t.HasReceiverInSameNamespace()))
                {
                    stringBuilder.AppendLine(".Event published by");
                    foreach (var t in groupedType.Where(t => !t.HasReceiverInSameNamespace()))
                    {
                        stringBuilder.AppendLine($"* {t.Namespace.Split('.').Reverse().Skip(1).First().SplitCamelCase()}");
                    }
                    stringBuilder.AppendLine();
                }

                if (groupedType.Any(t => t.HasReceiverInSameNamespace()))
                {
                    stringBuilder.AppendLine(".Event received by");
                    foreach (var t in groupedType.Where(t => t.HasReceiverInSameNamespace()))
                    {
                        stringBuilder.AppendLine($"* {t.Namespace.Split('.').Skip(1).First().SplitCamelCase()}");
                    }
                    stringBuilder.AppendLine();
                }

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

                AsciiDocHelper.BeginTag(stringBuilder, $"events-{type.DisplayName().ToLowerInvariant()}");
                RenderEventDiagram(stringBuilder, type);
                AsciiDocHelper.EndTag(stringBuilder, $"events-{type.DisplayName().ToLowerInvariant()}");
            }

            AsciiDocHelper.EndSection(stringBuilder, "events");

            return stringBuilder;
        }

        private void RenderEventDiagram(StringBuilder stringBuilder, TypeDescription type)
        {
            var services = new List<string>();
            var interactionTraverser = new InteractionTraverser();

            var interactions = interactionTraverser.ExtractConcequences(type, services);
            if (interactions.Fragments.Count == 0)
            {
                return;
            }

            var callingServices = Program.Types.GetSourceCommands(type);

            var subInteraction = new Interactions();
            foreach (var fragment in interactions.Fragments)
            {
                if (fragment is Arrow a && a.Source == "A")
                {
                    if (subInteraction.Fragments.Count > 0)
                    {
                        RenderSubDiagram(stringBuilder, type, services, subInteraction, callingServices);
                    }

                    subInteraction = new Interactions();
                    a.Source = callingServices[0].Service();
                }

                subInteraction.AddFragment(fragment);
            }

            RenderSubDiagram(stringBuilder, type, services, subInteraction, callingServices);
        }

        private static void RenderSubDiagram(StringBuilder stringBuilder, TypeDescription type, List<string> services, Interactions interactions, IReadOnlyList<TypeDescription> callingServices)
        {
            var innerBuilder = new StringBuilder();

            UmlFragmentRenderer.RenderTree(innerBuilder, interactions.Fragments, interactions);

            var handler = interactions.Fragments.OfType<Arrow>().First().Target.SplitCamelCase();

            stringBuilder.AppendLine($"=== {handler}");
            stringBuilder.AppendLine($".{AsciiDocHelper.FormatChapter(type.DisplayName())} Event as handled by {handler}");
            stringBuilder.AppendLine($"[plantuml]");
            stringBuilder.AppendLine("....");
            stringBuilder.AppendLine("@startuml");
            if (Program.Options.Experimental) stringBuilder.AppendLine("!pragma teoz true");
            AsciiDocHelper.DefaultSequenceDiagramStyling(stringBuilder);
            stringBuilder.AppendLine("scale max 4096 height");
            //AsciiDocHelper.Legend(stringBuilder);
            foreach (var callingService in callingServices)
            {
                stringBuilder.AppendLine($"participant \"{callingService.Service().AsServiceDisplayName()}\" as {callingService.Service()}");
            }
            stringBuilder.AppendLine("box \"Services\" #Ivory");
            foreach (var service in services.Where(s => interactions.Fragments.Descendants<Arrow>().Any(a => a.Target == s)))
            {
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