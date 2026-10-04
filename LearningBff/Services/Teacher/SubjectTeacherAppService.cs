using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Users;

namespace LearningBff.Services.Teacher
{
    [Authorize(Roles = LearningBffConsts.Teacher)]
    public class SubjectTeacherAppService : LearningBffAppService
    {
        private readonly LearningBffDbContext _db;
        private readonly ICurrentUser _currentUser;

        public SubjectTeacherAppService(LearningBffDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<(List<SubjectDto> Items, int TotalCount)> GetMySubjectsAsync(string name, int page = 1, int pageSize = 10)
        {
            var userId = _currentUser.Id;
            var query = _db.Subjects.AsNoTracking()
                .Where(s => s.Teachers.Any(t => t.Id == userId) && s.Name.Contains(name))
                .AsQueryable();

            var totalCount = await query.CountAsync();

            var safePage = Math.Max(1, page);
            var totalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
            if (totalPages > 0 && safePage > totalPages) safePage = totalPages;

            var subjects = await query
                .Include(s => s.Questions)
                .Include(s => s.Exams)
                .Include(s => s.EnrollmentSubjects)
                .OrderBy(s => s.Name)
                .Skip((safePage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (subjects.Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                CreationTime = s.CreationTime,
                QuestionCount = s.Questions.Count,
                ExamCount = s.Exams.Count,
                StudentCount = s.EnrollmentSubjects.Count(e => e.IsActive),
            }).ToList(), totalCount);
        }
        public async Task<SubjectDto> GetSubjectDetailAsync(long subjectId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            return new SubjectDto
            {
                Id = subject.Id,
                Name = subject.Name,
                Description = subject.Description,
                IsActive = subject.IsActive,
                CreationTime = subject.CreationTime,
                QuestionCount = subject.Questions.Count,
                ExamCount = subject.Exams.Count,
                StudentCount = subject.EnrollmentSubjects.Count(e => e.IsActive),
            };
        }
        
        private async Task<Subject> FindSubjectForTeacherAsync(long subjectId)
        {
            var userId = _currentUser.Id;
            var subject = await _db.Subjects.AsNoTracking()
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
}
