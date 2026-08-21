using ServicePilot.Domain.Knowledge;
using ServicePilot.Domain.Users;

namespace ServicePilot.UnitTests.Knowledge;

public sealed class KnowledgeDocumentTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 8, 19, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CustomerReport_ShouldNotAllowSharedAccess()
    {
        Assert.Throws<ArgumentException>(() => Create(
            KnowledgeDocumentType.CustomerServiceReport,
            KnowledgeDocumentAccessScope.Shared));
    }

    [Theory]
    [InlineData(UserRoles.Owner, true)]
    [InlineData(UserRoles.Admin, true)]
    [InlineData(UserRoles.Dispatcher, true)]
    [InlineData(UserRoles.Technician, false)]
    public void OperationsScope_ShouldApplyRoleBoundary(
        string role,
        bool expected)
    {
        KnowledgeDocument document = Create(
            KnowledgeDocumentType.Warranty,
            KnowledgeDocumentAccessScope.Operations);

        Assert.Equal(expected, document.CanBeAccessedByRole(role));
    }

    [Fact]
    public void ProcessingLifecycle_ShouldPreserveVisibleFailure()
    {
        KnowledgeDocument document = Create(
            KnowledgeDocumentType.Manual,
            KnowledgeDocumentAccessScope.Shared);

        document.MarkProcessing(UtcNow.AddMinutes(1));
        document.MarkFailed(
            "Knowledge.TextlessPdf",
            "OCR is not supported",
            UtcNow.AddMinutes(2));

        Assert.Equal(KnowledgeDocumentStatus.Failed, document.Status);
        Assert.Equal("Knowledge.TextlessPdf", document.LastErrorCode);
        Assert.Equal("OCR is not supported", document.LastErrorMessage);

        document.Retry(UtcNow.AddMinutes(3));

        Assert.Equal(KnowledgeDocumentStatus.Pending, document.Status);
        Assert.Equal(0, document.ProcessingAttemptCount);
        Assert.Null(document.LastErrorCode);
    }

    [Fact]
    public void ChangeAccessScope_ShouldEnforceDocumentBoundary()
    {
        KnowledgeDocument document = Create(
            KnowledgeDocumentType.CustomerServiceReport,
            KnowledgeDocumentAccessScope.Management);

        Assert.Throws<ArgumentException>(() =>
            document.ChangeAccessScope(
                KnowledgeDocumentAccessScope.Shared,
                UtcNow.AddMinutes(1)));

        Assert.Equal(
            KnowledgeDocumentAccessScope.Management,
            document.AccessScope);
    }

    [Fact]
    public void DeletedDocument_ShouldBecomeInaccessibleAndImmutable()
    {
        KnowledgeDocument document = Create(
            KnowledgeDocumentType.Manual,
            KnowledgeDocumentAccessScope.Shared);

        document.MarkDeleted(UtcNow.AddMinutes(1));

        Assert.Equal(KnowledgeDocumentStatus.Deleted, document.Status);
        Assert.False(document.CanBeAccessedByRole(UserRoles.Owner));
        Assert.Throws<InvalidOperationException>(() =>
            document.ChangeAccessScope(
                KnowledgeDocumentAccessScope.Management,
                UtcNow.AddMinutes(2)));
    }

    private static KnowledgeDocument Create(
        KnowledgeDocumentType type,
        KnowledgeDocumentAccessScope scope) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "procedure.pdf",
            "application/pdf",
            "knowledge/org/document/source.pdf",
            new string('a', 64),
            1024,
            type,
            scope,
            UtcNow);
}
