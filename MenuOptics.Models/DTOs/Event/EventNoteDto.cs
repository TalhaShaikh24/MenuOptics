using System;

namespace MenuOptics.Models
{
    public class EventNoteDto
    {
        public int EventNoteId { get; set; }
        public int EventId { get; set; }
        public string? NoteText { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
