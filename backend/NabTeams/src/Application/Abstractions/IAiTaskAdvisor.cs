using NabTeams.Application.Tasks.Models;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Abstractions;

public interface IAiTaskAdvisor
{
    Task<TaskAdviceResult> GenerateAdviceAsync(TaskAdviceRequest request, CancellationToken cancellationToken = default);
}
