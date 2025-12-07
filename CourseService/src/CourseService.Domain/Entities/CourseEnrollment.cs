using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CourseService.Domain.Entities;

public class CourseEnrollment
{
    public int Id { get; set; }          
    public int CourseId { get; set; }    
    public int UserId { get; set; }      
    public DateTime EnrollDate { get; set; }

    public Course Course { get; set; } = null!;
}
