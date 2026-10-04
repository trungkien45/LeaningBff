using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Users;

namespace LearningBff.Services;

public class ExamAppService : LearningBffAppService
{
    private const double ScoreTolerance = 0.001;
    private readonly LearningBffDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ExamAppService(LearningBffDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // ─── Admin: full list ────────────────────────────────────────────────────
    public async Task<List<ExamSummaryDto>> GetListAsync(long? subjectId = null)
    {
        var query = _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
            .AsQueryable();

        if (subjectId.HasValue)
            query = query.Where(e => e.SubjectId == subjectId.Value);

        var exams = await query
            .OrderByDescending(e => e.CreationTime)
            .ToListAsync();
        return exams.Select(MapToSummary).ToList();
    }

    public async Task<PagedResultDto<ExamSummaryDto>> GetListPageAsync(
        long? subjectId, int skipCount, int maxResultCount)
    {
        var query = _db.Exams.AsNoTracking().AsQueryable();
        if (subjectId.HasValue)
            query = query.Where(exam => exam.SubjectId == subjectId.Value);

        var totalCount = await query.CountAsync();
        var exams = await query
            .Include(exam => exam.Subject)
            .Include(exam => exam.ExamQuestions)
            .OrderByDescending(exam => exam.CreationTime)
            .ThenByDescending(exam => exam.Id)
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 100))
            .ToListAsync();

        return new PagedResultDto<ExamSummaryDto>(totalCount, exams.Select(MapToSummary).ToList());
    }

    public async Task<PagedResultDto<ExamSummaryDto>> GetPublishedExamsPageAsync(
        long? subjectId, int skipCount, int maxResultCount)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem đề thi.");
        var query = GetPublishedExamsQuery(userId);

        if (subjectId.HasValue)
            query = query.Where(e => e.SubjectId == subjectId.Value);

        var totalCount = await query.CountAsync();
        var exams = await query
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
            .OrderByDescending(e => e.CreationTime)
            .ThenByDescending(e => e.Id)
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 100))
            .ToListAsync();

        return new PagedResultDto<ExamSummaryDto>(
            totalCount,
            exams.Select(MapToSummary).ToList());
    }

    public async Task<List<SubjectFilterDto>> GetPublishedExamSubjectsAsync()
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem đề thi.");

        return await _db.Subjects.AsNoTracking()
            .Where(subject => subject.IsActive
                && _db.EnrollmentSubjects.Any(registration => registration.UserId == userId
                    && registration.SubjectId == subject.Id
                    && registration.IsActive))
            .OrderBy(subject => subject.Name)
            .Select(subject => new SubjectFilterDto
            {
                Id = subject.Id,
                Name = subject.Name
            })
            .ToListAsync();
    }

    private IQueryable<Exam> GetPublishedExamsQuery(Guid userId)
    {
        var now = DateTime.UtcNow;
        return _db.Exams.AsNoTracking()
            .Where(exam => exam.IsPublished
                && (exam.StartTime == null || exam.StartTime <= now)
                && (exam.EndTime == null || exam.EndTime >= now)
                && _db.EnrollmentSubjects.Any(registration => registration.UserId == userId
                    && registration.SubjectId == exam.SubjectId
                    && registration.IsActive));
    }

    public async Task<ExamDto> GetAsync(long id)
    {
        var exam = await _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
                .ThenInclude(eq => eq.Question)
                    .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy đề thi ID={id}");

        return MapToDto(exam);
    }

    public async Task<ExamSummaryDto> CreateAsync(CreateExamDto input)
    {
        var subject = await _db.Subjects.FindAsync(input.SubjectId)
            ?? throw new UserFriendlyException($"Không tìm thấy môn học ID={input.SubjectId}");

        var exam = new Exam(input.Title, input.SubjectId, input.DurationInMinutes, input.PassScore, input.MaxScore)
        {
            Description      = input.Description,
            IsPublished      = input.IsPublished,
            ShuffleQuestions = input.ShuffleQuestions,
            ShuffleAnswers   = input.ShuffleAnswers,
            StartTime        = input.StartTime,
            EndTime          = input.EndTime,
            MaxAttempts      = input.MaxAttempts
        };

        _db.Exams.Add(exam);
        await SyncQuestionsAsync(exam, input.Questions);
        await _db.SaveChangesAsync();

        var saved = await _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
            .FirstAsync(e => e.Id == exam.Id);
        return MapToSummary(saved);
    }

    public async Task<ExamSummaryDto> UpdateAsync(long id, UpdateExamDto input)
    {
        var exam = await _db.Exams.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy đề thi ID={id}");
        if (!await _db.Subjects.AnyAsync(s => s.Id == input.SubjectId))
            throw new UserFriendlyException($"Không tìm thấy môn học ID={input.SubjectId}");

        exam.Title           = input.Title;
        exam.Description     = input.Description;
        exam.SubjectId       = input.SubjectId;
        exam.DurationInMinutes = input.DurationInMinutes;
        exam.PassScore       = input.PassScore;
        exam.MaxScore        = input.MaxScore;
        exam.IsPublished     = input.IsPublished;
        exam.ShuffleQuestions = input.ShuffleQuestions;
        exam.ShuffleAnswers  = input.ShuffleAnswers;
        exam.StartTime       = input.StartTime;
        exam.EndTime         = input.EndTime;
        exam.MaxAttempts     = input.MaxAttempts;

        await SyncQuestionsAsync(exam, input.Questions);

        await _db.SaveChangesAsync();

        var saved = await _db.Exams.AsNoTracking()
            .Include(e => e.Subject)
            .Include(e => e.ExamQuestions)
            .FirstAsync(e => e.Id == exam.Id);
        return MapToSummary(saved);
    }

    public async Task DeleteAsync(long id)
    {
        var exam = await _db.Exams.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy đề thi ID={id}");
        _db.Exams.Remove(exam);
        await _db.SaveChangesAsync();
    }

    // ─── Manage exam questions ───────────────────────────────────────────────
    public async Task AddQuestionAsync(long examId, AddExamQuestionDto input)
    {
        var exam = await _db.Exams.Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e => e.Id == examId)
            ?? throw new UserFriendlyException($"Không tìm thấy đề thi ID={examId}");

        if (exam.ExamQuestions.Any(eq => eq.QuestionId == input.QuestionId))
            throw new UserFriendlyException("Câu hỏi đã có trong đề thi này.");

        var question = await _db.Questions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == input.QuestionId)
            ?? throw new UserFriendlyException("Không tìm thấy câu hỏi trong ngân hàng câu hỏi.");
        if (question.SubjectId != exam.SubjectId)
            throw new UserFriendlyException("Chỉ có thể thêm câu hỏi thuộc cùng môn học với đề thi.");

        var updatedScores = exam.ExamQuestions.Select(eq => eq.Score).Append(input.Score).ToList();
        var updatedMaxScore = (float)updatedScores.Sum(score => (double)score);
        ValidateScoreTotal(updatedMaxScore, updatedScores);
        exam.MaxScore = updatedMaxScore;

        var maxOrder = exam.ExamQuestions.Any() ? exam.ExamQuestions.Max(q => q.Order) : 0;
        exam.ExamQuestions.Add(new ExamQuestion(examId, input.QuestionId, input.Score, maxOrder + 1));
        await _db.SaveChangesAsync();
    }

    public async Task RemoveQuestionAsync(long examId, long questionId)
    {
        var exam = await _db.Exams
            .Include(x => x.ExamQuestions)
            .FirstOrDefaultAsync(x => x.Id == examId)
            ?? throw new UserFriendlyException($"Không tìm thấy đề thi ID={examId}");
        var eq = exam.ExamQuestions.FirstOrDefault(x => x.QuestionId == questionId)
            ?? throw new UserFriendlyException("Không tìm thấy câu hỏi trong đề thi.");

        var remainingScores = exam.ExamQuestions
            .Where(x => x.QuestionId != questionId)
            .Select(x => x.Score)
            .ToList();
        var updatedMaxScore = (float)remainingScores.Sum(score => (double)score);
        ValidateScoreTotal(updatedMaxScore, remainingScores);

        _db.ExamQuestions.Remove(eq);
        exam.MaxScore = updatedMaxScore;
        await _db.SaveChangesAsync();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    private static ExamSummaryDto MapToSummary(Exam e)
    {
        var dto = new ExamSummaryDto
        {
            Id              = e.Id,
            Title           = e.Title,
            Description     = e.Description,
            SubjectId       = e.SubjectId,
            SubjectName     = e.Subject?.Name ?? string.Empty,
            DurationInMinutes = e.DurationInMinutes,
            PassScore       = e.PassScore,
            MaxScore        = e.MaxScore,
            IsPublished     = e.IsPublished,
            ShuffleQuestions = e.ShuffleQuestions,
            ShuffleAnswers  = e.ShuffleAnswers,
            StartTime       = e.StartTime,
            EndTime         = e.EndTime,
            MaxAttempts     = e.MaxAttempts,
            QuestionCount   = e.ExamQuestions.Count
        };
        dto.ExamQuestions = e.ExamQuestions.OrderBy(eq => eq.Order).Select(eq => new ExamQuestionDto
        {
            Id = eq.Id,
            QuestionId = eq.QuestionId,
            QuestionTitle = eq.Question?.Title ?? string.Empty,
            QuestionType = eq.Question?.Type ?? QuestionType.Single,
            Score = eq.Score,
            Order = eq.Order
        }).ToList();
        return dto;
    }

    private static ExamDto MapToDto(Exam e)
    {
        var dto = new ExamDto
        {
            Id              = e.Id,
            Title           = e.Title,
            Description     = e.Description,
            SubjectId       = e.SubjectId,
            SubjectName     = e.Subject?.Name ?? string.Empty,
            DurationInMinutes = e.DurationInMinutes,
            PassScore       = e.PassScore,
            MaxScore        = e.MaxScore,
            IsPublished     = e.IsPublished,
            ShuffleQuestions = e.ShuffleQuestions,
            ShuffleAnswers  = e.ShuffleAnswers,
            StartTime       = e.StartTime,
            EndTime         = e.EndTime,
            MaxAttempts     = e.MaxAttempts,
            QuestionCount   = e.ExamQuestions.Count
        };

        dto.ExamQuestions = e.ExamQuestions.OrderBy(eq => eq.Order).Select(eq => new ExamQuestionDto
        {
            Id            = eq.Id,
            QuestionId    = eq.QuestionId,
            QuestionTitle = eq.Question?.Title ?? string.Empty,
            QuestionType  = eq.Question?.Type ?? QuestionType.Single,
            Score         = eq.Score,
            Order         = eq.Order,
            Answers       = eq.Question?.Answers.OrderBy(a => a.Order).Select(a => new AnswerDto
            {
                Id        = a.Id,
                Text      = a.Text,
                IsCorrect = a.IsCorrect,
                Order     = a.Order
            }).ToList() ?? new()
        }).ToList();

        return dto;
    }

    private async Task SyncQuestionsAsync(Exam exam, List<AddExamQuestionDto>? questions)
    {
        questions ??= new();
        var selected = questions
            .Where(q => q.QuestionId > 0)
            .GroupBy(q => q.QuestionId)
            .Select(g => g.First())
            .ToList();

        ValidateScoreTotal(exam.MaxScore, selected.Select(q => q.Score).ToList());

        var questionIds = selected.Select(q => q.QuestionId).ToList();
        var selectedQuestionInfo = questionIds.Count == 0
            ? new List<Question>()
            : await _db.Questions.AsNoTracking().Where(q => questionIds.Contains(q.Id)).ToListAsync();
        if (selectedQuestionInfo.Count != questionIds.Count)
            throw new UserFriendlyException("Có câu hỏi không tồn tại trong ngân hàng câu hỏi.");
        if (selectedQuestionInfo.Any(q => q.SubjectId != exam.SubjectId))
            throw new UserFriendlyException("Tất cả câu hỏi trong đề thi phải thuộc môn học của đề.");

        if (exam.Id != 0)
            await _db.Entry(exam).Collection(e => e.ExamQuestions).LoadAsync();
        _db.ExamQuestions.RemoveRange(exam.ExamQuestions);
        exam.ExamQuestions.Clear();

        for (var index = 0; index < selected.Count; index++)
        {
            var item = selected[index];
            exam.ExamQuestions.Add(new ExamQuestion(exam.Id, item.QuestionId, item.Score, index + 1));
        }
    }

    private static void ValidateScoreTotal(float maxScore, List<float> questionScores)
    {
        if (questionScores.Count == 0)
            throw new UserFriendlyException("Đề thi phải có ít nhất một câu hỏi.");

        if (!float.IsFinite(maxScore) || maxScore <= 0
            || questionScores.Any(score => !float.IsFinite(score) || score < 0))
            throw new UserFriendlyException("Tổng điểm đề thi và điểm câu hỏi phải là số hợp lệ, không âm.");

        var scoreTotal = questionScores.Sum(score => (double)score);
        if (Math.Abs(scoreTotal - maxScore) > ScoreTolerance)
            throw new UserFriendlyException(
                $"Tổng điểm câu hỏi ({scoreTotal:0.###}) phải bằng tổng điểm đề thi ({maxScore:0.###}).");
    }
}
