using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Organizations;


namespace ServicePilot.Application.Organizations.CreateOrganization;

public sealed class CreateOrganizationHandler
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOrganizationHandler(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CreateOrganizationResponse>> HandleAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        string name = command.Name.Trim();
        string slug = OrganizationSlug.Normalize(command.Slug);

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<CreateOrganizationResponse>.Failure(
                OrganizationErrors.NameIsRequired);
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            return Result<CreateOrganizationResponse>.Failure(
                OrganizationErrors.SlugIsRequired);
        }

        if (!OrganizationSlug.IsValid(slug))
        {
            return Result<CreateOrganizationResponse>.Failure(
                OrganizationErrors.InvalidSlug);
        }

        bool slugExists =
            await _organizationRepository.SlugExistsAsync(
                slug,
                cancellationToken);
        if (slugExists)
        {
            return Result<CreateOrganizationResponse>.Failure(
                OrganizationErrors.SlugAlreadyExists);
        }
        DateTimeOffset createdAtUtc =
            _timeProvider.GetUtcNow();

        Organization organization = new(
            Guid.NewGuid(),
            name,
            slug,
            createdAtUtc);

        _organizationRepository.Add(organization);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException exception)
            when (exception.ConstraintName == "ux_organizations_slug")
        {
            return Result<CreateOrganizationResponse>.Failure(
                OrganizationErrors.SlugAlreadyExists
            );
        }

        CreateOrganizationResponse response = new(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.CreatedAtUtc
        );

        return Result<CreateOrganizationResponse>.Success(response);
    }
}