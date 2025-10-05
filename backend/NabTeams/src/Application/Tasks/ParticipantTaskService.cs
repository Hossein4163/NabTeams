using NabTeams.Application.Abstractions;
using NabTeams.Application.Tasks.Models;
using NabTeams.Domain.Entities;
using NabTeams.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NabTeams.Application.Tasks;

public class ParticipantTaskService : IParticipantTaskService
{
    private readonly IParticipantTaskRepository _taskRepository;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IAiTaskAdvisor _aiTaskAdvisor;

    public ParticipantTaskService(
        IParticipantTaskRepository taskRepository,
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IAiTaskAdvisor aiTaskAdvisor)
    {
        _taskRepository = taskRepository;
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _aiTaskAdvisor = aiTaskAdvisor;
    }

    public async Task<ParticipantTask> CreateAsync(Guid participantId, ParticipantTaskInput input, CancellationToken cancellationToken = default)
    {
        var (registration, _) = await EnsureTaskManagerEnabledAsync(participantId, cancellationToken);
        var eventId = input.EventId == Guid.Empty ? registration.EventId : input.EventId;
        if (eventId != registration.EventId)
        {
            throw new InvalidOperationException("Task event does not match the participant registration event.");
        }

        var task = new ParticipantTask
        {
            ParticipantRegistrationId = participantId,
            EventId = eventId,
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            AssignedTo = string.IsNullOrWhiteSpace(input.AssignedTo) ? null : input.AssignedTo.Trim(),
            DueAt = input.DueAt,
            Status = ParticipantTaskStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return await _taskRepository.CreateAsync(task, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetAsync(taskId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        await EnsureTaskManagerEnabledAsync(existing.ParticipantRegistrationId, cancellationToken);
        return await _taskRepository.DeleteAsync(taskId, cancellationToken);
    }

    public async Task<TaskAdviceResult> GenerateAdviceAsync(Guid participantId, TaskAdviceRequest request, CancellationToken cancellationToken = default)
    {
        var (registration, eventDetail) = await EnsureTaskManagerEnabledAsync(participantId, cancellationToken);
        var tasks = await _taskRepository.ListForParticipantAsync(participantId, cancellationToken);

        var normalizedRequest = request with
        {
            ParticipantRegistrationId = participantId,
            EventId = registration.EventId,
            ExistingTasks = tasks
                .Select(t => $"{t.Title} | وضعیت: {t.Status} | توضیح: {t.Description}")
                .ToArray(),
            TaskContext = string.IsNullOrWhiteSpace(request.TaskContext)
                ? BuildDefaultContext(registration, tasks)
                : request.TaskContext
        };

        return await _aiTaskAdvisor.GenerateAdviceAsync(normalizedRequest, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParticipantTask>> ListAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var (_, eventDetail) = await EnsureTaskManagerEnabledAsync(participantId, cancellationToken);
        if (!eventDetail.AiTaskManagerEnabled)
        {
            return Array.Empty<ParticipantTask>();
        }

        return await _taskRepository.ListForParticipantAsync(participantId, cancellationToken);
    }

    public async Task<ParticipantTask?> UpdateAsync(Guid taskId, ParticipantTaskInput input, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetAsync(taskId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var (registration, _) = await EnsureTaskManagerEnabledAsync(existing.ParticipantRegistrationId, cancellationToken);
        var eventId = input.EventId == Guid.Empty ? registration.EventId : input.EventId;
        if (eventId != registration.EventId)
        {
            throw new InvalidOperationException("Task event does not match the participant registration event.");
        }

        var updated = existing with
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            DueAt = input.DueAt,
            AssignedTo = string.IsNullOrWhiteSpace(input.AssignedTo) ? null : input.AssignedTo.Trim(),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        return await _taskRepository.UpdateAsync(taskId, updated, cancellationToken);
    }

    public async Task<ParticipantTask?> UpdateStatusAsync(Guid taskId, ParticipantTaskStatus status, CancellationToken cancellationToken = default)
    {
        var existing = await _taskRepository.GetAsync(taskId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        await EnsureTaskManagerEnabledAsync(existing.ParticipantRegistrationId, cancellationToken);
        return await _taskRepository.UpdateStatusAsync(taskId, status, null, cancellationToken);
    }

    private static string BuildDefaultContext(ParticipantRegistration registration, IReadOnlyCollection<ParticipantTask> tasks)
    {
        var members = registration.Members?.Select(m => $"{m.FullName} ({m.Role})") ?? Array.Empty<string>();
        var openTasks = tasks.Where(t => t.Status != ParticipantTaskStatus.Completed && t.Status != ParticipantTaskStatus.Archived)
            .Select(t => $"- {t.Title}: {t.Description}")
            .ToArray();

        return string.Join('\n', new[]
        {
            $"نام تیم: {registration.TeamName}",
            $"حوزه: {registration.FieldOfStudy}",
            members.Any() ? $"اعضای تیم: {string.Join(", ", members)}" : null,
            openTasks.Any() ? "تسک‌های باز:\n" + string.Join('\n', openTasks) : null,
            string.IsNullOrWhiteSpace(registration.AdditionalNotes) ? null : $"یادداشت‌ها: {registration.AdditionalNotes}"
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private async Task<(ParticipantRegistration registration, EventDetail eventDetail)> EnsureTaskManagerEnabledAsync(Guid participantId, CancellationToken cancellationToken)
    {
        var registration = await _registrationRepository.GetParticipantAsync(participantId, cancellationToken)
            ?? throw new InvalidOperationException($"Participant registration {participantId} not found");

        if (registration.EventId == Guid.Empty)
        {
            throw new InvalidOperationException("Participant registration is not associated with an event.");
        }

        var eventDetail = await _eventRepository.GetAsync(registration.EventId, cancellationToken)
            ?? throw new InvalidOperationException("Event configuration not found for the participant registration.");

        if (!eventDetail.AiTaskManagerEnabled)
        {
            throw new InvalidOperationException("AI task manager is disabled for this event.");
        }

        return (registration, eventDetail);
    }
}
