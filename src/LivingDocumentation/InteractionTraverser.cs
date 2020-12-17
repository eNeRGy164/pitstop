using LivingDocumentation;
using LivingDocumentation.Uml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pitstop.LivingDocumentation
{
    internal class InteractionTraverser
    {
        private readonly Stack<string> activations = new Stack<string>();

        public Interactions ExtractConcequences(TypeDescription originatingMessage, List<string> services, string previousService = null, string inAlternativeFlow = null, List<ArgumentDescription> arguments = null)
        {
            var result = new Interactions();

            var handlers = Program.Types.HandlersFor(originatingMessage);

            foreach (var handler in handlers)
            {
                var levelName = handler.Service();

                var source = previousService ?? "A";
                var target = levelName ?? "Q";

                var arrow = new Arrow
                {
                    Source = source,
                    Target = target,
                    Name = AsciiDocHelper.FormatChapter(originatingMessage.DisplayName()),
                    Color = AsciiDocHelper.ArrowColor(originatingMessage)
                };
                result.AddFragment(arrow);

                if (!activations.Contains(levelName))
                {
                    activations.Push(levelName);
                }

                if (!services.Contains(target)) services.Add(target);

                var statements = handler.HandlingMethod(originatingMessage).Statements;
                foreach (var statement in statements)
                {
                    var statementInteractions = TraverseBody(services, handler, previousService ?? levelName, statement, inAlternativeFlow ?? levelName);
                    if (statementInteractions.Fragments.Count > 0) result.AddFragments(statementInteractions.Fragments);
                }

                if (activations.Count > 0 && activations.Peek() == levelName && levelName != inAlternativeFlow)
                {
                    activations.Pop();
                }
            }

            return result;
        }

        private Interactions TraverseBody(List<string> services, TypeDescription handler, string serviceName, Statement statement, string inAlternativeFlow)
        {
            switch (statement)
            {
                case InvocationDescription invocation when invocation.IsMessageCreation():

                    var message = Program.Types.FirstOrDefault(invocation.Arguments.Skip(invocation.Name == "RaiseEvent" ? 0 : 1).First().Type);
                    if (message == null)
                    {
                        Console.WriteLine($"Error while traversing code stucture.");
                        Console.WriteLine($" --> '{invocation.Arguments.First().Type}' not found in the list of analysed types. Documentation will be incomplete.");
                        break;
                    }

                    return ExtractConcequences(message, services, handler.Service(), inAlternativeFlow, invocation.Arguments);

                case InvocationDescription invocation when invocation.IsMailMessage():
                    {
                        var result = new Interactions();

                        var subject = "Email" + (invocation.Arguments.Count > 2 ? " \"" + invocation.Arguments[2].Text.Trim('"').Trim() + "\"" : "");

                        var arrow = new Arrow
                        {
                            Source = serviceName,
                            Target = "]",
                            Name = AsciiDocHelper.FormatChapter(subject)
                        };
                        result.AddFragment(arrow);

                        return result;
                    }

                case InvocationDescription invocation when invocation.IsDatabaseAction():
                    {
                        var result = new Interactions();

                        var target = invocation.ContainingType.GenericTypes().FirstOrDefault()?.ToTypeDescription().Name;
                        if (invocation.Name == "ExecuteAsync" || invocation.Name == "QueryAsync")
                        {
                            target = invocation.Arguments.Last().Type.ToTypeDescription()?.Name;
                        }
                        else
                        {

                            var argument = invocation.Arguments.First().Type;
                            while (argument.IsGeneric())
                            {
                                argument = argument.GenericTypes().FirstOrDefault();
                            }
                            target = argument?.ToTypeDescription().Name;
                        }


                        var arrow = new Arrow
                        {
                            Source = serviceName,
                            Target = serviceName + "_" + (target ?? "anonymous") + "_Entity",
                            Name = invocation.Name.EndsWith("Async") ? invocation.Name[0..^5] : invocation.Name,
                            Color = "[#Black]"
                        };
                        result.AddFragment(arrow);
                        if (!services.Contains(arrow.Target)) services.Add(arrow.Target);
                        return result;
                    }

                case InvocationDescription invocation:
                    {
                        var result = new Interactions();
                        var consequences = Program.Types.GetInvocationConsequenceStatements2(invocation).Where(s => s != invocation);

                        foreach (var consequence in consequences)
                        {
                            var invocationInteractions = TraverseBody(services, handler, serviceName, consequence, inAlternativeFlow);
                            if (invocationInteractions.Fragments.Count > 0) result.AddFragments(invocationInteractions.Fragments);
                        }

                        return result;
                    }

                case ForEach forEachStatement:
                    {
                        var result = new Interactions();
                        var interactions = new List<InteractionFragment>();

                        foreach (var invocation in forEachStatement.Statements)
                        {
                            var invocationInteractions = TraverseBody(services, handler, serviceName, invocation, inAlternativeFlow);
                            if (invocationInteractions.Fragments.Count > 0) interactions.AddRange(invocationInteractions.Fragments);
                        }

                        if (interactions.Count > 0)
                        {
                            var alt = new Alt();

                            var altSection = new AltSection();
                            altSection.AddFragments(interactions);
                            altSection.GroupType = "forEach";
                            altSection.Label = forEachStatement.Expression;

                            alt.AddSection(altSection);

                            result.AddFragment(alt);
                        }

                        return result;
                    }

                case Switch switchStatement:
                    {
                        var result = new Interactions();

                        var alt = new Alt();

                        foreach (var section in switchStatement.Sections)
                        {
                            var interactions = new List<InteractionFragment>();

                            foreach (var invocation in section.Statements)
                            {
                                var invocationInteractions = TraverseBody(services, handler, serviceName, invocation, inAlternativeFlow);
                                if (invocationInteractions.Fragments.Count > 0) interactions.AddRange(invocationInteractions.Fragments);
                            }

                            if (interactions.Count > 0)
                            {
                                var altSection = new AltSection();
                                if (alt.Sections.Count == 0) { altSection.GroupType = "case"; };
                                altSection.AddFragments(interactions);
                                altSection.Label = section.Labels.Aggregate(string.Empty, (s1, s2) => s1 + s2);
                                alt.AddSection(altSection);
                            }
                        }

                        if (alt.Sections.Count > 0)
                        {
                            result.AddFragment(alt);
                        }

                        return result;
                    }

                case If ifStatement:
                    {
                        var result = new Interactions();

                        var alt = new Alt();

                        foreach (var section in ifStatement.Sections)
                        {
                            var interactions = new List<InteractionFragment>();

                            foreach (var invocation in section.Statements)
                            {
                                var invocationInteractions = TraverseBody(services, handler, serviceName, invocation, inAlternativeFlow);
                                if (invocationInteractions.Fragments.Count > 0) interactions.AddRange(invocationInteractions.Fragments);
                            }

                            if (interactions.Count > 0)
                            {
                                var altSection = new AltSection();
                                if (alt.Sections.Count == 0) { altSection.GroupType = "if"; };
                                altSection.AddFragments(interactions);
                                altSection.Label = section.Condition;
                                alt.AddSection(altSection);
                            }
                        }

                        if (alt.Sections.Count > 0)
                        {
                            result.AddFragment(alt);
                        }

                        return result;
                    }
            }

            return new Interactions();
        }
    }
}
