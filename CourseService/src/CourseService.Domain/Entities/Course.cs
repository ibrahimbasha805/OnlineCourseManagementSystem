namespace CourseService.Domain.Entities;

public class Course
{
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }    
    public int InstructorUserId { get; set; }   
    
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}
