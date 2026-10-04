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

public class ExamResultAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ExamResultAppService(LearningBffDbContext db, ICurrentUser currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    /// <summary>Bắt đầu thi — tạo ExamResult mới, trả về ID để redirect đến trang làm bài</summary>
    public async Task<long> StartExamAsync(long examId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để thi.");
        return await StartExamAsync(examId, userId, allowUnenrolled: false);
    }

    private async Task<long> StartExamAsync(long examId, Guid userId, bool allowUnenrolled)
    {
        var exam = await _db.Exams.AsNoTracking()
            .Include(e => e.ExamQuestions)
            .FirstOrDefaultAsync(e => e.Id == examId)
            ?? throw new UserFriendlyException("Không tìm thấy đề thi.");

        if (!exam.IsPublished)
            throw new UserFriendlyException("Đề thi chưa được phát hành.");

        var now = DateTime.UtcNow;
        if (exam.StartTime.HasValue && exam.StartTime > now)
            throw new UserFriendlyException("Đề thi chưa bắt đầu.");
        if (exam.EndTime.HasValue && exam.EndTime < now)
            throw new UserFriendlyException("Đề thi đã kết thúc.");

        if (exam.ExamQuestions.Count == 0)
            throw new UserFriendlyException("Đề thi phải có ít nhất một câu hỏi.");

        var questionScoreTotal = exam.ExamQuestions.Sum(question => (double)question.Score);
        if (exam.ExamQuestions.Any(question => !float.IsFinite(question.Score) || question.Score < 0)
            || !float.IsFinite(exam.MaxScore)
            || exam.MaxScore <= 0
            || Math.Abs(questionScoreTotal - exam.MaxScore) > 0.001)
            throw new UserFriendlyException("Tổng điểm câu hỏi phải bằng tổng điểm đề thi.");

        if (exam.MaxAttempts.HasValue)
        {
            var submittedAttempts = await _db.ExamResults.CountAsync(result =>
                result.ExamId == examId
                && result.UserId == userId
                && result.Status == ExamResultStatus.Submitted);
            if (submittedAttempts >= exam.MaxAttempts.Value)
                throw new UserFriendlyException($"Bạn đã dùng hết {exam.MaxAttempts.Value} lượt thi.");
        }

        if (!allowUnenrolled && !await _db.EnrollmentSubjects.AnyAsync(registration =>
                registration.UserId == userId && registration.SubjectId == exam.SubjectId && registration.IsActive))
            throw new UserFriendlyException("Bạn cần đăng ký môn học để bắt đầu đề thi.");

        // Cancel any unfinished in-progress session for this exam
        var inProgress = await _db.ExamResults
            .Where(r => r.ExamId == examId && r.UserId == userId && r.Status == ExamResultStatus.InProgress)
            .ToListAsync();
        foreach (var r in inProgress)
            r.Status = ExamResultStatus.Expired;

        var attemptNumber = await _db.ExamResults
            .CountAsync(r => r.ExamId == examId && r.UserId == userId) + 1;

        var result = new ExamResult(examId, userId, attemptNumber);
        _db.ExamResults.Add(result);
        await _db.SaveChangesAsync();
        return result.Id;
    }

    public async Task<bool> CanRetakeAsync(long resultId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để thi.");
        var now = DateTime.UtcNow;

        var exam = await _db.ExamResults.AsNoTracking()
            .Where(result => result.Id == resultId
                && result.UserId == userId
                && result.Status == ExamResultStatus.Submitted)
            .Select(result => new
            {
                result.ExamId,
                result.Exam!.IsPublished,
                result.Exam.StartTime,
                result.Exam.EndTime,
                result.Exam.MaxAttempts
            })
            .FirstOrDefaultAsync();

        if (exam == null
            || !exam.IsPublished
            || (exam.StartTime.HasValue && exam.StartTime > now)
            || (exam.EndTime.HasValue && exam.EndTime < now))
            return false;

        if (!exam.MaxAttempts.HasValue)
            return true;

        var submittedAttempts = await _db.ExamResults.CountAsync(result =>
            result.ExamId == exam.ExamId
            && result.UserId == userId
            && result.Status == ExamResultStatus.Submitted);
        return submittedAttempts < exam.MaxAttempts.Value;
    }

    public async Task<long> StartRetakeAsync(long resultId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để thi.");
        var examId = await _db.ExamResults.AsNoTracking()
            .Where(result => result.Id == resultId
                && result.UserId == userId
                && result.Status == ExamResultStatus.Submitted)
            .Select(result => (long?)result.ExamId)
            .FirstOrDefaultAsync()
            ?? throw new UserFriendlyException("Không tìm thấy kết quả thi.");

        return await StartExamAsync(examId, userId, allowUnenrolled: true);
    }

    /// <summary>Học sinh nộp bài — chấm điểm và lưu kết quả</summary>
    public async Task<ExamResultDto> SubmitExamAsync(SubmitExamDto input)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để nộp bài.");

        var result = await _db.ExamResults
            .Include(r => r.Exam)
                .ThenInclude(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(r => r.Id == input.ExamResultId && r.UserId == userId)
            ?? throw new UserFriendlyException("Không tìm thấy bài thi.");

        if (result.Status != ExamResultStatus.InProgress)
            throw new UserFriendlyException("Bài thi đã được nộp hoặc đã hết hạn.");

        var exam         = result.Exam!;
        float totalScore = 0;
        var resultAnswers = new List<ExamResultAnswer>();

        foreach (var eq in exam.ExamQuestions)
        {
            var question    = eq.Question!;
            var studentAns  = input.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var selectedIds = studentAns?.SelectedAnswerIds ?? new List<long>();

            bool isCorrect;

            if (question.Type == QuestionType.Single)
            {
                var correctAnswerId = question.Answers.FirstOrDefault(a => a.IsCorrect)?.Id;
                isCorrect = selectedIds.Count == 1 && selectedIds[0] == correctAnswerId;
            }
            else // Multi
            {
                var correctIds = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).OrderBy(x => x).ToList();
                isCorrect = selectedIds.OrderBy(x => x).SequenceEqual(correctIds);
            }

            float earnedScore = isCorrect ? eq.Score : 0;
            totalScore += earnedScore;

            // Lưu từng câu trả lời đã chọn
            if (selectedIds.Any())
            {
                foreach (var answerId in selectedIds)
                {
                    resultAnswers.Add(new ExamResultAnswer(result.Id, question.Id, answerId, isCorrect, earnedScore));
                }
            }
            // Không chọn: không lưu record (question bị bỏ qua = 0 điểm)
        }

        // Quy đổi sang thang điểm MaxScore
        var totalExamScore = exam.ExamQuestions.Sum(eq => eq.Score);
        float normalizedScore = 0;
        if (totalExamScore > 0)
            normalizedScore = (float)Math.Round(totalScore / totalExamScore * exam.MaxScore, 2);

        result.Score      = normalizedScore;
        result.MaxScore   = exam.MaxScore;
        result.IsPassed   = normalizedScore >= exam.PassScore;
        result.SubmitTime = DateTime.UtcNow;
        result.Status     = ExamResultStatus.Submitted;

        _db.ExamResultAnswers.AddRange(resultAnswers);
        await _db.SaveChangesAsync();

        return await GetResultAsync(result.Id);
    }

    /// <summary>Lấy chi tiết kết quả</summary>
    public async Task<ExamResultDto> GetResultAsync(long resultId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem kết quả.");

        var result = await _db.ExamResults.AsNoTracking()
            .Include(r => r.Exam)
                .ThenInclude(e => e!.ExamQuestions.OrderBy(eq => eq.Order))
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers.OrderBy(a => a.Order))
            .Include(r => r.User)
            .Include(r => r.ExamResultAnswers)
                .ThenInclude(ra => ra.Question)
            .Include(r => r.ExamResultAnswers)
                .ThenInclude(ra => ra.Answer)
            .FirstOrDefaultAsync(r => r.Id == resultId && r.UserId == userId)
            ?? throw new UserFriendlyException("Không tìm thấy kết quả.");

        // Student selected answer IDs, grouped by question
        var selectedByQuestion = result.ExamResultAnswers
            .GroupBy(ra => ra.QuestionId)
            .ToDictionary(g => g.Key, g => g.Select(ra => ra.AnswerId).ToHashSet());

        // isCorrect per question (all rows for same question have same IsCorrect)
        var isCorrectByQuestion = result.ExamResultAnswers
            .GroupBy(ra => ra.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().IsCorrect);

        // earnedScore per question
        var earnedByQuestion = result.ExamResultAnswers
            .GroupBy(ra => ra.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().EarnedScore);

        // Build Questions review list from exam definition
        var questionsList = result.Exam?.ExamQuestions.Select(eq =>
        {
            var q = eq.Question!;
            var selected = selectedByQuestion.GetValueOrDefault(q.Id) ?? new HashSet<long>();
            return new ExamResultQuestionDto
            {
                QuestionId         = q.Id,
                QuestionTitle      = q.Title,
                QuestionType       = q.Type,
                IsCorrect          = isCorrectByQuestion.GetValueOrDefault(q.Id, false),
                EarnedScore        = earnedByQuestion.GetValueOrDefault(q.Id, 0f),
                GeneralExplanation = q.GeneralExplanation,
                Options            = q.Answers.Select(a => new ExamResultOptionDto
                {
                    AnswerId           = a.Id,
                    Text               = a.Text,
                    IsCorrectAnswer    = a.IsCorrect,
                    IsSelectedByStudent = selected.Contains(a.Id),
                    Explanation        = a.Explanation
                }).ToList()
            };
        }).ToList() ?? new();

        return new ExamResultDto
        {
            Id            = result.Id,
            ExamId        = result.ExamId,
            ExamTitle     = result.Exam?.Title ?? string.Empty,
            SubjectId     = result.Exam?.SubjectId ?? 0,
            SubjectName   = result.Exam?.Subject?.Name ?? string.Empty,
            UserId        = result.UserId,
            UserName      = result.User?.UserName ?? string.Empty,
            Score         = result.Score,
            MaxScore      = result.MaxScore,
            IsPassed      = result.IsPassed,
            StartTime     = result.StartTime,
            SubmitTime    = result.SubmitTime,
            Status        = result.Status,
            AttemptNumber = result.AttemptNumber,
            Questions     = questionsList,
            Answers       = result.ExamResultAnswers.Select(ra => new ExamResultAnswerDto
            {
                QuestionId    = ra.QuestionId,
                QuestionTitle = ra.Question?.Title ?? string.Empty,
                AnswerId      = ra.AnswerId,
                AnswerText    = ra.Answer?.Text,
                IsCorrect     = ra.IsCorrect,
                Score         = ra.EarnedScore,
                Explanation   = ra.Answer?.Explanation
            }).ToList()
        };
    }

    public async Task<PagedResultDto<ExamResultDto>> GetMyResultsPageAsync(
        long? subjectId, int skipCount, int maxResultCount)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem kết quả.");

        var query = _db.ExamResults.AsNoTracking()
            .Where(result => result.UserId == userId && result.Status == ExamResultStatus.Submitted);
        if (subjectId.HasValue)
            query = query.Where(result => result.Exam!.SubjectId == subjectId.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(result => result.SubmitTime)
            .ThenByDescending(result => result.Id)
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 100))
            .Select(result => new ExamResultDto
            {
                Id = result.Id,
                ExamId = result.ExamId,
                ExamTitle = result.Exam!.Title,
                SubjectId = result.Exam.SubjectId,
                SubjectName = result.Exam.Subject!.Name,
                UserId = result.UserId,
                Score = result.Score,
                MaxScore = result.MaxScore,
                IsPassed = result.IsPassed,
                StartTime = result.StartTime,
                SubmitTime = result.SubmitTime,
                Status = result.Status,
                AttemptNumber = result.AttemptNumber
            })
            .ToListAsync();

        return new PagedResultDto<ExamResultDto>(totalCount, items);
    }

    public async Task<int> GetMyResultsCountAsync(long? subjectId = null)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem kết quả.");

        var query = _db.ExamResults.AsNoTracking()
            .Where(result => result.UserId == userId && result.Status == ExamResultStatus.Submitted);
        if (subjectId.HasValue)
            query = query.Where(result => result.Exam!.SubjectId == subjectId.Value);

        return await query.CountAsync();
    }

    public async Task<List<SubjectFilterDto>> GetMyResultSubjectsAsync()
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem kết quả.");

        return await _db.ExamResults.AsNoTracking()
            .Where(result => result.UserId == userId
                && result.Status == ExamResultStatus.Submitted)
            .Select(result => new SubjectFilterDto
            {
                Id = result.Exam!.SubjectId,
                Name = result.Exam.Subject!.Name
            })
            .Distinct()
            .OrderBy(subject => subject.Name)
            .ToListAsync();
    }

    public async Task<Dictionary<long, int>> GetMyAttemptCountsAsync(List<long> examIds)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem đề thi.");
        if (examIds.Count == 0)
            return new Dictionary<long, int>();

        return await _db.ExamResults.AsNoTracking()
            .Where(result => result.UserId == userId
                && result.Status == ExamResultStatus.Submitted
                && examIds.Contains(result.ExamId))
            .GroupBy(result => result.ExamId)
            .ToDictionaryAsync(group => group.Key, group => group.Count());
    }
}
