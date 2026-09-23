using System.ComponentModel.DataAnnotations;

namespace eSKHub.Components.Model
{
    public class TransparencyDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Budget, Resolution, Performance, Procurement
        public string? Description { get; set; }
        public byte[]? FileData { get; set; }
        public string? FileMimeType { get; set; }
        public string? FileName { get; set; }
        public int? FiscalYear { get; set; }
        public string? Quarter { get; set; } // Q1, Q2, Q3, Q4
        public DateTime DateUploaded { get; set; } = DateTime.Now;
        public string? UploadedBy { get; set; }
        public bool IsPublished { get; set; } = false;
        public int ViewCount { get; set; } = 0;
        public DateTime? DateModified { get; set; }
    }
}