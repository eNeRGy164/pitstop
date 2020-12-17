using LivingDocumentation;
using System;
using System.Text;

namespace Pitstop.LivingDocumentation
{
    internal static class AsciiDocHelper
    {
        internal static void BeginSection(StringBuilder stringBuilder, string section)
        {
            stringBuilder.AppendLine($"== {section}");
            stringBuilder.AppendLine(":leveloffset: +1");
            stringBuilder.AppendLine();

            BeginTag(stringBuilder, section);
        }

        internal static void EndSection(StringBuilder stringBuilder, string section)
        {
            EndTag(stringBuilder, section);

            stringBuilder.AppendLine();
            stringBuilder.AppendLine(":leveloffset: -1");
        }

        internal static void BeginTag(StringBuilder stringBuilder, string tag)
        {
            stringBuilder.AppendLine($"// tag::{tag}[]");
        }

        internal static void EndTag(StringBuilder stringBuilder, string tag)
        {
            stringBuilder.AppendLine($"// end::{tag}[]");
        }

        internal static string FormatChapter(string value)
        {
            value = value.ToSentenceCase();

            return value;
        }

        internal static string ArrowColor(TypeDescription type)
        {
            if (type == null)
            {
                return string.Empty;
            }

            return $"[#{(type.IsCommand() ? "DodgerBlue" : "ForestGreen")}]";
        }

        internal static void Legend(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("legend bottom right");
            stringBuilder.AppendLine("|= |= Message |");
            stringBuilder.AppendLine("|<#DodgerBlue>   | Command |");
            stringBuilder.AppendLine("|<#ForestGreen>   | Event |");
            stringBuilder.AppendLine("endlegend");
        }

        internal static void DefaultSequenceDiagramStyling(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("skinparam SequenceMessageAlign reverseDirection");
            stringBuilder.AppendLine("skinparam SequenceGroupBodyBackgroundColor Transparent");
            stringBuilder.AppendLine("skinparam SequenceBoxBackgroundColor #Gainsboro");
            stringBuilder.AppendLine("skinparam SequenceArrowThickness 2");
            stringBuilder.AppendLine("skinparam BoxPadding 10");
            stringBuilder.AppendLine("skinparam ParticipantPadding 10");
            stringBuilder.AppendLine("skinparam LifeLineStrategy solid");
            stringBuilder.AppendLine("skinparam WrapMessageWidth 250");
            stringBuilder.AppendLine("skinparam WrapWidth 250");
            stringBuilder.AppendLine("skinparam NoteBackgroundColor Khaki");
            stringBuilder.AppendLine("skinparam NoteBorderColor Black");
            stringBuilder.AppendLine("skinparam Shadowing<<noshadow>> False");
            //stringBuilder.AppendLine("hide footbox");
        }
    }
}