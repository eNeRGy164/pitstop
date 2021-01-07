using LivingDocumentation;
using LivingDocumentation.Uml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Pitstop.LivingDocumentation
{
    public static class Extensions
    {
        public static void RenderProperty(this PropertyDescription property, StringBuilder stringBuilder)
        {
            stringBuilder.ClassMember($"{property.Name}: {property.Type.ForDiagram()}", property.IsStatic(), visibility: property.ToUmlVisibility());
        }

        public static void RenderStereoType(this TypeDescription type, StringBuilder stringBuilder)
        {
            stringBuilder.Append("<<");
            if (type.IsEnumeration()) stringBuilder.Append("enumeration");
            if (type.IsAggregateRoot()) stringBuilder.Append("(R,LightGreen)root");
            if (type.IsValueObject() || type.IsAggregateId()) stringBuilder.Append("(O,LightBlue)value object");
            if (type.IsEntity()) stringBuilder.Append("entity");
            stringBuilder.Append(">> ");
        }

        public static void RenderMethod(this MethodDescription method, StringBuilder stringBuilder)
        {
            stringBuilder.ClassMember($"{method.Name}({method.Parameters.Select(p => p.Name).Aggregate("", (s, a) => a + "," + s, s => s.Trim(','))})", method.IsStatic(), visibility: method.ToUmlVisibility());
        }

        public static TypeDescription ToTypeDescription(this string type)
        {
            return type.ToTypeDescription(Program.Types);
        }

        public static TypeDescription ToTypeDescription(this string type, IReadOnlyList<TypeDescription> types)
        {
            return types.FirstOrDefault(type);
        }

        public static bool IsDeprecated(this TypeDescription type, out string message)
        {
            var obsoleteAttribute = type.Attributes.FirstOrDefault(a => string.Equals(a.Type, "System.ObsoleteAttribute", StringComparison.Ordinal));

            message = obsoleteAttribute?.Arguments.FirstOrDefault()?.Value ?? string.Empty;

            return obsoleteAttribute != null;
        }

        public static bool IsAggregateRoot(this TypeDescription type)
        {
            return type.Type == TypeType.Class 
                && type.ImplementsTypeStartsWith(Constants.AggregateRoot);
        }

        public static string GetAggregateRootId(this TypeDescription type)
        {
            var aggregateRoot = type.BaseTypes.FirstOrDefault(bt => bt.StartsWith(Constants.AggregateRoot, StringComparison.Ordinal));

            return aggregateRoot?.GenericTypes().First();
        }

        public static bool IsEnumeration(this TypeDescription type)
        {
            return type.Type == TypeType.Enum;
        }

        public static bool IsValueObject(this TypeDescription type)
        {
            return type.Type == TypeType.Class && type.Namespace.Split('.').Contains("ValueObjects");
        }

        public static bool IsAggregateId(this TypeDescription type)
        {
            return type.IsValueObject() && type.Name.EndsWith("Id", StringComparison.Ordinal);
        }

        public static bool IsEntity(this TypeDescription type)
        {
            return !type.IsAggregateRoot()
                && !type.IsAggregateId()
                && !type.IsValueObject()
                && type.Type == TypeType.Class;
        }

        public static bool IsEvent(this TypeDescription type)
        {
            return type.BaseTypes.Any(bt => string.Equals(bt, Constants.Event, StringComparison.Ordinal));
        }

        public static bool IsCommand(this TypeDescription type)
        {
            return type.BaseTypes.Any(bt => string.Equals(bt, Constants.Command, StringComparison.Ordinal));
        }

        public static IReadOnlyList<TypeDescription> HandlersFor(this IEnumerable<TypeDescription> types, TypeDescription type)
        {
            var commandHandlers = types.CommandHandlersFor(type);
            var eventHandlers = types.EventHandlersFor(type);

            return commandHandlers.Concat(eventHandlers).ToList();
        }

        public static IReadOnlyList<TypeDescription> CommandHandlersFor(this IEnumerable<TypeDescription> types, TypeDescription type)
        {
            return types
                .Where(t => t.Methods.Any(m => m.Parameters.Any(p => p.Type == type.FullName && p.Attributes.Any(a => a.Type.EndsWith("FromBodyAttribute")))))
                .ToList();
        }

        public static IReadOnlyList<TypeDescription> EventHandlersFor(this IEnumerable<TypeDescription> types, TypeDescription type)
        {
            return types
                .Where(t => t.Type == TypeType.Class && t.BaseTypes.Contains(Constants.MessageHandlerCallback))
                .Where(t => t.Methods.Any(m => m.Name == "HandleAsync" && m.Parameters.Any(p => p.Type.EndsWith("." + type.Name, StringComparison.Ordinal))))
                .ToList();
        }

        public static IReadOnlyList<TypeDescription> GetSourceCommands(this IEnumerable<TypeDescription> types, TypeDescription type)
        {
            var commands =
                from t in types
                from m in t.Methods
                from s in m.Statements
                from i in FlattenStatements(s).OfType<InvocationDescription>()
                where i.ContainingType.EndsWith(type.Name) && i.Name == type.Name
                select types.FirstOrDefault(i.ContainingType);

            return commands.ToList();
        }

        //public static bool IsHandler(this TypeDescription type)
        //{
        //    return type.IsHandlerFor().Count > 0;
        //}

        public static string Service(this TypeDescription type)
        {
            return type.FullName.Split('.').SkipWhile(n => n.Equals("Pitstop", StringComparison.OrdinalIgnoreCase) || n.Equals("Application")).FirstOrDefault();
        }

        public static string AsServiceDisplayName(this string value)
        {
            return value
                .SplitCamelCase()
                .Replace(" Event Handler", "\\nEvent Handler")
                .Replace(" Service", "\\nService")
                .Replace(" API", "\\nAPI")
                .Replace("Controller", "");
        }

        public static string DisplayName(this TypeDescription type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            var name = type.Name;

            return name;
        }

        public static bool HasReceiverInSameNamespace(this TypeDescription type)
        {
            var ns = string.Join('.', type.Namespace.Split('.', 3).Take(2));

            var eventReceivers = Program.Types.Where(t => t.Namespace.StartsWith(ns, StringComparison.Ordinal)
                && t.BaseTypes.Contains(Constants.MessageHandlerCallback, StringComparer.Ordinal)
                && t.Methods.Any(m => string.Equals(m.Name, "HandleAsync", StringComparison.Ordinal) && m.Parameters.First().Type == type.FullName));

            return eventReceivers.Any();
        }

        public static bool HasPublisherInSameNamespace(this TypeDescription type)
        {
            var namespaceParts = type.Namespace.Split('.');

            var ns = string.Join('.', namespaceParts.Take(namespaceParts.Length - 1));

            var invocations = Program.Types
                .Where(t => t.Namespace.StartsWith(ns, StringComparison.Ordinal))
                .SelectMany(t => t.Methods.OfType<IHaveAMethodBody>().Concat(t.Constructors).SelectMany(mb => mb.Statements))
                .SelectMany(s => FlattenStatements(s))
                .OfType<InvocationDescription>()
                //.Where(i => i.ContainingType == Constants.MessagePublisher && i.Name == Constants.PublishMessage);
                .SelectMany(t => Program.Types.GetInvocationConsequences(t));

            return invocations.Any();// i => i.Arguments[1].Type == type.FullName);
        }

        private static IEnumerable<Statement> FlattenStatements(Statement sourceStatement, List<Statement> statements = null)
        {
            if (statements == null)
            {
                statements = new List<Statement>();
            }

            switch (sourceStatement)
            {
                case InvocationDescription invocationDescription:
                    statements.Add(invocationDescription);
                    break;

                case Switch sourceSwitch:
                    foreach (var statement in sourceSwitch.Sections.SelectMany(s => s.Statements))
                    {
                        FlattenStatements(statement, statements);
                    }
                    break;

                case If sourceIf:
                    foreach (var statement in sourceIf.Sections.SelectMany(s => s.Statements))
                    {
                        FlattenStatements(statement, statements);
                    }
                    break;

                case Statement statementBlock:
                    foreach (var statement in statementBlock.Statements)
                    {
                        FlattenStatements(statement, statements);
                    }
                    break;
            }

            return statements;
        }

        public static string SplitCamelCase(this string value)
        {
            return Regex.Replace(
                Regex.Replace(
                    value,
                    @"(\P{Ll})(\P{Ll}\p{Ll})",
                    "$1 $2"
                ),
                @"(\p{Ll})(\P{Ll})",
                "$1 $2"
            );
        }

        public static MethodDescription HandlingMethod(this TypeDescription type, TypeDescription message)
        {
            if (message.IsEvent())
            {
                return type.Methods.FirstOrDefault(m => string.Equals(m.Name, "HandleAsync") && m.Parameters.Any(p => p.Type.EndsWith("." + message.Name, StringComparison.Ordinal)));
            }

            if (message.IsCommand())
            {
                return type.Methods.FirstOrDefault(m =>
                //m.Attributes.Any(a => a.Type.EndsWith("HttpPostAttribute")) && 
                m.Parameters.Any(p => p.Type == message.FullName && p.Attributes.Any(a => a.Type.EndsWith("FromBodyAttribute"))) ||
                m.Name == "HandleCommandAsync" && m.Parameters.Any(p => p.Type.EndsWith("." + message.Name, StringComparison.Ordinal)));
            }

            return null;
        }

        public static bool IsMessageCreation(this InvocationDescription invocation)
        {
            return invocation.Name == "PublishMessageAsync" || invocation.Name == "RaiseEvent";
        }

        public static bool IsMailMessage(this InvocationDescription invocation)
        {
            return invocation.Name == "SendEmailAsync";
        }

        public static bool IsDatabaseAction(this InvocationDescription invocation)
        {
            return invocation.Name == "AddAsync"
                || invocation.Name == "FirstOrDefaultAsync"
                || invocation.Name == "ExecuteAsync"
                || invocation.Name == "QueryAsync"
                || invocation.Name == "QueryFirstOrDefaultAsync";
        }

        public static IReadOnlyList<Statement> GetInvocationConsequenceStatements2(this IEnumerable<TypeDescription> types, InvocationDescription invocation)
        {
            InvocationDescription implementatedInvocation = null;

            if (Program.Types.FirstOrDefault(invocation.ContainingType)?.Type == TypeType.Interface)
            {
                var implementation = Program.Types.FirstOrDefault(t => t.BaseTypes.Contains(invocation.ContainingType));
                implementatedInvocation = new InvocationDescription(implementation.FullName, invocation.Name);
                implementatedInvocation.Arguments.AddRange(invocation.Arguments);
            }

            var statements = types.GetInvokedMethod(implementatedInvocation ?? invocation)
                .SelectMany(m => m.Statements);

            var consequences = new List<Statement>();

            foreach (var statement in statements)
            {
                var innerStatements = TraverseStatement(types, statement);
                if (innerStatements.Count > 0)
                {
                    consequences.AddRange(innerStatements);
                }
                else
                {
                    consequences.Add(statement);
                }
            }

            return consequences;
        }

        private static IReadOnlyList<Statement> TraverseStatement(this IEnumerable<TypeDescription> types, Statement sourceStatement)
        {
            switch (sourceStatement)
            {
                case ForEach forEachStatement:
                    {
                        var destinationForEach = new ForEach();
                        foreach (var statement in forEachStatement.Statements)
                        {
                            destinationForEach.Statements.AddRange(types.TraverseStatement(statement));
                        }

                        destinationForEach.Expression = forEachStatement.Expression;

                        return new List<Statement> { destinationForEach };
                    }

                case Switch sourceSwitch:
                    var destinationSwitch = new Switch();
                    foreach (var switchSection in sourceSwitch.Sections)
                    {
                        var section = new SwitchSection();
                        section.Labels.AddRange(switchSection.Labels);

                        foreach (var statement in switchSection.Statements)
                        {
                            section.Statements.AddRange(types.TraverseStatement(statement));
                        }

                        destinationSwitch.Sections.Add(section);
                    }

                    destinationSwitch.Expression = sourceSwitch.Expression;

                    return new List<Statement> { destinationSwitch };

                case If sourceIf:
                    var destinationÍf = new If();

                    foreach (var ifElseSection in sourceIf.Sections)
                    {
                        var section = new IfElseSection();

                        foreach (var statement in ifElseSection.Statements)
                        {
                            section.Statements.AddRange(types.TraverseStatement(statement));
                        }

                        section.Condition = ifElseSection.Condition;

                        destinationÍf.Sections.Add(section);
                    }

                    return new List<Statement> { destinationÍf };

                default:
                    return new List<Statement>(0);
            }
        }
    }
}