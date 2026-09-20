using System.Globalization;
using System.Text;
using Ustas.RimAI.Communication.Memory;

namespace Ustas.RimAI.Communication.Memory.Colony
{
    /// <summary>
    /// The request that turns a snapshot into a paragraph.
    ///
    /// The instruction lives in settings so a player can rewrite it without a
    /// build - it is prompt text, which this project keeps in configuration
    /// rather than in code. What is built here is only the facts beneath it,
    /// and they are written as plain lines rather than JSON: a model asked for
    /// a sentence answers a sentence more readily when it was not handed a
    /// data structure.
    /// </summary>
    internal static class ColonyTrendPrompt
    {
        internal const string DefaultInstruction =
            "Ти описуєш становище колонії в RimWorld для персонажів, які в ній живуть.\n" +
            "З наведених фактів склади один абзац українською: як зараз справи, куди все рухається, "
            + "що загрожує і що поки невідомо.\n"
            + "Пиши так, як сказав би колоніст, а не звіт: без списків, без чисел, без заголовків. "
            + "Три-чотири речення.";

        internal static string Build(ColonySnapshot snapshot, RimTalkMemoryPatchSettings settings)
        {
            string instruction = settings?.colonyTrendInstruction;
            if (string.IsNullOrWhiteSpace(instruction))
            {
                instruction = DefaultInstruction;
            }

            StringBuilder prompt = new StringBuilder(instruction.Trim());
            prompt.Append("\n\n");
            Line(prompt, "Поселення", snapshot.MapLabel);
            Line(prompt, "Колоністів", snapshot.Colonists.ToString(CultureInfo.InvariantCulture));
            if (snapshot.Downed > 0)
            {
                Line(prompt, "З них лежать", snapshot.Downed.ToString(CultureInfo.InvariantCulture));
            }

            if (snapshot.Prisoners > 0)
            {
                Line(prompt, "Полонених", snapshot.Prisoners.ToString(CultureInfo.InvariantCulture));
            }

            Line(prompt, "Середній настрій",
                (snapshot.AverageMood * 100f).ToString("0", CultureInfo.InvariantCulture) + "%");
            if (snapshot.MoodBelowBreakThreshold > 0)
            {
                Line(prompt, "На межі зриву",
                    snapshot.MoodBelowBreakThreshold.ToString(CultureInfo.InvariantCulture));
            }

            Line(prompt, "Їжі вистачить на днів",
                snapshot.DaysOfFood.ToString(CultureInfo.InvariantCulture));
            Line(prompt, "Медикаментів", snapshot.Medicine.ToString(CultureInfo.InvariantCulture));
            Line(prompt, "Статки", ((int)snapshot.Wealth).ToString(CultureInfo.InvariantCulture));
            if (snapshot.HostilePawns > 0)
            {
                Line(prompt, "Ворогів на карті",
                    snapshot.HostilePawns.ToString(CultureInfo.InvariantCulture));
            }

            if (snapshot.FiresBurning > 0)
            {
                Line(prompt, "Пожеж", snapshot.FiresBurning.ToString(CultureInfo.InvariantCulture));
            }

            Line(prompt, "Пора року", snapshot.Season);
            Line(prompt, "Надворі",
                snapshot.OutdoorTemperature.ToString(CultureInfo.InvariantCulture) + "°C");
            Line(prompt, "Досліджують", snapshot.ResearchInProgress);

            if (snapshot.RecentEvents.Count > 0)
            {
                prompt.Append("Останні події: ");
                prompt.Append(string.Join("; ", snapshot.RecentEvents.ToArray()));
                prompt.Append('\n');
            }

            return prompt.ToString();
        }

        private static void Line(StringBuilder prompt, string label, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                prompt.Append(label).Append(": ").Append(value).Append('\n');
            }
        }
    }
}
