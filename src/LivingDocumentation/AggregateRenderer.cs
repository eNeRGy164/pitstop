using LivingDocumentation;
using PlantUml.Builder;
using PlantUml.Builder.ClassDiagrams;
using System.Linq;
using System.Text;

namespace Pitstop.LivingDocumentation
{
    public class AggregateRenderer
    {
        public StringBuilder Render()
        {
            var aggregates = Program.Types.Where(t => t.IsAggregateRoot()).ToList();

            var stringBuilder = new StringBuilder();

            foreach (var aggregate in aggregates)
            {
                AsciiDocHelper.BeginSection(stringBuilder, $"aggregate-{aggregate.Name.ToLowerInvariant()}");
                stringBuilder.AppendLine($".Aggregate - {aggregate.Name.ToSentenceCase()}");
                stringBuilder.AppendLine($"[plantuml]");
                stringBuilder.AppendLine("....");

                stringBuilder.UmlDiagramStart();
                stringBuilder.SkinParameter(SkinParameter.MinClassWidth, "160");
                stringBuilder.AppendLine("scale max 4096 height");
                stringBuilder.NamespaceStart(aggregate.Name, stereotype: "aggregate");

                var idType = Program.Types.FirstOrDefault(aggregate.GetAggregateRootId());
                var idBuilder = RenderClass(idType);
                stringBuilder.Append(idBuilder);
                stringBuilder.AppendLine($"{idType.Name} -- {aggregate.Name}");

                var rootBuilder = RenderClass(aggregate);
                stringBuilder.Append(rootBuilder);

                stringBuilder.NamespaceEnd();
                stringBuilder.UmlDiagramEnd();

                stringBuilder.AppendLine("....");
                AsciiDocHelper.EndSection(stringBuilder, $"aggregate-{aggregate.Name.ToLowerInvariant()}");
            }

            return stringBuilder;
        }

        private StringBuilder RenderClass(TypeDescription type)
        {
            var stringBuilder = new StringBuilder();

            if (type.IsAbstract()) stringBuilder.Append("abstract ");
            if (type.IsEnumeration())
            {
                stringBuilder.Append("enum");
            }
            else
            {
                stringBuilder.Append("class");
            }
            stringBuilder.Append($" {type.Name} ");
            type.RenderStereoType(stringBuilder);
            stringBuilder.AppendLine("{");

            if (type.IsEnumeration())
            {
                foreach (var member in type.EnumMembers)
                {
                    stringBuilder.AppendLine(member.Name);
                }
            }
            else
            {
                foreach (var property in type.Properties.Where(p => !p.IsPrivate()))
                {
                    property.RenderProperty(stringBuilder);
                }

                foreach (var method in type.Methods.Where(m => !m.IsPrivate() && !m.IsOverride()))
                {
                    method.RenderMethod(stringBuilder);
                }
            }

            stringBuilder.AppendLine("}");

            foreach (var propertyDescription in type.Properties)
            {
                var property = Program.Types.FirstOrDefault(t => string.Equals(t.FullName, propertyDescription.Type) || (propertyDescription.Type.IsEnumerable() && string.Equals(t.FullName, propertyDescription.Type.GenericTypes().First())));
                if (property != null)
                {
                    var classBuilder = RenderClass(property);
                    stringBuilder.Append(classBuilder);

                    // Relation
                    stringBuilder.Append($"{type.Name} -- {property.Name}");
                    if (propertyDescription.Type.IsEnumerable()) stringBuilder.Append(" : 1..*");
                    stringBuilder.AppendLine();
                }
            }

            return stringBuilder;
        }
    }
}