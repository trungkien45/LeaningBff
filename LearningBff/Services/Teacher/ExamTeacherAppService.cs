using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LearningBff.Entities;
using LearningBff.Dtos;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LearningBff.Services.Teacher
{
    public class ExamTeacherAppService : TeacherBaseAppService
    {
        private readonly IRepository<Exam, long> _examRepository;
        private readonly IRepository<Question, long> _questionRepository;
        private readonly IRepository<ExamResult, long> _examResultRepository;
        private readonly IRepository<ExamQuestion, long> _examQuestionRepository;

        private const double ScoreTolerance = 0.001;

        public ExamTeacherAppService(
            IRepository<Subject, long> subjectRepository,
            IRepository<Exam, long> examRepository,
            IRepository<Question, long> questionRepository,
            IRepository<ExamResult, long> examResultRepository,
            IRepository<ExamQuestion, long> examQuestionRepository,
            ICurrentUser currentUser) : base(subjectRepository, currentUser)
        {
            _examRepository = examRepository;
            _questionRepository = questionRepository;
            _examResultRepository = examResultRepository;
            _examQuestionRepository = examQuestionRepository;
        }
        public async Task<(List<ExamDto> Exams, int TotalCount)> GetExamsOfSubjectAsync(long? subjectId, string searchName = "", int page = 1, int pageSize = 9)
        {
            var subjectIdquery = (await GetQueryableSubjectsForTeacherAsync()).Select(s => s.Id);
            if (subjectId.HasValue && !await subjectIdquery.ContainsAsync(subjectId.Value))
            {
                throw new UserFriendlyException("Bạn không có quyền truy cập môn học này.");
            }
            var query = (await _examRepository.GetQueryableAsync())
                .Where(e => ((subjectId == null && subjectIdquery.Contains(e.SubjectId)) || (subjectId.HasValue && e.SubjectId == subjectId.Value)) && (string.IsNullOrEmpty(searchName) || e.Title.Contains(searchName)))
                .OrderBy(e => e.CreationTime);
            var totalCount = await query.CountAsync();
            var exams = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExamDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                SubjectId = e.SubjectId,
                SubjectName = e.Subject != null ? e.Subject.Name : string.Empty,
                CreationTime = e.CreationTime,
                IsPublished = e.IsPublished,
                AttemptsUsed = e.ExamResults.Count
            }).ToListAsync();
            return (exams, totalCount);
        }
        public async Task<ExamSummaryDto> GetExamDetailAsync(long subjectId, long examId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            return new ExamSummaryDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                SubjectId = exam.SubjectId,
                AttemptsUsed = exam.ExamResults.Count,
                CanTake = exam.IsPublished && (exam.StartTime == null || exam.StartTime <= DateTime.UtcNow) && (exam.EndTime == null || exam.EndTime >= DateTime.UtcNow),
                DurationInMinutes = exam.DurationInMinutes,
                PassScore = exam.PassScore,
                ChapterId = exam.ChapterId,
                ChapterName = exam.Chapter?.Name,
                EndTime = exam.EndTime,
                IsPublished = exam.IsPublished,
                MaxAttempts = exam.MaxAttempts,
                MaxScore = exam.MaxScore,
                QuestionCount = exam.ExamQuestions.Count,
                ShuffleAnswers = exam.ShuffleAnswers,
                ShuffleQuestions = exam.ShuffleQuestions,
                StartTime = exam.StartTime,
                SubjectName = exam.Subject?.Name ?? string.Empty,
            };
        }
        public async Task<ExamDto> CreateExamAsync(long subjectId, CreateExamDto input)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            if (input.IsPublished)
            {
                throw new UserFriendlyException("Hãy tạo đề thi ở trạng thái nháp, thêm câu hỏi và kiểm tra tổng điểm trước khi xuất bản.");
            }

            var trimmedTitle = input.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                throw new UserFriendlyException("Tiêu đề đề thi không được để trống.");
            }
            var exists = await _examRepository.AnyAsync(e => e.SubjectId == subjectId && e.Title.ToLower() == trimmedTitle.ToLower());
            if (exists)
            {
                throw new UserFriendlyException($"Đề thi '{trimmedTitle}' đã tồn tại trong môn học này.");
            }
            if (input.DurationInMinutes <= 0)
            {
                throw new UserFriendlyException("Thời lượng làm bài phải lớn hơn 0.");
            }
            if (input.PassScore < 0 || input.PassScore > input.MaxScore)
            {
                throw new UserFriendlyException("Điểm đạt phải nằm trong khoảng từ 0 đến điểm tối đa.");
            }
            var exam = new Exam(trimmedTitle, subjectId, input.DurationInMinutes, input.PassScore, input.MaxScore)
            {
                Description = input.Description?.Trim(),
                IsPublished = input.IsPublished,
                ShuffleQuestions = input.ShuffleQuestions,
                ShuffleAnswers = input.ShuffleAnswers,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                MaxAttempts = input.MaxAttempts
            };
            await _examRepository.InsertAsync(exam, autoSave: true);
            return new ExamDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                SubjectId = exam.SubjectId,
                CreationTime = exam.CreationTime
            };
        }
        public async Task<ExamDto> UpdateExamAsync(long subjectId, long examId, UpdateExamDto input)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            EnsureExamIsDraft(exam);
            if (input.IsPublished)
            {
                throw new UserFriendlyException("Hãy sử dụng chức năng xuất bản đề thi.");
            }
            var trimmedTitle = input.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(trimmedTitle))
            {
                throw new UserFriendlyException("Tiêu đề đề thi không được để trống.");
            }
            var exists = await _examRepository.AnyAsync(e => e.SubjectId == subjectId && e.Title.ToLower() == trimmedTitle.ToLower() && e.Id != examId);
            if (exists)
            {
                throw new UserFriendlyException($"Đề thi '{trimmedTitle}' đã tồn tại trong môn học này.");
            }
            if (input.DurationInMinutes <= 0)
            {
                throw new UserFriendlyException("Thời lượng làm bài phải lớn hơn 0.");
            }
            if (input.PassScore < 0 || input.PassScore > input.MaxScore)
            {
                throw new UserFriendlyException("Điểm đạt phải nằm trong khoảng từ 0 đến điểm tối đa.");
            }
            exam.Title = trimmedTitle;
            exam.Description = input.Description?.Trim() ?? string.Empty;
            exam.DurationInMinutes = input.DurationInMinutes;
            exam.PassScore = input.PassScore;
            exam.MaxScore = input.MaxScore;
            exam.IsPublished = input.IsPublished;
            exam.ShuffleQuestions = input.ShuffleQuestions;
            exam.ShuffleAnswers = input.ShuffleAnswers;
            exam.StartTime = input.StartTime;
            exam.EndTime = input.EndTime;
            exam.MaxAttempts = input.MaxAttempts;
            await _examRepository.UpdateAsync(exam, autoSave: true);
            return new ExamDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                SubjectId = exam.SubjectId,
                CreationTime = exam.CreationTime
            };
        }
        public async Task<ExamDto> PublishExamAsync(long subjectId, long examId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            var questions = await _examQuestionRepository.GetListAsync(q => q.ExamId == examId);
            ValidateScoreTotal(exam.MaxScore, questions.Select(q => q.Score).ToList());

            exam.IsPublished = true;
            await _examRepository.UpdateAsync(exam, autoSave: true);
            return new ExamDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                SubjectId = exam.SubjectId,
                CreationTime = exam.CreationTime
            };
        }
        public async Task<ExamDto> UnpublishExamAsync(long subjectId, long examId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            if (!exam.IsPublished)
            {
                throw new UserFriendlyException("Đề thi này chưa được xuất bản.");
            }
            var deletionTime = DateTime.UtcNow;
            await (await _examResultRepository.GetQueryableAsync())
                .Where(result => result.ExamId == examId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(result => result.IsDeleted, true)
                    .SetProperty(result => result.DeletionTime, deletionTime)
                    .SetProperty(result => result.DeleterId, _currentUser.Id));

            exam.IsPublished = false;
            await _examRepository.UpdateAsync(exam, autoSave: true);
            return new ExamDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                SubjectId = exam.SubjectId,
                CreationTime = exam.CreationTime
            };
        }
        public async Task<ExamQuestionScoreSummaryDto> AddQuestionToExamAsync(long subjectId, long examId, long questionId, float score)
        {
            if (!float.IsFinite(score) || score < 0)
            {
                throw new UserFriendlyException("Điểm câu hỏi phải là số không âm hợp lệ.");
            }

            await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            EnsureExamIsDraft(exam);
            var question = await _questionRepository.FindAsync(questionId);
            if (question == null || question.SubjectId != subjectId)
            {
                throw new UserFriendlyException("Không tìm thấy câu hỏi.");
            }

            var questions = await _examQuestionRepository.GetListAsync(eq => eq.ExamId == examId);
            if (questions.Any(eq => eq.QuestionId == questionId))
            {
                throw new UserFriendlyException("Câu hỏi đã tồn tại trong đề thi.");
            }

            await _examQuestionRepository.InsertAsync(
                new ExamQuestion(examId, questionId, score, questions.Count + 1),
                autoSave: true);

            return await GetQuestionScoreSummaryAsync(examId, exam.MaxScore);
        }
        public async Task<ExamQuestionScoreSummaryDto> UpdateExamQuestionScoreAsync(long subjectId, long examId, long questionId, float score)
        {
            if (!float.IsFinite(score) || score < 0)
            {
                throw new UserFriendlyException("Điểm câu hỏi phải là số không âm hợp lệ.");
            }

            await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            EnsureExamIsDraft(exam);

            var examQuestion = await _examQuestionRepository.FirstOrDefaultAsync(
                q => q.ExamId == examId && q.QuestionId == questionId);
            if (examQuestion == null)
            {
                throw new UserFriendlyException("Câu hỏi không tồn tại trong đề thi.");
            }

            examQuestion.Score = score;
            await _examQuestionRepository.UpdateAsync(examQuestion, autoSave: true);
            return await GetQuestionScoreSummaryAsync(examId, exam.MaxScore);
        }

        public async Task<ExamQuestionScoreSummaryDto> RemoveQuestionFromExamAsync(long subjectId, long examId, long questionId)
        {
            await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            EnsureExamIsDraft(exam);
            var examQuestion = await _examQuestionRepository.FirstOrDefaultAsync(
                eq => eq.ExamId == examId && eq.QuestionId == questionId);
            if (examQuestion == null)
            {
                throw new UserFriendlyException("Câu hỏi không tồn tại trong đề thi.");
            }
            await _examQuestionRepository.DeleteAsync(examQuestion, autoSave: true);
            return await GetQuestionScoreSummaryAsync(examId, exam.MaxScore);
        }
        public async Task<ExamDetailDto> GetExamWithQuestionsAsync(long subjectId, long examId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Include(e => e.ExamQuestions)
                    .ThenInclude(eq => eq.Question)
                        .ThenInclude(q => q!.Answers)
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            return new ExamDetailDto
            {
                Id = exam.Id,
                Title = exam.Title,
                Description = exam.Description,
                SubjectId = exam.SubjectId,
                CreationTime = exam.CreationTime,
                DurationInMinutes = exam.DurationInMinutes,
                PassScore = exam.PassScore,
                MaxScore = exam.MaxScore,
                IsPublished = exam.IsPublished,
                ShuffleQuestions = exam.ShuffleQuestions,
                ShuffleAnswers = exam.ShuffleAnswers,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                MaxAttempts = exam.MaxAttempts,
                Questions = exam.ExamQuestions.Select(eq => new QuestionDto
                {
                    Id = eq.Question!.Id,
                    Title = eq.Question.Title,
                    Type = eq.Question.Type,
                    DefaultScore = eq.Score,
                    Difficulty = eq.Question.Difficulty,
                    GeneralExplanation = eq.Question.GeneralExplanation,
                    Order = eq.Question.Order
                }).ToList()
            };
        }
        public async Task<long> DeleteExamAsync(long subjectId, long examId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            EnsureExamIsDraft(exam);
            await _examRepository.DeleteAsync(exam, autoSave: true);
            return exam.Id;
        }
        public async Task<(List<ExamResultDto> ExamResults, int TotalCount)> GetExamResultsOfExamAsync(long subjectId, long examId, string searchName = "",
            int page = 1,
            int pageSize = 9)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            var query = (await _examResultRepository.GetQueryableAsync())
                .Where(r => r.ExamId == examId && (string.IsNullOrEmpty(searchName) || r.User!.UserName.Contains(searchName) || r.User.Name.Contains(searchName)))
                .OrderByDescending(r => r.SubmitTime);
            var totalCount = await query.CountAsync();
            var results = await query
                .Include(r => r.User)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            return (results.Select(r => new ExamResultDto
            {
                Id = r.Id,
                ExamId = r.ExamId,
                UserId = r.UserId,
                UserName = r.User?.UserName ?? string.Empty,
                UserFullName = r.User?.Name ?? string.Empty,
                Score = r.Score,
                Status = r.Status,
                IsPassed = r.IsPassed,
                SubmitTime = r.SubmitTime
            }).ToList(), totalCount);
        }
        public async Task<ExamResultDto> GetExamResultDetailAsync(long subjectId, long examId, long resultId)
        {
            Subject subject = await FindSubjectForTeacherAsync(subjectId);
            var exam = await (await _examRepository.GetQueryableAsync())
                .Where(e => e.SubjectId == subjectId && e.Id == examId)
                .FirstOrDefaultAsync();
            if (exam == null)
            {
                throw new UserFriendlyException("Không tìm thấy đề thi.");
            }
            var result = await (await _examResultRepository.GetQueryableAsync())
                .Include(r => r.User)
                .Where(r => r.ExamId == examId && r.Id == resultId)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                throw new UserFriendlyException("Không tìm thấy kết quả làm bài.");
            }
            return new ExamResultDto
            {
                Id = result.Id,
                ExamId = result.ExamId,
                UserId = result.UserId,
                UserName = result.User?.UserName ?? string.Empty,
                UserFullName = result.User?.Name ?? string.Empty,
                Score = result.Score,
                Status = result.Status,
                IsPassed = result.IsPassed,
                SubmitTime = result.SubmitTime
            };
        }

        private async Task<ExamQuestionScoreSummaryDto> GetQuestionScoreSummaryAsync(long examId, float maxScore)
        {
            var questions = await _examQuestionRepository.GetListAsync(q => q.ExamId == examId);
            var totalScore = questions.Sum(q => (double)q.Score);
            return new ExamQuestionScoreSummaryDto
            {
                TotalScore = (float)totalScore,
                MaxScore = maxScore,
                IsValid = float.IsFinite(maxScore)
                    && maxScore > 0
                    && questions.Count > 0
                    && questions.All(q => float.IsFinite(q.Score) && q.Score >= 0)
                    && Math.Abs(totalScore - maxScore) <= ScoreTolerance
            };
        }

        private static void EnsureExamIsDraft(Exam exam)
        {
            if (exam.IsPublished)
            {
                throw new UserFriendlyException("Không thể chỉnh sửa đề thi đã xuất bản. Hãy gỡ xuất bản trước.");
            }
        }

        private static void ValidateScoreTotal(float maxScore, List<float> questionScores)
        {
            if (questionScores.Count == 0)
            {
                throw new UserFriendlyException("Đề thi phải có ít nhất một câu hỏi.");
            }

            if (!float.IsFinite(maxScore) || maxScore <= 0
                || questionScores.Any(score => !float.IsFinite(score) || score < 0))
            {
                throw new UserFriendlyException("Tổng điểm đề thi và điểm câu hỏi phải là số hợp lệ, không âm.");
            }

            var scoreTotal = questionScores.Sum(score => (double)score);
            if (Math.Abs(scoreTotal - maxScore) > ScoreTolerance)
            {
                throw new UserFriendlyException(
                    $"Tổng điểm câu hỏi ({scoreTotal:0.###}) phải bằng tổng điểm đề thi ({maxScore:0.###}).");
            }
        }
    }
}