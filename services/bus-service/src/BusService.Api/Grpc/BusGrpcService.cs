using System.Text.Json;
using BusService.Application.Common.Interfaces;
using BusService.Application.Features.Buses.GetBus;
using BusService.Application.Features.Buses.GetBuses;
using BusService.Domain.Enums;
using BusService.Domain.Exceptions;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusService.Api.Grpc;

public sealed class BusGrpcService : BusService.BusServiceBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public BusGrpcService(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [Authorize]
    public override async Task<GetBusResponse> GetBus(GetBusRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.BusId, out var busId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid bus_id format."));

        try
        {
            var result = await _sender.Send(new GetBusQuery(busId), context.CancellationToken);
            return new GetBusResponse
            {
                BusId = result.Id.ToString(),
                OperatorId = result.OperatorId.ToString(),
                PlateNumber = result.PlateNumber,
                BusType = result.BusType,
                TotalSeats = result.TotalSeats,
                DepotId = result.DepotId.ToString(),
                Status = result.Status,
                Manufacturer = result.Manufacturer ?? string.Empty,
                Model = result.Model ?? string.Empty,
                YearOfManufacture = result.YearOfManufacture ?? 0,
                IsDeleted = result.IsDeleted,
                CreatedAtUtc = result.CreatedAtUtc.ToString("o"),
                UpdatedAtUtc = result.UpdatedAtUtc.ToString("o")
            };
        }
        catch (BusNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Bus not found."));
        }
    }

    [Authorize]
    public override async Task<ListBusesResponse> ListBuses(ListBusesRequest request, ServerCallContext context)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 50;

        var query = new GetBusesQuery(
            string.IsNullOrWhiteSpace(request.OperatorId) ? null : Guid.Parse(request.OperatorId),
            string.IsNullOrWhiteSpace(request.DepotId) ? null : Guid.Parse(request.DepotId),
            _currentUser.TenantId,
            _currentUser.CompanyId,
            _currentUser.OrganizationId,
            string.IsNullOrWhiteSpace(request.Status) ? null : request.Status,
            page,
            pageSize);

        var result = await _sender.Send(query, context.CancellationToken);

        var response = new ListBusesResponse
        {
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        response.Buses.AddRange(result.Items.Select(b => new GetBusResponse
        {
            BusId = b.Id.ToString(),
            OperatorId = b.OperatorId.ToString(),
            PlateNumber = b.PlateNumber,
            BusType = b.BusType,
            TotalSeats = b.TotalSeats,
            DepotId = b.DepotId.ToString(),
            Status = b.Status,
            Manufacturer = b.Manufacturer ?? string.Empty,
            Model = b.Model ?? string.Empty,
            YearOfManufacture = b.YearOfManufacture ?? 0,
            IsDeleted = b.IsDeleted,
            CreatedAtUtc = b.CreatedAtUtc.ToString("o"),
            UpdatedAtUtc = b.UpdatedAtUtc.ToString("o")
        }));

        return response;
    }
}
