using SRVS.Application.Models;
using SRVS.Domain.Entities;

namespace SRVS.Application.Abstractions;

public interface ISyllabusWorkflowService
{
    Task<SyllabusDocument> SaveDraftAsync(SyllabusDraftUpsertRequest request, CancellationToken cancellationToken = default);

    Task SubmitAsync(Guid syllabusDocumentId, string actorUserId, string actorName, CancellationToken cancellationToken = default);

    Task ApproveAsync(Guid syllabusDocumentId, string actorUserId, string actorName, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid syllabusDocumentId, string actorUserId, string actorName, string feedback, CancellationToken cancellationToken = default);

    Task<SyllabusDocument> RestoreVersionAsync(Guid syllabusVersionId, string actorUserId, string actorName, CancellationToken cancellationToken = default);
}