using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit2;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CourseService.Common;

public class AutoMoqDataAttribute : AutoDataAttribute
{
    public AutoMoqDataAttribute()
        : base(() =>
        {
            var fixture = new Fixture();

            // Enable Auto-Mocking using Moq
            fixture.Customize(new AutoMoqCustomization
            {
                ConfigureMembers = true
            });

            fixture.Customize<BindingInfo>(c => c.OmitAutoProperties());
            
            // (Optional) Don't over-populate MVC infrastructure types
           // fixture.Customize<ControllerContext>(c => c.OmitAutoProperties());
           // fixture.Customize<ControllerActionDescriptor>(c => c.OmitAutoProperties());

            return fixture;

            
        })
    {
    }
}

