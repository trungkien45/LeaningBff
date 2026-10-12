using ClosedXML.Excel;
using LearningBff.Dtos;
using LearningBff.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;
using static LearningBff.Permissions.LearningBffPermissions;

namespace LearningBff.Services.Teacher;

public class QuestionBankTeacherAppService : TeacherBaseAppService
{
    private readonly IRepository<Question, long> _questionRepository;
    private readonly IRepository<Answer, long> _answerRepository;
    private readonly IRepository<ExamQuestion, long> _examQuestionRepository;

    public QuestionBankTeacherAppService(
        IRepository<Subject, long> subjectRepository,
        IRepository<Question, long> questionRepository,
        IRepository<Answer, long> answerRepository,
        IRepository<ExamQuestion, long> examQuestionRepository,
        ICurrentUser currentUser) : base(subjectRepository, currentUser)
    {
        _questionRepository = questionRepository;
        _answerRepository = answerRepository;
        _examQuestionRepository = examQuestionRepository;
    }
    public async Task<(List<QuestionDto> Questions, int TotalCount)> GetQuestionsOfSubjectAsync(long? subjectId, string searchName = "",
        int page = 1,
        int pageSize = 9,
        QuestionDifficulty? difficultyFilter = null,
        QuestionType? typeFilter = null)
    {
        var subjectIdquery = (await GetQueryableSubjectsForTeacherAsync()).Select(s => s.Id);
        if (subjectId.HasValue && !await subjectIdquery.ContainsAsync(subjectId.Value))
        {
            throw new AbpAuthorizationException("Bạn không có quyền truy cập môn học này.");
        }
        var query = (await _questionRepository.GetQueryableAsync())
            .Include(q => q.Answers)
            .Include(q => q.Subject)
            .Where(q => ((subjectId == null && subjectIdquery.Contains(q.SubjectId)) || (subjectId.HasValue && q.SubjectId == subjectId.Value))
                && (string.IsNullOrEmpty(searchName) || q.Title.Contains(searchName))
                && (!difficultyFilter.HasValue || q.Difficulty == difficultyFilter.Value)
                && (!typeFilter.HasValue || q.Type == typeFilter.Value))
            .OrderBy(q => q.CreationTime);
        var totalCount = await query.CountAsync();
        var questions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return (questions.Select(q => new QuestionDto
        {
            Id = q.Id,
            SubjectId = q.SubjectId,
            SubjectName = q.Subject?.Name ?? string.Empty,
            Title = q.Title,
            Difficulty = q.Difficulty,
            Type = q.Type,
            GeneralExplanation = q.GeneralExplanation,
            DefaultScore = q.DefaultScore,
            Order = q.Order,
            Answers = q.Answers.OrderBy(a => a.Order).Select(a => new AnswerDto
            {
                Id = a.Id,
                Text = a.Text,
                IsCorrect = a.IsCorrect,
                Order = a.Order
            }).ToList()
        }).ToList(), totalCount);
    }
    public async Task<(List<QuestionDto> Questions, int TotalCount)> GetAvailableQuestionsForExamAsync(long subjectId, long examId, string searchName = "",
        int page = 1,
        int pageSize = 10)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);

        var excludedQuestionIds = await (await _examQuestionRepository.GetQueryableAsync())
            .Where(eq => eq.ExamId == examId)
            .Select(eq => eq.QuestionId)
            .ToListAsync();

        var query = (await _questionRepository.GetQueryableAsync())
            .Include(q => q.Answers)
            .Where(q => q.SubjectId == subjectId
                && !excludedQuestionIds.Contains(q.Id)
                && (string.IsNullOrEmpty(searchName) || q.Title.Contains(searchName)))
            .OrderBy(q => q.CreationTime);

        var totalCount = await query.CountAsync();
        var questions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (questions.Select(q => new QuestionDto
        {
            Id = q.Id,
            SubjectId = q.SubjectId,
            SubjectName = q.Subject?.Name ?? string.Empty,
            Title = q.Title,
            Difficulty = q.Difficulty,
            Type = q.Type,
            GeneralExplanation = q.GeneralExplanation,
            DefaultScore = q.DefaultScore,
            Order = q.Order,
            Answers = q.Answers.OrderBy(a => a.Order).Select(a => new AnswerDto
            {
                Id = a.Id,
                Text = a.Text,
                IsCorrect = a.IsCorrect,
                Order = a.Order
            }).ToList()
        }).ToList(), totalCount);
    }

    public async Task<QuestionDto> GetQuestionDetailAsync(long subjectId, long questionId)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        return new QuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Difficulty = question.Difficulty,
            Type = question.Type,
            GeneralExplanation = question.GeneralExplanation,
            DefaultScore = question.DefaultScore,
            Order = question.Order,
        };
    }
    public async Task<QuestionDto> CreateQuestionAsync(long subjectId, CreateQuestionDto input)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        if (await _questionRepository.AnyAsync(q => q.SubjectId == subjectId && q.Title == input.Title))
        {
            throw new AbpAuthorizationException("Đã tồn tại câu hỏi với tiêu đề này trong môn học.");
        }
        if (input.DefaultScore < 0)
        {
            throw new AbpAuthorizationException("Điểm mặc định không thể âm.");
        }
        if (input.Order < 0)
        {
            throw new AbpAuthorizationException("Thứ tự không thể âm.");
        }
        if (input.Difficulty < QuestionDifficulty.Easy || input.Difficulty > QuestionDifficulty.Hard)
        {
            throw new AbpAuthorizationException("Độ khó không hợp lệ.");
        }
        if (input.Type < QuestionType.Single || input.Type > QuestionType.Multi)
        {
            throw new AbpAuthorizationException("Loại câu hỏi không hợp lệ.");
        }
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new AbpAuthorizationException("Tiêu đề câu hỏi không được để trống.");
        }
        var question = new Question
        {
            Title = input.Title.Trim(),
            Difficulty = input.Difficulty,
            Type = input.Type,
            GeneralExplanation = input.GeneralExplanation.Trim(),
            DefaultScore = input.DefaultScore,
            Order = input.Order,
            SubjectId = subjectId
        };
        await _questionRepository.InsertAsync(question, autoSave: true);

        // Insert answers
        var answerOrder = 0;
        foreach (var a in input.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Text)))
        {
            await _answerRepository.InsertAsync(new Answer
            {
                QuestionId = question.Id,
                Text = a.Text!.Trim(),
                IsCorrect = a.IsCorrect,
                Order = answerOrder++
            }, autoSave: true);
        }

        return new QuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Difficulty = question.Difficulty,
            Type = question.Type,
            GeneralExplanation = question.GeneralExplanation,
            DefaultScore = question.DefaultScore,
            Order = question.Order,
        };
    }
    public async Task<ImportQuestionResultDto> ImportQuestion(ImportQuestionDto importQuestionDto)
    {
        Subject subject = await FindSubjectForTeacherAsync(importQuestionDto.SubjectId);
        if (importQuestionDto.File == null || importQuestionDto.File.Length == 0)
        {
            throw new UserFriendlyException("Vui lòng chọn tệp Excel để nhập câu hỏi.");
        }
        using var memoryStream = new MemoryStream(importQuestionDto.File.GetAllBytes());
        if (!IsExcelStream(memoryStream))
        {
            throw new UserFriendlyException("Tệp được chọn không phải là tệp Excel hợp lệ.");
        }
        memoryStream.Position = 0;
        using var workbook = new XLWorkbook(memoryStream);
        var worksheet = workbook.Worksheet(1);

        var questionList = new List<Question>();
        var result = new ImportQuestionResultDto
        {
            TotalQuestions = 0,
            ImportedQuestions = 0,
            Errors = new List<string>()
        };
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            result.TotalQuestions++;
            var rowNumber = row.RowNumber();

            var question = row.Cell(1).GetString();
            var typeStr = row.Cell(2).GetString();
            if (string.IsNullOrWhiteSpace(question))
            {
                result.Errors.Add($"Dòng {rowNumber} không có tiêu đề câu hỏi.");
                continue;
            }
            var type = typeStr switch
            {
                "1 đáp án" => QuestionType.Single,
                "nhiều đáp án" => QuestionType.Multi,
                _ => (QuestionType?)null
            };
            if (type == null)
            {
                result.Errors.Add($"Dòng {rowNumber} có kiểu câu hỏi không hợp lệ.");
                continue;
            }
            var questionEntity = new Question
            {
                SubjectId = importQuestionDto.SubjectId,
                Type = type.Value,
                Difficulty = QuestionDifficulty.Medium, // Default difficulty, adjust as needed
                Title = question,
                DefaultScore = 1 // Default score, adjust as needed
            };
            var lastCell = row.LastCellUsed();
            if (lastCell == null || lastCell.Address.ColumnNumber < 3)
            {
                result.Errors.Add($"Dòng {rowNumber} không có đáp án nào.");
                continue;
            }

            var correctCount = row.Cells(3, lastCell.Address.ColumnNumber)
                .Count(c => IsCorrectAnswerCell(c));

            if (questionEntity.Type == QuestionType.Single && correctCount != 1)
            {
                result.Errors.Add($"Dòng {rowNumber} là câu hỏi loại 'Một đáp án đúng' nhưng có {correctCount} đáp án đúng.");
                continue;
            }

            if (correctCount == 0)
            {
                result.Errors.Add($"Dòng {rowNumber} không có đáp án đúng nào.");
                continue;
            }

            for (int col = 3; col <= lastCell.Address.ColumnNumber; col++)
            {
                var cell = row.Cell(col);

                if (cell.IsEmpty())
                    continue;

                var answerText = cell.GetString();
                if (string.IsNullOrWhiteSpace(answerText))
                {
                    result.Errors.Add($"Dòng {rowNumber}, cột {col} có đáp án trống.");
                    continue;
                }
                var answerEntity = new Answer
                {
                    Text = answerText,
                    IsCorrect = IsCorrectAnswerCell(cell),
                    Order = col - 3 // Assuming answers start from column 3
                };

                questionEntity.Answers.Add(answerEntity);
            }

            if (questionEntity.Answers.Count == 0)
            {
                result.Errors.Add($"Dòng {rowNumber} không có đáp án hợp lệ nào.");
                continue;
            }

            questionList.Add(questionEntity);
            result.ImportedQuestions++;
        }
        if (questionList.Count > 0)
        {
            await _questionRepository.InsertManyAsync(questionList, autoSave: true);
        }

        return result;
    }

    public static bool IsExcelStream(Stream stream)
    {
        if (!stream.CanRead || !stream.CanSeek)
        {
            return false;
        }

        var originalPosition = stream.Position;

        try
        {
            stream.Position = 0;
            var header = new byte[8];
            var bytesRead = stream.Read(header, 0, header.Length);
            if (bytesRead < 4)
            {
                return false;
            }

            if (HasXlsSignature(header, bytesRead))
            {
                return true;
            }

            if (!HasZipSignature(header, bytesRead))
            {
                return false;
            }

            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            return archive.Entries.Any(entry =>
                string.Equals(entry.FullName, "xl/workbook.xml", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
    private static bool HasXlsSignature(byte[] header, int bytesRead)
    {
        return bytesRead >= 8 &&
               header[0] == 0xD0 &&
               header[1] == 0xCF &&
               header[2] == 0x11 &&
               header[3] == 0xE0 &&
               header[4] == 0xA1 &&
               header[5] == 0xB1 &&
               header[6] == 0x1A &&
               header[7] == 0xE1;
    }

    private static bool HasZipSignature(byte[] header, int bytesRead)
    {
        if (bytesRead < 4)
        {
            return false;
        }

        return header[0] == 0x50 && header[1] == 0x4B && (
            (header[2] == 0x03 && header[3] == 0x04) || // Local file header
            (header[2] == 0x05 && header[3] == 0x06) || // End of central directory record
            (header[2] == 0x07 && header[3] == 0x08)    // Data descriptor
        );
    }

    private static bool IsCorrectAnswerCell(IXLCell cell)
    {
        var color = cell.Style.Fill.BackgroundColor;
        return color.ColorType == XLColorType.Color && (color.Color.ToArgb() != XLColor.NoColor.Color.ToArgb() || color.Color.ToArgb() != XLColor.White.Color.ToArgb());
    }

    public async Task<QuestionDto> UpdateQuestionAsync(long subjectId, long questionId, UpdateQuestionDto input)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        await EnsureQuestionIsNotInPublishedExamAsync(questionId);
        if (await _questionRepository.AnyAsync(q => q.SubjectId == subjectId && q.Title == input.Title && q.Id != questionId && q.Type == input.Type))
        {
            throw new AbpAuthorizationException("Đã tồn tại câu hỏi với tiêu đề và loại này trong môn học.");
        }
        if (input.DefaultScore < 0)
        {
            throw new AbpAuthorizationException("Điểm mặc định không thể âm.");
        }
        if (input.Order < 0)
        {
            throw new AbpAuthorizationException("Thứ tự không thể âm.");
        }
        if (input.Difficulty < QuestionDifficulty.Easy || input.Difficulty > QuestionDifficulty.Hard)
        {
            throw new AbpAuthorizationException("Độ khó không hợp lệ.");
        }
        if (input.Type < QuestionType.Single || input.Type > QuestionType.Multi)
        {
            throw new AbpAuthorizationException("Loại câu hỏi không hợp lệ.");
        }
        if (string.IsNullOrWhiteSpace(input.Title))
        {
            throw new AbpAuthorizationException("Tiêu đề câu hỏi không được để trống.");
        }
        question.Title = input.Title.Trim();
        question.Difficulty = input.Difficulty;
        question.Type = input.Type;
        question.GeneralExplanation = input.GeneralExplanation.Trim();
        question.DefaultScore = input.DefaultScore;
        question.Order = input.Order;
        await _questionRepository.UpdateAsync(question, autoSave: true);

        // Replace answers: delete old, insert new
        var existingAnswers = await _answerRepository.GetListAsync(a => a.QuestionId == questionId);
        foreach (var old in existingAnswers)
            await _answerRepository.DeleteAsync(old, autoSave: true);
        var answerOrder = 0;
        foreach (var a in input.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Text)))
        {
            await _answerRepository.InsertAsync(new Answer
            {
                QuestionId = questionId,
                Text = a.Text!.Trim(),
                IsCorrect = a.IsCorrect,
                Order = answerOrder++
            }, autoSave: true);
        }

        return new QuestionDto
        {
            Id = question.Id,
            Title = question.Title,
            Difficulty = question.Difficulty,
            Type = question.Type,
            GeneralExplanation = question.GeneralExplanation,
            DefaultScore = question.DefaultScore,
            Order = question.Order,
        };
    }
    public async Task<long> DeleteQuestionAsync(long subjectId, long questionId)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        await EnsureQuestionIsNotInPublishedExamAsync(questionId);
        await _questionRepository.DeleteAsync(question, autoSave: true);
        return question.Id;
    }

    private async Task EnsureQuestionIsNotInPublishedExamAsync(long questionId)
    {
        var questionIsPublished = await (await _examQuestionRepository.GetQueryableAsync())
            .AnyAsync(examQuestion => examQuestion.QuestionId == questionId
                && examQuestion.Exam != null
                && examQuestion.Exam.IsPublished);
        if (questionIsPublished)
        {
            throw new AbpAuthorizationException(
                "Không thể chỉnh sửa hoặc xóa câu hỏi đang nằm trong đề thi đã xuất bản. Hãy gỡ xuất bản đề thi trước.");
        }
    }

    public async Task<List<AnswerDto>> GetAnswersAsync(long subjectId, long questionId)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        var answers = await _answerRepository.GetListAsync(a => a.QuestionId == questionId);
        return answers.Select(a => new AnswerDto
        {
            Id = a.Id,
            Text = a.Text,
            IsCorrect = a.IsCorrect,
            Order = a.Order
        }).ToList();
    }
    public async Task<AnswerDto> CreateAnswerAsync(long subjectId, long questionId, CreateAnswerDto input)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Include(q => q.Answers)
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        if (string.IsNullOrWhiteSpace(input.Text))
        {
            throw new AbpAuthorizationException("Nội dung đáp án không được để trống.");
        }
        if (question.Type == QuestionType.Single)
        {
            if (input.IsCorrect && question.Answers.Any(a => a.IsCorrect))
            {
                throw new AbpAuthorizationException("Câu hỏi loại Single chỉ được có một đáp án đúng.");
            }
        }
        var answer = new Answer
        {
            Text = input.Text.Trim(),
            IsCorrect = input.IsCorrect,
            Order = input.Order,
            QuestionId = questionId
        };
        await _answerRepository.InsertAsync(answer, autoSave: true);
        return new AnswerDto
        {
            Id = answer.Id,
            Text = answer.Text,
            IsCorrect = answer.IsCorrect,
            Order = answer.Order
        };
    }
    public async Task<AnswerDto> UpdateAnswerAsync(long subjectId, long questionId, long answerId, UpdateAnswerDto input)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Include(q => q.Answers)
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        var answer = question.Answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy đáp án hoặc bạn không có quyền truy cập.");
        }
        if (string.IsNullOrWhiteSpace(input.Text))
        {
            throw new AbpAuthorizationException("Nội dung đáp án không được để trống.");
        }
        if (question.Type == QuestionType.Single)
        {
            if (input.IsCorrect && question.Answers.Any(a => a.IsCorrect && a.Id != answerId))
            {
                throw new AbpAuthorizationException("Câu hỏi loại Single chỉ được có một đáp án đúng.");
            }
        }
        answer.Text = input.Text.Trim();
        answer.IsCorrect = input.IsCorrect;
        answer.Order = input.Order;
        await _answerRepository.UpdateAsync(answer, autoSave: true);
        return new AnswerDto
        {
            Id = answer.Id,
            Text = answer.Text,
            IsCorrect = answer.IsCorrect,
            Order = answer.Order
        };
    }
    public async Task<long> DeleteAnswerAsync(long subjectId, long questionId, long answerId)
    {
        Subject subject = await FindSubjectForTeacherAsync(subjectId);
        var question = await (await _questionRepository.GetQueryableAsync())
            .Include(q => q.Answers)
            .Where(q => q.SubjectId == subjectId && q.Id == questionId)
            .FirstOrDefaultAsync();
        if (question == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy câu hỏi hoặc bạn không có quyền truy cập.");
        }
        var answer = question.Answers.FirstOrDefault(a => a.Id == answerId);
        if (answer == null)
        {
            throw new AbpAuthorizationException("Không tìm thấy đáp án hoặc bạn không có quyền truy cập.");
        }
        await _answerRepository.DeleteAsync(answer, autoSave: true);
        return answer.Id;
    }
}