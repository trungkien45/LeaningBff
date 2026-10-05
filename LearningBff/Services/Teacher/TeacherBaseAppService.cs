using LearningBff.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LearningBff.Services.Teacher;

[Authorize(Roles = LearningBffConsts.Teacher)]
public abstract class TeacherBaseAppService : LearningBffAppService
{
    protected readonly IRepository<Subject, long> _subjectRepository;
    protected readonly ICurrentUser _currentUser;

    public TeacherBaseAppService(IRepository<Subject, long> subjectRepository, ICurrentUser currentUser)
    {
        _subjectRepository = subjectRepository;
        _currentUser = currentUser;
    }

    protected async Task<Subject> FindSubjectForTeacherAsync(long subjectId)
    {
        var userId = _currentUser.Id;
        var subject = await (await _subjectRepository.GetQueryableAsync())
            .Where(s => s.Id == subjectId && s.Teachers.Any(t => t.Id == userId))
            .Include(s => s.Questions)
            .Include(s => s.Exams)
            .Include(s => s.EnrollmentSubjects)
            .FirstOrDefaultAsync();
        if (subject == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy môn học hoặc bạn không có quyền truy cập.");
        }

        return subject;
    }
}