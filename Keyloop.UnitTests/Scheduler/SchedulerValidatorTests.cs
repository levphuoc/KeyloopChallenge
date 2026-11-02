using Keyloop.Application.Features.Appointments.Commands.CreateAppointment;
using Keyloop.Application.Features.Appointments.Queries.GetAvailability;
using Keyloop.Application.Features.ReferenceData;

namespace Keyloop.UnitTests.Scheduler;

public sealed class SchedulerValidatorTests
{
    [Fact]
    public void CreateAppointmentCommandValidator_RejectsMissingIdentifiersAndStartTime()
    {
        var result = new CreateAppointmentCommandValidator()
            .Validate(new CreateAppointmentCommand(0, 0, 0, 0, default));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAppointmentCommand.CustomerId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAppointmentCommand.VehicleId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAppointmentCommand.DealershipId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAppointmentCommand.ServiceTypeId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateAppointmentCommand.StartTime));
    }

    [Fact]
    public void GetAvailabilityQueryValidator_RejectsMissingIdentifiersAndDate()
    {
        var result = new GetAvailabilityQueryValidator()
            .Validate(new GetAvailabilityQuery(0, 0, default));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetAvailabilityQuery.DealershipId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetAvailabilityQuery.ServiceTypeId));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetAvailabilityQuery.Date));
    }

    [Fact]
    public void ListVehiclesQueryValidator_RejectsNonPositiveCustomerId()
    {
        var result = new ListVehiclesQueryValidator().Validate(new ListVehiclesQuery(0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ListVehiclesQuery.CustomerId));
    }
}
