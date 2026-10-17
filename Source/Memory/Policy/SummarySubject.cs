using System.Collections.Generic;

namespace Ustas.RimAI.Communication.Memory.Policy
{
    /// <summary>
    /// Who a memory summary is about, as the prompt names them. A name alone
    /// leaves the model to guess, and it guesses gender from the name and age
    /// from nothing; a Ukrainian summary then gets its verb endings wrong and a
    /// child's day reads like a veteran's. So the name carries both.
    /// </summary>
    public static class SummarySubject
    {
        /// <summary>"Name (gender, age)", leaving out whatever is not known.</summary>
        public static string Compose(string name, string genderLabel, int? ageYears)
        {
            var parts = new List<string>(2);
            if (!string.IsNullOrWhiteSpace(genderLabel))
                parts.Add(genderLabel.Trim());
            if (ageYears is { } age && age >= 0)
                parts.Add(age.ToString(System.Globalization.CultureInfo.InvariantCulture));

            string subject = name ?? string.Empty;
            return parts.Count == 0 ? subject : subject + " (" + string.Join(", ", parts) + ")";
        }
    }
}
