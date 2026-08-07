using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;

namespace NotificationService.Application.Features.Preferences.GetRecipientPreference;

public sealed class GetRecipientPreferenceHandler : IRequestHandler<GetRecipientPreferenceQuery, Result<RecipientPreferenceDto>>
{
    private readonly INotificationDbContext _dbContext;

    public GetRecipientPreferenceHandler(INotificationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<RecipientPreferenceDto>> Handle(GetRecipientPreferenceQuery request, CancellationToken cancellationToken)
    {
        var preference = await _dbContext.RecipientPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.RecipientId == request.RecipientId, cancellationToken);

        if (preference is null)
            return Result<RecipientPreferenceDto>.Success(
                new RecipientPreferenceDto(Guid.Empty, request.RecipientId, false, false, false, "en", default, null));

        return Result<RecipientPreferenceDto>.Success(new RecipientPreferenceDto(
            preference.Id, preference.RecipientId, preference.EmailOptOut, preference.SmsOptOut,
            preference.PushOptOut, preference.Locale, preference.CreatedAtUtc, preference.UpdatedAtUtc));
    }
}
