using FluentValidation;
using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.ReferenceData;

public sealed record ListDealershipsQuery : IQuery<IReadOnlyList<DealershipDto>>;
public sealed record ListServiceTypesQuery : IQuery<IReadOnlyList<ServiceTypeDto>>;
public sealed record ListCustomersQuery : IQuery<IReadOnlyList<CustomerDto>>;
public sealed record ListVehiclesQuery(int CustomerId) : IQuery<IReadOnlyList<VehicleDto>>;

public sealed class ListDealershipsQueryHandler(IAppointmentScheduler scheduler)
    : IRequestHandler<ListDealershipsQuery, IReadOnlyList<DealershipDto>>
{
    public Task<IReadOnlyList<DealershipDto>> Handle(
        ListDealershipsQuery request, CancellationToken cancellationToken) =>
        scheduler.ListDealershipsAsync(cancellationToken);
}

public sealed class ListServiceTypesQueryHandler(IAppointmentScheduler scheduler)
    : IRequestHandler<ListServiceTypesQuery, IReadOnlyList<ServiceTypeDto>>
{
    public Task<IReadOnlyList<ServiceTypeDto>> Handle(
        ListServiceTypesQuery request, CancellationToken cancellationToken) =>
        scheduler.ListServiceTypesAsync(cancellationToken);
}

public sealed class ListCustomersQueryHandler(IAppointmentScheduler scheduler)
    : IRequestHandler<ListCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public Task<IReadOnlyList<CustomerDto>> Handle(
        ListCustomersQuery request, CancellationToken cancellationToken) =>
        scheduler.ListCustomersAsync(cancellationToken);
}

public sealed class ListVehiclesQueryValidator : AbstractValidator<ListVehiclesQuery>
{
    public ListVehiclesQueryValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
    }
}

public sealed class ListVehiclesQueryHandler(IAppointmentScheduler scheduler)
    : IRequestHandler<ListVehiclesQuery, IReadOnlyList<VehicleDto>>
{
    public Task<IReadOnlyList<VehicleDto>> Handle(
        ListVehiclesQuery request, CancellationToken cancellationToken) =>
        scheduler.ListVehiclesAsync(request.CustomerId, cancellationToken);
}
