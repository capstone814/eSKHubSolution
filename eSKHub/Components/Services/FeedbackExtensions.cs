// eSKHub/Components/Services/FeedbackExtensions.cs
using System.Text.Json;
using eSKHub.Components.Model;

namespace eSKHub.Components.Services
{
    public static class FeedbackExtensions
    {
        public static bool IsReadByUser(this Feedback feedback, string username)
        {
            if (string.IsNullOrEmpty(feedback.ReadByUsers))
                return false;

            try
            {
                var readByUsers = JsonSerializer.Deserialize<Dictionary<string, bool>>(feedback.ReadByUsers);
                return readByUsers?.ContainsKey(username) == true && readByUsers[username];
            }
            catch
            {
                return false;
            }
        }

        public static void MarkAsReadByUser(this Feedback feedback, string username)
        {
            Dictionary<string, bool> readByUsers;

            if (string.IsNullOrEmpty(feedback.ReadByUsers))
            {
                readByUsers = new Dictionary<string, bool>();
            }
            else
            {
                try
                {
                    readByUsers = JsonSerializer.Deserialize<Dictionary<string, bool>>(feedback.ReadByUsers)
                        ?? new Dictionary<string, bool>();
                }
                catch
                {
                    readByUsers = new Dictionary<string, bool>();
                }
            }

            readByUsers[username] = true;
            feedback.ReadByUsers = JsonSerializer.Serialize(readByUsers);
        }

        public static void MarkAsUnreadByUser(this Feedback feedback, string username)
        {
            if (string.IsNullOrEmpty(feedback.ReadByUsers))
                return;

            try
            {
                var readByUsers = JsonSerializer.Deserialize<Dictionary<string, bool>>(feedback.ReadByUsers);
                if (readByUsers?.ContainsKey(username) == true)
                {
                    readByUsers[username] = false;
                    feedback.ReadByUsers = JsonSerializer.Serialize(readByUsers);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        public static bool IsSolvedForBarangay(this Feedback feedback, string barangay)
        {
            if (string.IsNullOrEmpty(feedback.SolvedByBarangays))
                return false;

            try
            {
                var solvedByBarangays = JsonSerializer.Deserialize<Dictionary<string, bool>>(feedback.SolvedByBarangays);
                return solvedByBarangays?.ContainsKey(barangay) == true && solvedByBarangays[barangay];
            }
            catch
            {
                return false;
            }
        }

        public static void MarkAsSolvedForBarangay(this Feedback feedback, string barangay)
        {
            Dictionary<string, bool> solvedByBarangays;

            if (string.IsNullOrEmpty(feedback.SolvedByBarangays))
            {
                solvedByBarangays = new Dictionary<string, bool>();
            }
            else
            {
                try
                {
                    solvedByBarangays = JsonSerializer.Deserialize<Dictionary<string, bool>>(feedback.SolvedByBarangays)
                        ?? new Dictionary<string, bool>();
                }
                catch
                {
                    solvedByBarangays = new Dictionary<string, bool>();
                }
            }

            solvedByBarangays[barangay] = true;
            feedback.SolvedByBarangays = JsonSerializer.Serialize(solvedByBarangays);
        }

        public static List<string> GetForwardedBarangays(this Feedback feedback)
        {
            if (string.IsNullOrEmpty(feedback.ForwardedToBarangays))
                return new List<string>();

            return feedback.ForwardedToBarangays
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(b => b.Trim())
                .ToList();
        }

        public static void AddForwardedBarangays(this Feedback feedback, List<string> barangays)
        {
            var existing = feedback.GetForwardedBarangays();
            existing.AddRange(barangays.Where(b => !existing.Contains(b)));
            feedback.ForwardedToBarangays = string.Join(",", existing);
            feedback.IsForwarded = existing.Any();
        }
        public static void ResetSolvedStatusForBarangay(this Feedback feedback, string barangay)
        {
            if (string.IsNullOrEmpty(feedback.SolvedByBarangays))
                return;

            try
            {
                var solvedByBarangays = JsonSerializer.Deserialize<Dictionary<string, bool>>(feedback.SolvedByBarangays);
                if (solvedByBarangays?.ContainsKey(barangay) == true)
                {
                    // Remove the solved status for this barangay
                    solvedByBarangays.Remove(barangay);
                    feedback.SolvedByBarangays = JsonSerializer.Serialize(solvedByBarangays);
                }
            }
            catch
            {
                // If there's an error parsing, just ignore
            }
        }
    }
}