using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ustas.RimAI.Communication.Memory.Colony
{
    /// <summary>
    /// What is measurably true about a colony right now.
    ///
    /// Everything a pawn knows in this module is about a pawn: what they
    /// remember, who they are, how they feel about somebody. Nothing has been
    /// about the place they all live in, so a colonist could discuss a quarrel
    /// from last week while a raid burned the kitchen and never mention it.
    ///
    /// This is the measured half of the answer and it holds numbers only - no
    /// prose, no judgement. A model turns it into a paragraph
    /// (<see cref="ColonyTrendManager"/>); keeping the two apart is what lets
    /// the paragraph be re-asked for without re-deciding what a colony is.
    /// </summary>
    internal sealed class ColonySnapshot
    {
        internal string MapLabel { get; set; } = "";

        internal int Colonists { get; set; }

        internal int Downed { get; set; }

        internal int Prisoners { get; set; }

        internal float AverageMood { get; set; }

        internal int MoodBelowBreakThreshold { get; set; }

        internal int DaysOfFood { get; set; }

        internal int Medicine { get; set; }

        internal float Wealth { get; set; }

        internal int HostilePawns { get; set; }

        internal int FiresBurning { get; set; }

        internal int UnroofedBeds { get; set; }

        internal string Season { get; set; } = "";

        internal int OutdoorTemperature { get; set; }

        internal string ResearchInProgress { get; set; } = "";

        internal List<string> RecentEvents { get; } = new List<string>();

        /// <summary>
        /// What the colony looks like to the nearest step, so a summary is
        /// re-asked for when something moved and not when a colonist walked.
        ///
        /// Mood is rounded to a tenth and wealth to a thousand deliberately:
        /// an exact figure changes every tick and would make the fingerprint a
        /// clock, which is the opposite of what it is for.
        /// </summary>
        internal string Fingerprint()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(MapLabel).Append('|')
                .Append(Colonists).Append('|')
                .Append(Downed).Append('|')
                .Append(Prisoners).Append('|')
                .Append(Math.Round(AverageMood, 1).ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(MoodBelowBreakThreshold).Append('|')
                .Append(DaysOfFood).Append('|')
                .Append(Medicine / 10).Append('|')
                .Append((int)(Wealth / 1000f)).Append('|')
                .Append(HostilePawns).Append('|')
                .Append(FiresBurning).Append('|')
                .Append(UnroofedBeds).Append('|')
                .Append(Season).Append('|')
                .Append(OutdoorTemperature / 5).Append('|')
                .Append(ResearchInProgress);
            foreach (string entry in RecentEvents)
            {
                builder.Append('|').Append(entry);
            }

            return builder.ToString();
        }
    }
}
