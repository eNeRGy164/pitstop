using LivingDocumentation.Uml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pitstop.LivingDocumentation
{
    internal class UmlFragmentRenderer
    {
        private static readonly string[] ExternalTargets = new[] { "[", "]" };

        public static void RenderTree(StringBuilder stringBuilder, IEnumerable<InteractionFragment> branch, Interactions tree, Predicate<Arrow> filter = null)
        {
            RenderTree(stringBuilder, branch, tree, null, filter);
        }

        private static void RenderTree(StringBuilder stringBuilder, IEnumerable<InteractionFragment> branch, Interactions tree, List<string> activations, Predicate<Arrow> filter)
        {
            if (activations == null) activations = new List<string>();

            var branchActivations = new List<string>(activations);

            foreach (var leaf in branch)
            {
                var leafActivations = new List<string>(activations);

                switch (leaf)
                {
                    case Interactions statementList:
                        RenderTree(stringBuilder, statementList.Fragments, tree, leafActivations, filter);
                        break;

                    case Arrow arrow:
                        RenderArrow(stringBuilder, arrow, branch.ToList(), tree, leafActivations, filter);
                        break;

                    case Alt alt:
                        leafActivations = new List<string>(branchActivations);
                        RenderGroup(stringBuilder, alt, tree, leafActivations, filter);
                        break;
                }

                branchActivations.AddRange(leafActivations.Except(branchActivations));
            }

            foreach (var branchActivation in branchActivations.Except(activations))
            {
                stringBuilder.AppendLine($"deactivate {branchActivation}");
            }
        }

        private static void RenderGroup(StringBuilder stringBuilder, Alt alt, Interactions tree, List<string> activations, Predicate<Arrow> filter)
        {
            var switchBuilder = new StringBuilder();

            foreach (var section in alt.Sections)
            {
                var sectionBuilder = new StringBuilder();

                RenderTree(sectionBuilder, section.Fragments, tree, new List<string>(activations), filter);

                if (sectionBuilder.Length > 0)
                {
                    var first = switchBuilder.Length == 0;
                    if (first)
                    {
                        switchBuilder.AppendLine("||5||");

                        if (string.IsNullOrWhiteSpace(section.GroupType))
                        {
                            switchBuilder.Append("alt");
                        }
                        else
                        {
                            if (section.GroupType == "stateMachine")
                            {
                                switchBuilder.Append($"group #Khaki stateMachine");
                            }
                            else
                            {
                                switchBuilder.Append($"group {(section.GroupType == "case" || section.GroupType == "switch" ? "switch" : section.GroupType)}");
                            }

                            if (section.GroupType == "case" || section.GroupType == "stateMachine")
                            {
                                switchBuilder.AppendLine();
                                switchBuilder.Append("else");
                            }
                        }
                    }
                    else
                    {
                        switchBuilder.Append("else");
                    }

                    switchBuilder.AppendLine(string.IsNullOrWhiteSpace(section.GroupType) || section.GroupType == "case" || section.GroupType == "stateMachine" ? $" {section.Label}" : $" [{section.Label}]");
                    switchBuilder.Append(sectionBuilder);
                    switchBuilder.AppendLine("||5||");
                }
            }

            if (switchBuilder.Length > 0)
            {
                stringBuilder.Append(switchBuilder);
                stringBuilder.AppendLine("end");
            }
        }

        private static void RenderArrow(StringBuilder stringBuilder, Arrow arrow, IReadOnlyList<InteractionFragment> scope, Interactions tree, List<string> activations, Predicate<Arrow> filter)
        {
            if (filter != null && !filter.Invoke(arrow))
            {
                return;
            }

            var target = (arrow.Source != "W" && arrow.Target == "A") ? "Q" : arrow.Target;
            var isParallel = !Program.Options.Experimental ? false : scope.OfType<Arrow>().TakeWhile(a => a != arrow).LastOrDefault()?.Name == arrow.Name;

            stringBuilder.AppendLine($"{(isParallel ? "& " : string.Empty)}{arrow.Source}-{arrow.Color}{(arrow.Dashed ? "-" : string.Empty)}>{target}:{arrow.Name}");

            if (!activations.Contains(arrow.Source))
            {
                // Scope was activated in current scope

                if (arrow.Target != "]" && arrow.Source != "A" && arrow.Source != "W" && !arrow.Source.StartsWith("x ", StringComparison.Ordinal) && scope.Descendants<Arrow>().Last(a => a.Source == arrow.Source) == arrow && arrow.Source != arrow.Target)
                {
                    // This is the last arrow from this source
                    stringBuilder.AppendLine($"deactivate {arrow.Source}");

                    activations.Remove(arrow.Source);
                }
            }

            if (!activations.Contains(arrow.Target))
            {
                // Scope was not activated by the parent scope

                if ((arrow.Target != "A" || arrow.Source == "W") && arrow.Target != "Q" && arrow.Target != "W" && !arrow.Target.StartsWith("x ", StringComparison.Ordinal) && arrow.Source != arrow.Target && scope.OfType<Arrow>().First(a => a.Target == arrow.Target) == arrow)
                {
                    var previousArrows = arrow.Ancestors().SelectMany(a => a.StatementsBeforeSelf()).OfType<Arrow>().ToList();
                    if (!previousArrows.Any(a => a.Target == arrow.Target) && !ExternalTargets.Contains(arrow.Target))
                    {
                        // There was no earlier activation in the current scope
                        stringBuilder.AppendLine($"activate {arrow.Target}");

                        activations.Add(arrow.Target);
                    }
                }
            }
        }
    }
}