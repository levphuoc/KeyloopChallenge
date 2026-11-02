using Keyloop.Application.Abstractions.Scheduler;
using Keyloop.Application.DTOs.Scheduler;
using Keyloop.Application.Messaging;
using MediatR;

namespace Keyloop.Application.Features.ReferenceData;
public sealed record ListDealershipsQuery : IQuery<IReadOnlyList<DealershipDto>>;
public sealed record ListServiceTypesQuery : IQuery<IReadOnlyList<ServiceTypeDto>>;
public sealed class ListDealershipsQueryHandler(IAppointmentScheduler scheduler) : IRequestHandler<ListDealershipsQuery, IReadOnlyList<DealershipDto>>
{ public Task<IReadOnlyList<DealershipDto>> Handle(ListDealershipsQuery request, CancellationToken cancellationToken) => scheduler.ListDealershipsAsync(cancellationToken); }
public sealed class ListServiceTypesQueryHandler(IAppointmentScheduler scheduler) : IRequestHandler<ListServiceTypesQuery, IReadOnlyList<ServiceTypeDto>>
{ public Task<IReadOnlyList<ServiceTypeDto>> Handle(ListServiceTypesQuery request, CancellationToken cancellationToken) => scheduler.ListServiceTypesAsync(cancellationToken); }
