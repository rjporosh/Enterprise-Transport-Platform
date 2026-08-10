using Grpc.Core;
using MediatR;
using RouteService.Application.Features.Routes.GetRoute;
using RouteService.Application.Features.Routes.SearchRoutes;
using RouteService.Application.Common.Models;

namespace RouteService.Api.Grpc;

public sealed class RouteGrpcServiceImpl : RouteGrpcService.RouteGrpcServiceBase
{
    private readonly IMediator _mediator;

    public RouteGrpcServiceImpl(IMediator mediator) => _mediator = mediator;

    public override async Task<GetRouteReply> GetRoute(GetRouteRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.RouteId, out var routeId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "route_id must be a GUID."));

        var result = await _mediator.Send(new GetRouteQuery(routeId), context.CancellationToken);
        return new GetRouteReply
        {
            RouteId = result.Id.ToString(),
            Code = result.Code,
            Name = result.Name,
            Status = result.Status,
            TransportMode = result.TransportMode,
            DistanceKm = result.DistanceKm
        };
    }

    public override async Task<SearchRoutesReply> SearchRoutes(SearchRoutesRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Term))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "term is required."));

        var result = await _mediator.Send(new SearchRoutesQuery(request.Term, request.Page > 0 ? request.Page : 1, request.PageSize > 0 ? request.PageSize : 50), context.CancellationToken);

        var reply = new SearchRoutesReply
        {
            TotalCount = result.TotalCount
        };
        reply.Routes.AddRange(result.Items.Select(r => new RouteItem
        {
            RouteId = r.Id.ToString(),
            Code = r.Code,
            Name = r.Name,
            Status = r.Status,
            TransportMode = r.TransportMode
        }));

        return reply;
    }
}
