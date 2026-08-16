using System;

namespace AscentSchools.Core.DTOs.School.Calendar
{
    // One calendar entry — a holiday / exam / celebration / event, school-wide,
    // spanning a date range (StartDate..EndDate; equal for a single day).
    public class CalendarEventDto
    {
        public int      CalendarEventId { get; set; }
        public string   Title           { get; set; }
        public string   Description     { get; set; }
        public string   Category        { get; set; }   // Holiday | Exam | Celebration | Event
        public DateTime StartDate       { get; set; }
        public DateTime EndDate         { get; set; }
        public int?     AcademicYearId  { get; set; }
        public string   Status          { get; set; }
        public string   CreatedBy       { get; set; }
        public DateTime CreatedAt       { get; set; }
    }

    public class SaveCalendarEventRequest
    {
        public string    Title          { get; set; }
        public string    Description    { get; set; }
        public string    Category       { get; set; }
        public DateTime? StartDate       { get; set; }
        public DateTime? EndDate         { get; set; }
        public int?      AcademicYearId  { get; set; }
    }
}
