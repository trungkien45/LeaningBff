using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Volo.Abp.Users;

namespace LearningBff.Services;

public class EnrollmentSubjectAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly ICurrentUser _currentUser;

    public EnrollmentSubjectAppService(LearningBffDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<List<EnrollmentSubjectDto>> GetSubjectsAsync()
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để đăng ký môn học.");

        var registeredIds = await _db.EnrollmentSubjects.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.SubjectId)
            .ToListAsync();
        var registeredSet = registeredIds.ToHashSet();

        var subjects = await _db.Subjects.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description })
            .ToListAsync();

        return subjects.Select(x => new EnrollmentSubjectDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            IsRegistered = registeredSet.Contains(x.Id)
        }).ToList();
    }

    public async Task RegisterAsync(long subjectId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để đăng ký môn học.");

        var subjectExists = await _db.Subjects.AnyAsync(x => x.Id == subjectId && x.IsActive);
        if (!subjectExists)
            throw new UserFriendlyException("Môn học không tồn tại hoặc đã ngừng hoạt động.");

        var registration = await _db.EnrollmentSubjects
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SubjectId == subjectId);

        if (registration?.IsActive == true)
            throw new UserFriendlyException("Bạn đã đăng ký môn học này.");

        if (registration == null)
        {
            _db.EnrollmentSubjects.Add(new EnrollmentSubject
            {
                UserId = userId,
                SubjectId = subjectId
            });
        }
        else
        {
            registration.IsActive = true;
            registration.RegisteredAt = DateTime.UtcNow;
            registration.UnregisteredAt = null;
        }

        await _db.SaveChangesAsync();
    }

    public async Task UnregisterAsync(long subjectId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để hủy đăng ký.");

        var registration = await _db.EnrollmentSubjects
            .FirstOrDefaultAsync(x => x.UserId == userId && x.SubjectId == subjectId && x.IsActive)
            ?? throw new UserFriendlyException("Bạn chưa đăng ký môn học này.");

        registration.IsActive = false;
        registration.UnregisteredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}