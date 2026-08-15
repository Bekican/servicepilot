using ServicePilot.Application.Abstractions.Persistence;
using ServicePilot.Application.Abstractions.Persistence.Exceptions;
using ServicePilot.Application.Abstractions.Tenancy;
using ServicePilot.Application.Common;
using ServicePilot.Domain.Services;

namespace ServicePilot.Application.Services;

public sealed class ServiceCatalogService(
    ITenantContext tenantContext,
    IServiceCatalogRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<Result<ServiceCatalogResponse>> CreateAsync(
        ServiceCatalogData data,
        CancellationToken cancellationToken = default)
    {
        string name = data.Name?.Trim() ?? string.Empty;
        string normalizedName = name.ToLowerInvariant();

        Error? validationError = Validate(data, name);
        if (validationError is not null)
            return Result<ServiceCatalogResponse>.Failure(validationError);

        if (await repository.NameExistsAsync(
            tenantContext.OrganizationId,
            normalizedName,
            null,
            cancellationToken))
        {
            return Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.NameAlreadyExists);
        }

        ServiceCatalogItem service;

        try
        {
            service = new ServiceCatalogItem(
                Guid.NewGuid(),
                tenantContext.OrganizationId,
                name,
                data.DefaultDurationMinutes,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.InvalidData);
        }

        repository.Add(service);
        Result saveResult =
            await SaveAsync(cancellationToken);

        return saveResult.IsFailure
            ? Result<ServiceCatalogResponse>.Failure(
                saveResult.Error)
            : Result<ServiceCatalogResponse>.Success(
                Map(service));
    }

    public async Task<Result<ServiceCatalogResponse>> GetAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default)
    {
        ServiceCatalogItem? service =
            await repository.GetByIdAsync(
                tenantContext.OrganizationId,
                serviceId,
                cancellationToken);

        return service is null
            ? Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.NotFound)
            : Result<ServiceCatalogResponse>.Success(
                Map(service));
    }

    public async Task<IReadOnlyList<ServiceCatalogResponse>>
        ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ServiceCatalogItem> services =
            await repository.ListAsync(
                tenantContext.OrganizationId,
                includeInactive,
                cancellationToken);

        return services.Select(Map).ToArray();
    }

    public async Task<Result<ServiceCatalogResponse>> UpdateAsync(
        Guid serviceId,
        ServiceCatalogData data,
        CancellationToken cancellationToken = default)
    {
        ServiceCatalogItem? service =
            await repository.GetByIdAsync(
                tenantContext.OrganizationId,
                serviceId,
                cancellationToken);

        if (service is null)
        {
            return Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.NotFound);
        }

        string name = data.Name?.Trim() ?? string.Empty;
        string normalizedName = name.ToLowerInvariant();

        Error? validationError = Validate(data, name);
        if (validationError is not null)
            return Result<ServiceCatalogResponse>.Failure(validationError);

        if (await repository.NameExistsAsync(
            tenantContext.OrganizationId,
            normalizedName,
            serviceId,
            cancellationToken))
        {
            return Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.NameAlreadyExists);
        }

        try
        {
            service.Update(
                name,
                data.DefaultDurationMinutes,
                timeProvider.GetUtcNow());
        }
        catch (ArgumentException)
        {
            return Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.InvalidData);
        }

        Result saveResult =
            await SaveAsync(cancellationToken);

        return saveResult.IsFailure
            ? Result<ServiceCatalogResponse>.Failure(
                saveResult.Error)
            : Result<ServiceCatalogResponse>.Success(
                Map(service));
    }

    public async Task<Result<ServiceCatalogResponse>>
        SetStatusAsync(
            Guid serviceId,
            bool isActive,
            CancellationToken cancellationToken = default)
    {
        ServiceCatalogItem? service =
            await repository.GetByIdAsync(
                tenantContext.OrganizationId,
                serviceId,
                cancellationToken);

        if (service is null)
        {
            return Result<ServiceCatalogResponse>.Failure(
                ServiceCatalogErrors.NotFound);
        }

        service.SetActive(
            isActive,
            timeProvider.GetUtcNow());
        Result saveResult =
            await SaveAsync(cancellationToken);

        return saveResult.IsFailure
            ? Result<ServiceCatalogResponse>.Failure(
                saveResult.Error)
            : Result<ServiceCatalogResponse>.Success(
                Map(service));
    }

    private async Task<Result> SaveAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(
                cancellationToken);
            return Result.Success();
        }
        catch (UniqueConstraintViolationException exception)
            when (
                exception.ConstraintName
                == "ux_services_organization_name")
        {
            return Result.Failure(
                ServiceCatalogErrors.NameAlreadyExists);
        }
    }

    private static Error? Validate(ServiceCatalogData data, string name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || name.Length > ServiceCatalogItem.MaxNameLength)
            return ServiceCatalogErrors.InvalidName;

        if (data.DefaultDurationMinutes is < ServiceCatalogItem.MinDurationMinutes
            or > ServiceCatalogItem.MaxDurationMinutes)
            return ServiceCatalogErrors.InvalidDuration;

        return null;
    }

    private static ServiceCatalogResponse Map(
        ServiceCatalogItem service)
    {
        return new ServiceCatalogResponse(
            service.Id,
            service.Name,
            service.DefaultDurationMinutes,
            service.IsActive,
            service.CreatedAtUtc,
            service.UpdatedAtUtc);
    }
}