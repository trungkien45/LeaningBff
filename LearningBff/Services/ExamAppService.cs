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

public class ExamAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ExamAppService(LearningBffDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Danh sách các đề thi đang mở cho học viên
    /// </summary>
    public async Task<List<ExamSummaryDto>> GetPublishedExamsAsync(long? subjectId = null)
    {
        var userId = _currentUser.Id;
        var now = DateTime.UtcNow;

        var query = _db.Exams.AsNoTracking()
            .Where(e => e.IsPublished
                && (e.StartTime == null || e.StartTime <= now)
                && (e.EndTime == null || e.EndTime >= now))
            .Include(e => e.Subject)
            .Include(e => e.Chapter)
            .Include(e => e.ExamQuestions)
            .AsQueryable();

        if (subjectId.HasValue)
        {
            query = query.Where(e => e.SubjectId == subjectId.Value);
        }

        var exams = await query
            .OrderByDescending(e => e.CreationTime)
            .ToListAsync();

        var attemptsDict = new Dictionary<long, int>();
        if (userId.HasValue)
        {
            attemptsDict = await _db.ExamResults.AsNoTracking()
                .Where(r => r.UserId == userId.Value && r.Status == ExamResultStatus.Submitted)
                .GroupBy(r => r.ExamId)
                .Select(g => new { ExamId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ExamId, x => x.Count);
        }

        var result = new List<ExamSummaryDto>();
        foreach (var e in exams)
        {
            var attemptsUsed = attemptsDict.GetValueOrDefault(e.Id, 0);
            var canTake = !e.MaxAttempts.HasValue || attemptsUsed < e.MaxAttempts.Value;

            result.Add(new ExamSummaryDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                SubjectId = e.SubjectId,
                SubjectName = e.Subject?.Name ?? string.Empty,
                ChapterId = e.ChapterId,
                ChapterName = e.Chapter?.Name,
                DurationInMinutes = e.DurationInMinutes,
                PassScore = e.PassScore,
                MaxScore = e.MaxScore,
                IsPublished = e.IsPublished,
                ShuffleQuestions = e.ShuffleQuestions,
                ShuffleAnswers = e.ShuffleAnswers,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                MaxAttempts = e.MaxAttempts,
                AttemptsUsed = attemptsUsed,
                CanTake = canTake,
                QuestionCount = e.ExamQuestions.Count
            });
        }

        return result;
    }

    /// <summary>
    /// Danh sách môn học có đề thi để lọc
    /// </summary>
    public async Task<List<SubjectFilterDto>> GetPublishedExamSubjectsAsync()
    {
        return await _db.Subjects.AsNoTracking()
            .Where(s => s.IsActive && s.Exams.Any(e => e.IsPublished))
            .OrderBy(s => s.Name)
            .Select(s => new SubjectFilterDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync();
    }

    /// <summary>
    /// Lịch sử kết quả làm bài của học viên hiện tại
    /// </summary>
    public async Task<List<ExamResultDto>> GetMyExamHistoryAsync(long? subjectId = null)
    {
        var userId = _currentUser.Id;
        if (!userId.HasValue)
        {
            return new List<ExamResultDto>();
        }

        var query = _db.ExamResults.AsNoTracking()
            .Where(r => r.UserId == userId.Value && r.Status == ExamResultStatus.Submitted)
            .Include(r => r.Exam)
                .ThenInclude(e => e!.Subject)
            .Include(r => r.ExamResultAnswers)
            .AsQueryable();

        if (subjectId.HasValue)
        {
            query = query.Where(r => r.Exam != null && r.Exam.SubjectId == subjectId.Value);
        }

        var list = await query
            .OrderByDescending(r => r.SubmitTime ?? r.CreationTime)
            .ToListAsync();

        return list.Select(r => new ExamResultDto
        {
            Id = r.Id,
            ExamId = r.ExamId,
            ExamTitle = !string.IsNullOrEmpty(r.ExamTitle) ? r.ExamTitle : (r.Exam?.Title ?? "Bài thi"),
            SubjectId = r.Exam?.SubjectId ?? 0,
            SubjectName = r.Exam?.Subject?.Name ?? string.Empty,
            UserId = r.UserId,
            Score = r.Score,
            MaxScore = r.MaxScore,
            IsPassed = r.IsPassed,
            StartTime = r.StartTime,
            SubmitTime = r.SubmitTime,
            Status = r.Status,
            AttemptNumber = r.AttemptNumber,
            TotalQuestions = r.ExamResultAnswers.Select(a => a.QuestionId).Distinct().Count(),
            CorrectQuestions = r.ExamResultAnswers.Where(a => a.IsCorrect).Select(a => a.QuestionId).Distinct().Count()
        }).ToList();
    }

    /// <summary>
    /// Bắt đầu làm bài thi: Khởi tạo ExamResult phiên thi mới
    /// </summary>
    public async Task<long> StartExamAsync(long examId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để làm bài thi.");

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
            throw new UserFriendlyException("Đề thi này chưa có câu hỏi nào.");

        if (exam.MaxAttempts.HasValue)
        {
            var attempts = await _db.ExamResults.CountAsync(r =>
                r.ExamId == examId && r.UserId == userId && r.Status == ExamResultStatus.Submitted);
            if (attempts >= exam.MaxAttempts.Value)
                throw new UserFriendlyException($"Bạn đã dùng hết {exam.MaxAttempts.Value} lượt thi cho đề thi này.");
        }

        // Hủy bất kỳ phiên thi dở dang trước đó của đề này
        var inProgress = await _db.ExamResults
            .Where(r => r.ExamId == examId && r.UserId == userId && r.Status == ExamResultStatus.InProgress)
            .ToListAsync();
        foreach (var p in inProgress)
        {
            p.Status = ExamResultStatus.Expired;
        }

        var attemptNumber = await _db.ExamResults
            .CountAsync(r => r.ExamId == examId && r.UserId == userId) + 1;

        var result = new ExamResult(examId, userId, attemptNumber)
        {
            ExamTitle = exam.Title,
            MaxScore = exam.MaxScore,
            Status = ExamResultStatus.InProgress,
            StartTime = DateTime.UtcNow
        };

        _db.ExamResults.Add(result);
        await _db.SaveChangesAsync();

        return result.Id;
    }

    /// <summary>
    /// Lấy dữ liệu đề thi để học sinh làm bài
    /// </summary>
    public async Task<ExamTakeDto> GetForTakeAsync(long examResultId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để làm bài thi.");

        var result = await _db.ExamResults.AsNoTracking()
            .Include(r => r.Exam)
                .ThenInclude(e => e!.Subject)
            .Include(r => r.Exam)
                .ThenInclude(e => e!.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
            .FirstOrDefaultAsync(r => r.Id == examResultId && r.UserId == userId)
            ?? throw new UserFriendlyException("Không tìm thấy phiên làm bài.");

        if (result.Status != ExamResultStatus.InProgress)
            throw new UserFriendlyException("Phiên làm bài này đã hoàn thành hoặc đã hết hạn.");

        var exam = result.Exam
            ?? throw new UserFriendlyException("Không tìm thấy thông tin đề thi.");

        var elapsedSeconds = (int)(DateTime.UtcNow - result.StartTime).TotalSeconds;
        var totalAllowedSeconds = exam.DurationInMinutes * 60;
        var remainingSeconds = Math.Max(0, totalAllowedSeconds - elapsedSeconds);

        var questions = exam.ExamQuestions.OrderBy(eq => eq.Order).Select(eq =>
        {
            var q = eq.Question!;
            var answers = q.Answers.OrderBy(a => a.Order).Select(a => new AnswerItemDto
            {
                Id = a.Id,
                Text = a.Text
            }).ToList();

            if (exam.ShuffleAnswers)
            {
                var rng = new Random((int)result.Id + (int)q.Id);
                answers = answers.OrderBy(_ => rng.Next()).ToList();
            }

            return new ExamTakeQuestionDto
            {
                QuestionId = q.Id,
                Title = q.Title,
                Type = q.Type,
                Score = eq.Score,
                Order = eq.Order,
                Answers = answers
            };
        }).ToList();

        if (exam.ShuffleQuestions)
        {
            var rng = new Random((int)result.Id);
            questions = questions.OrderBy(_ => rng.Next()).ToList();
        }

        return new ExamTakeDto
        {
            ExamResultId = result.Id,
            ExamId = exam.Id,
            Title = exam.Title,
            SubjectName = exam.Subject?.Name ?? string.Empty,
            DurationInMinutes = exam.DurationInMinutes,
            RemainingSeconds = remainingSeconds,
            StartTime = result.StartTime,
            Questions = questions
        };
    }

    /// <summary>
    /// Nộp bài thi và chấm điểm tự động
    /// </summary>
    public async Task<ExamResultDto> SubmitExamAsync(SubmitExamDto input)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để nộp bài.");

        var result = await _db.ExamResults
            .Include(r => r.Exam)
                .ThenInclude(e => e!.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
            .FirstOrDefaultAsync(r => r.Id == input.ExamResultId && r.UserId == userId)
            ?? throw new UserFriendlyException("Không tìm thấy phiên làm bài.");

        if (result.Status != ExamResultStatus.InProgress)
            throw new UserFriendlyException("Bài thi này đã được nộp hoặc đã hết hạn.");

        var exam = result.Exam!;
        float totalEarnedScore = 0;
        var examResultAnswers = new List<ExamResultAnswer>();

        foreach (var eq in exam.ExamQuestions)
        {
            var question = eq.Question!;
            var studentAns = input.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
            var selectedIds = studentAns?.SelectedAnswerIds ?? new List<long>();

            bool isCorrect = false;

            if (question.Type == QuestionType.Single)
            {
                var correctId = question.Answers.FirstOrDefault(a => a.IsCorrect)?.Id;
                isCorrect = correctId.HasValue && selectedIds.Count == 1 && selectedIds[0] == correctId.Value;
            }
            else // Multi
            {
                var correctIds = question.Answers.Where(a => a.IsCorrect).Select(a => a.Id).OrderBy(x => x).ToList();
                isCorrect = selectedIds.OrderBy(x => x).SequenceEqual(correctIds);
            }

            float earnedScore = isCorrect ? eq.Score : 0f;
            totalEarnedScore += earnedScore;

            if (selectedIds.Any())
            {
                foreach (var answerId in selectedIds)
                {
                    examResultAnswers.Add(new ExamResultAnswer(result.Id, question.Id, answerId, isCorrect, earnedScore));
                }
            }
        }

        // Quy đổi sang thang điểm MaxScore của đề thi
        var totalQuestionScore = exam.ExamQuestions.Sum(eq => eq.Score);
        float normalizedScore = 0;
        if (totalQuestionScore > 0)
        {
            normalizedScore = (float)Math.Round(totalEarnedScore / totalQuestionScore * exam.MaxScore, 2);
        }

        result.ExamTitle = exam.Title;
        result.Score = normalizedScore;
        result.MaxScore = exam.MaxScore;
        result.IsPassed = normalizedScore >= exam.PassScore;
        result.SubmitTime = DateTime.UtcNow;
        result.Status = ExamResultStatus.Submitted;

        _db.ExamResultAnswers.AddRange(examResultAnswers);
        await _db.SaveChangesAsync();

        return await GetResultAsync(result.Id);
    }

    /// <summary>
    /// Xem chi tiết kết quả bài thi và review đáp án
    /// </summary>
    public async Task<ExamResultDto> GetResultAsync(long resultId)
    {
        var userId = _currentUser.Id
            ?? throw new UserFriendlyException("Bạn cần đăng nhập để xem kết quả.");

        var result = await _db.ExamResults.AsNoTracking()
            .Include(r => r.Exam)
                .ThenInclude(e => e!.Subject)
            .Include(r => r.Exam)
                .ThenInclude(e => e!.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
            .Include(r => r.ExamResultAnswers)
            .FirstOrDefaultAsync(r => r.Id == resultId && r.UserId == userId)
            ?? throw new UserFriendlyException("Không tìm thấy kết quả bài thi.");

        var exam = result.Exam;

        var selectedByQuestion = result.ExamResultAnswers
            .GroupBy(ra => ra.QuestionId)
            .ToDictionary(g => g.Key, g => g.Select(ra => ra.AnswerId).ToHashSet());

        var isCorrectByQuestion = result.ExamResultAnswers
            .GroupBy(ra => ra.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().IsCorrect);

        var earnedByQuestion = result.ExamResultAnswers
            .GroupBy(ra => ra.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().EarnedScore);

        var questionsReview = new List<ExamResultQuestionDto>();
        if (exam != null)
        {
            foreach (var eq in exam.ExamQuestions.OrderBy(q => q.Order))
            {
                var q = eq.Question!;
                var studentSelected = selectedByQuestion.GetValueOrDefault(q.Id) ?? new HashSet<long>();

                questionsReview.Add(new ExamResultQuestionDto
                {
                    QuestionId = q.Id,
                    QuestionTitle = q.Title,
                    QuestionType = q.Type,
                    IsCorrect = isCorrectByQuestion.GetValueOrDefault(q.Id, false),
                    EarnedScore = earnedByQuestion.GetValueOrDefault(q.Id, 0f),
                    MaxScore = eq.Score,
                    GeneralExplanation = q.GeneralExplanation,
                    Options = q.Answers.OrderBy(a => a.Order).Select(a => new ExamResultOptionDto
                    {
                        AnswerId = a.Id,
                        Text = a.Text,
                        IsCorrectAnswer = a.IsCorrect,
                        IsSelectedByStudent = studentSelected.Contains(a.Id),
                        Explanation = a.Explanation
                    }).ToList()
                });
            }
        }

        var canRetake = false;
        if (exam != null && exam.IsPublished)
        {
            if (!exam.MaxAttempts.HasValue)
            {
                canRetake = true;
            }
            else
            {
                var usedAttempts = await _db.ExamResults.CountAsync(r =>
                    r.ExamId == exam.Id && r.UserId == userId && r.Status == ExamResultStatus.Submitted);
                canRetake = usedAttempts < exam.MaxAttempts.Value;
            }
        }

        return new ExamResultDto
        {
            Id = result.Id,
            ExamId = result.ExamId,
            ExamTitle = !string.IsNullOrEmpty(result.ExamTitle) ? result.ExamTitle : (exam?.Title ?? "Bài thi"),
            SubjectId = exam?.SubjectId ?? 0,
            SubjectName = exam?.Subject?.Name ?? string.Empty,
            UserId = result.UserId,
            Score = result.Score,
            MaxScore = result.MaxScore,
            IsPassed = result.IsPassed,
            StartTime = result.StartTime,
            SubmitTime = result.SubmitTime,
            Status = result.Status,
            AttemptNumber = result.AttemptNumber,
            CanRetake = canRetake,
            TotalQuestions = questionsReview.Count,
            CorrectQuestions = questionsReview.Count(q => q.IsCorrect),
            Questions = questionsReview
        };
    }
}
