using MediatR;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Common.Interfaces;
using NotificationService.Application.Common.Models;
using NotificationService.Application.Features.Preferences.GetRecipientPreference;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Features.Preferences.UpdateRecipientPreference;

public sealed class UpdateRecipientPreferenceHandler : IRequestHandler<UpdateRecipientPreferenceCommand, Result<RecipientPreferenceDto>>
{
    private readonly INotificationDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateRecipientPreferenceHandler(INotificationDbContext dbContext, IDateTimeProvider dateTimeProvider)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<RecipientPreferenceDto>> Handle(UpdateRecipientPreferenceCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var preference = await _dbContext.RecipientPreferences
            .FirstOrDefaultAsync(p => p.RecipientId == request.RecipientId, cancellationToken);

        if (preference is null)
        {
            preference = RecipientPreference.CreateDefault(request.RecipientId, request.Locale, nowUtc);
            _dbContext.RecipientPreferences.Add(preference);
        }

        preference.UpdatePreferences(request.EmailOptOut, request.SmsOptOut, request.PushOptOut, request.Locale, nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result<RecipientPreferenceDto>.Success(new RecipientPreferenceDto(
            preference.Id, preference.RecipientId, preference.EmailOptOut, preference.SmsOptOut,
            preference.PushOptOut, preference.Locale, preference.CreatedAtUtc, preference.UpdatedAtUtc));
    }
}
