using System.Collections.Generic;
using tupadportal.Models;

namespace tupadportal.ViewModels
{
    public class ApplicantAttendanceViewModel
    {
        public int ApplicantId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Barangay { get; set; }
        public List<Attendance> Attendances { get; set; } = new List<Attendance>();
    }


    public class AttendanceViewModel
    {
        public string Date { get; set; } // String for formatted date
        public string TimeInAM { get; set; }
        public string TimeOutAM { get; set; }
        // Add other properties as needed
    }
}
