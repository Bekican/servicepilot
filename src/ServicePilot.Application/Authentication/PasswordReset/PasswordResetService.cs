using ServicePilot.Application.Abstractions.Authentication;
using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Common;
using ServicePilot.Application.Organizations;
using ServicePilot.Application.Organizations.CreateOrganization;
using ServicePilot.Application.Users;
using ServicePilot.Domain.Users;

namespace ServicePilot.Application.Authentication.PasswordReset;

public sealed class PasswordResetService(
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository,
    IPasswordResetTokenRepository tokenRepository,
    IInvitationTokenService tokenService,
    IPasswordHasher passwordHasher,
    IPasswordResetLinkBuilder linkBuilder,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task RequestAsync(
        string organizationSlug,
        string email,
        CancellationToken cancellationToken = default)
    {
        string slug = OrganizationSlug.Normalize(organizationSlug ?? string.Empty);
        string normalizedEmail = AuthenticationRules.NormalizeEmail(email ?? string.Empty);
        var organization = await organizationRepository.GetBySlugAsync(slug, cancellationToken);
        if (organization is null) return;
        User? user = await userRepository.GetByOrganizationAndEmailAsync(
            organization.Id, normalizedEmail, cancellationToken);
        if (user is not { IsActive: true }) return;

        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        foreach (PasswordResetToken existing in await tokenRepository.ListAvailableForUserAsync(
            organization.Id, user.Id, cancellationToken))
        {
            existing.Revoke(nowUtc);
        }

        InvitationToken generated = tokenService.Create();
        tokenRepository.Add(new PasswordResetToken(
            Guid.NewGuid(), organization.Id, user.Id, generated.Hash,
            nowUtc, nowUtc.AddMinutes(30)));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await emailSender.SendAsync(new EmailMessage(
                user.Email,
                "ServicePilot parola sıfırlama",
                "Parolanızı yenilemek için aşağıdaki tek kullanımlık bağlantıyı açın:"
                + $"{Environment.NewLine}{linkBuilder.Build(generated.RawValue)}"
                + $"{Environment.NewLine}{Environment.NewLine}Bağlantı 30 dakika geçerlidir."),
                cancellationToken);
        }
        catch (EmailDeliveryException)
        {
            // The public response deliberately does not reveal account or delivery state.
        }
    }

    public async Task<Result> CompleteAsync(
        string rawToken,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return Result.Failure(PasswordResetErrors.InvalidOrExpired);
        }

        if (string.IsNullOrWhiteSpace(password))
            return Result.Failure(AuthenticationErrors.PasswordIsRequired);
        if (password.Length < AuthenticationRules.MinPasswordLength)
            return Result.Failure(AuthenticationErrors.PasswordTooShort);
        if (password.Length > AuthenticationRules.MaxPasswordLength)
            return Result.Failure(AuthenticationErrors.PasswordTooLong);

        PasswordResetToken? token = await tokenRepository.GetByHashAsync(
            tokenService.Hash(rawToken.Trim()), cancellationToken);
        DateTimeOffset nowUtc = timeProvider.GetUtcNow();
        if (token is null || !token.CanBeUsedAt(nowUtc))
            return Result.Failure(PasswordResetErrors.InvalidOrExpired);

        User? user = await userRepository.GetByIdAsync(
            token.OrganizationId, token.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return Result.Failure(PasswordResetErrors.InvalidOrExpired);

        user.ChangePassword(passwordHasher.Hash(password));
        token.Use(nowUtc);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}