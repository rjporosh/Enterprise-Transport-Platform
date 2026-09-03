using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TicketingService.Application.Common.Interfaces;
using TicketingService.Application.Common.Models;
using TicketingService.Domain.Entities;

namespace TicketingService.Application.Features.Templates;

public sealed record TemplateDto(
    Guid Id, Guid OperatorId, string Name, string BrandName, string PrimaryColorHex, string AccentColorHex,
    string TermsText, string FooterText, bool IsDefault, bool IsActive, bool HasLogo);

internal static class TemplateMap
{
    public static TemplateDto To(TicketTemplate t) => new(
        t.Id, t.OperatorId, t.Name, t.BrandName, t.PrimaryColorHex, t.AccentColorHex,
        t.TermsText, t.FooterText, t.IsDefault, t.IsActive, !string.IsNullOrEmpty(t.LogoPngBase64));
}

// ---- Create ----------------------------------------------------------
public sealed record CreateTemplateCommand(Guid OperatorId, string Name, string BrandName, bool IsDefault) : IRequest<TemplateDto>;

public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.OperatorId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BrandName).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateTemplateHandler(ITicketingDbContext db, IDateTimeProvider clock) : IRequestHandler<CreateTemplateCommand, TemplateDto>
{
    public async Task<TemplateDto> Handle(CreateTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.IsDefault)
        {
            var current = await db.TicketTemplates.Where(t => t.OperatorId == request.OperatorId && t.IsDefault).ToListAsync(cancellationToken);
            foreach (var c in current) c.Update(c.Name, c.BrandName, c.PrimaryColorHex, c.AccentColorHex, c.TermsText, c.FooterText, c.IsActive, clock.UtcNow);
        }

        var template = TicketTemplate.Create(request.OperatorId, request.Name, request.BrandName, request.IsDefault, clock.UtcNow);
        db.TicketTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);
        return TemplateMap.To(template);
    }
}

// ---- Update --------------------------------------------------------
public sealed record UpdateTemplateCommand(
    Guid Id, string Name, string BrandName, string PrimaryColorHex, string AccentColorHex,
    string TermsText, string FooterText, bool IsActive) : IRequest<TemplateDto>;

public sealed class UpdateTemplateHandler(ITicketingDbContext db, IDateTimeProvider clock) : IRequestHandler<UpdateTemplateCommand, TemplateDto>
{
    public async Task<TemplateDto> Handle(UpdateTemplateCommand request, CancellationToken cancellationToken)
    {
        var t = await db.TicketTemplates.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Template {request.Id} not found.");
        t.Update(request.Name, request.BrandName, request.PrimaryColorHex, request.AccentColorHex, request.TermsText, request.FooterText, request.IsActive, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return TemplateMap.To(t);
    }
}

// ---- Set logo -----------------------------------------------------
public sealed record SetTemplateLogoCommand(Guid Id, byte[] PngBytes) : IRequest<TemplateDto>;

public sealed class SetTemplateLogoValidator : AbstractValidator<SetTemplateLogoCommand>
{
    private static readonly byte[] PngMagic = { 0x89, 0x50, 0x4E, 0x47 };

    public SetTemplateLogoValidator()
    {
        RuleFor(x => x.PngBytes).NotEmpty()
            .Must(b => b.Length <= 512 * 1024).WithMessage("Logo must be 512 KB or smaller.")
            .Must(b => b.Length >= 4 && b[0] == PngMagic[0] && b[1] == PngMagic[1] && b[2] == PngMagic[2] && b[3] == PngMagic[3])
            .WithMessage("Logo must be a PNG file.");
    }
}

public sealed class SetTemplateLogoHandler(ITicketingDbContext db, IDateTimeProvider clock) : IRequestHandler<SetTemplateLogoCommand, TemplateDto>
{
    public async Task<TemplateDto> Handle(SetTemplateLogoCommand request, CancellationToken cancellationToken)
    {
        var t = await db.TicketTemplates.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Template {request.Id} not found.");
        t.SetLogo(Convert.ToBase64String(request.PngBytes), clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return TemplateMap.To(t);
    }
}

// ---- List ---------------------------------------------------------
public sealed record GetTemplatesQuery(Guid? OperatorId, int Page = 1, int PageSize = 50) : IRequest<PagedResult<TemplateDto>>;

public sealed class GetTemplatesHandler(ITicketingDbContext db) : IRequestHandler<GetTemplatesQuery, PagedResult<TemplateDto>>
{
    public async Task<PagedResult<TemplateDto>> Handle(GetTemplatesQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var size = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;

        var q = db.TicketTemplates.AsNoTracking();
        if (request.OperatorId is { } op) q = q.Where(t => t.OperatorId == op);
        q = q.OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name);

        var total = await q.CountAsync(cancellationToken);
        var rows = await q.Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PagedResult<TemplateDto>(rows.Select(TemplateMap.To).ToList(), total, page, size);
    }
}
