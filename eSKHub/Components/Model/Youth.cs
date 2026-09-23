using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class Youth
    {
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = default!;

        public string? MiddleName { get; set; }

        [Required]
        public string LastName { get; set; } = default!;

        public string FullName => $"{FirstName} {MiddleName} {LastName}".Replace("  ", " ");

        [Required]
        public DateTime DateOfBirth { get; set; }

        public int Age => DateTime.Today.Year - DateOfBirth.Year -
            (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);

        [Required]
        public string Gender { get; set; } = default!; // Male, Female, Other

        [Required]
        public string CivilStatus { get; set; } = default!; // Single, Married, Widowed, Separated

        [Required]
        [EmailAddress]
        public string Email { get; set; } = default!;

        [Required]
        public string ContactNumber { get; set; } = default!;

        [Required]
        public string Barangay { get; set; } = default!;

        [Required]
        public string Address { get; set; } = default!;

        public string? EducationalAttainment { get; set; } // Elementary, High School, College, Vocational, Graduate

        public string? CurrentSchool { get; set; }

        public string? Occupation { get; set; }

        public bool IsVoter { get; set; }

        public bool IsSKVoter { get; set; }

        public string? Skills { get; set; } // Comma-separated skills

        public string? Interests { get; set; } // Comma-separated interests

        public bool WillingToVolunteer { get; set; }

        public byte[]? ImageData { get; set; }
        public string? ImageMimeType { get; set; }

        public DateTime DateRegistered { get; set; } = DateTime.Now;

        public DateTime? DateModified { get; set; }

        public string Status { get; set; } = "Active"; // Active, Inactive
    }
}