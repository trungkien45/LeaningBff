using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

using System.IO.Compression;
using LearningBff.Data;
using LearningBff.Entities;
using LearningBff.Services.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LearningBff.Services;

public class QuestionAppService : LearningBffAppService
{
    private readonly LearningBffDbContext _db;

    public QuestionAppService(LearningBffDbContext db)
    {
        _db = db;
    }

    public async Task<List<QuestionDto>> GetListAsync(long? subjectId = null, QuestionType? type = null)
    {
        var query = _db.Questions.AsNoTracking()
            .Include(q => q.Answers)
            .Include(q => q.Subject)
            .AsQueryable();

        if (subjectId.HasValue)
            query = query.Where(q => q.SubjectId == subjectId.Value);
        if (type.HasValue)
            query = query.Where(q => q.Type == type.Value);

        var questions = await query.OrderBy(q => q.SubjectId).ThenBy(q => q.Order).ToListAsync();
        return questions.Select(MapToDto).ToList();
    }

    public async Task<PagedResultDto<QuestionDto>> GetPageAsync(
        long? subjectId, QuestionType? type, int skipCount, int maxResultCount)
    {
        var query = _db.Questions.AsNoTracking()
            .Include(question => question.Answers)
            .Include(question => question.Subject)
            .AsQueryable();

        if (subjectId.HasValue)
            query = query.Where(question => question.SubjectId == subjectId.Value);
        if (type.HasValue)
            query = query.Where(question => question.Type == type.Value);

        var totalCount = await query.CountAsync();
        var questions = await query
            .OrderBy(question => question.SubjectId)
            .ThenBy(question => question.Order)
            .ThenBy(question => question.Id)
            .Skip(Math.Max(0, skipCount))
            .Take(Math.Clamp(maxResultCount, 1, 100))
            .ToListAsync();

        return new PagedResultDto<QuestionDto>(totalCount, questions.Select(MapToDto).ToList());
    }

    public async Task<QuestionDto> GetAsync(long id)
    {
        var q = await _db.Questions
            .AsNoTracking()
            .Include(x => x.Answers)
            .Include(x => x.Subject)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy câu hỏi ID={id}");

        return MapToDto(q);
    }
    public async Task<ImportQuestionResultDto> ImportQuestion(ImportQuestionDto importQuestionDto)
    {
        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == importQuestionDto.SubjectId);
        if (!subjectExists)
            throw new UserFriendlyException($"Không tìm thấy môn học ID={importQuestionDto.SubjectId}");

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
        int i = 0;
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            result.TotalQuestions++;

            var question = row.Cell(1).GetString();
            var typeStr = row.Cell(2).GetString();
            if (string.IsNullOrWhiteSpace(question))
            {
                result.Errors.Add($"Dòng {i + 1} không có tiêu đề câu hỏi.");
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
                result.Errors.Add($"Dòng {i + 1} có kiểu câu hỏi không hợp lệ.");
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
                result.Errors.Add($"Dòng {i + 1} không có đáp án nào.");
                continue;
            }

            var correctCount = row.Cells(3, lastCell.Address.ColumnNumber)
                .Count(c => IsCorrectAnswerCell(c));

            if (questionEntity.Type == QuestionType.Single && correctCount != 1)
            {
                result.Errors.Add($"Dòng {i + 1} là câu hỏi loại 'Một đáp án đúng' nhưng có {correctCount} đáp án đúng.");
                continue;
            }

            if (correctCount == 0)
            {
                result.Errors.Add($"Dòng {i + 1} không có đáp án đúng nào.");
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
                    result.Errors.Add($"Dòng {i + 1}, cột {col} có đáp án trống.");
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
                result.Errors.Add($"Dòng {i + 1} không có đáp án hợp lệ nào.");
                continue;
            }

            questionList.Add(questionEntity);
            result.ImportedQuestions++;
            i++;
        }
        await _db.Questions.AddRangeAsync(questionList);
        await _db.SaveChangesAsync();
        return result;
    }

    private string GetFirstSheetNameOrThrow(Stream stream)
    {
        try
        {
            return GetFirstSheetNameOrThrow(stream);
        }
        catch (InvalidDataException ex) when (ex.Message == "Excel file does not contain any worksheet.")
        {
            throw new UserFriendlyException("Tệp Excel không chứa bất kỳ trang tính nào.");
        }
        catch
        {
            throw new UserFriendlyException("Tệp được chọn không phải là tệp Excel hợp lệ.");
        }
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

    public async Task<QuestionDto> CreateAsync(CreateQuestionDto input)
    {
        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == input.SubjectId);
        if (!subjectExists)
            throw new UserFriendlyException($"Không tìm thấy môn học ID={input.SubjectId}");
        var correctCount = input.Answers.Count(a => a.IsCorrect);

        if (input.Type == QuestionType.Single)
        {
            if (correctCount != 1)
                throw new UserFriendlyException("Câu hỏi loại 'Một đáp án đúng' phải có chính xác 1 đáp án đúng.");
        }
        if (correctCount == 0)
            throw new UserFriendlyException("Câu hỏi phải có ít nhất 1 đáp án đúng.");
        if (input.Answers.Any(x => string.IsNullOrEmpty(x.Text)))
        {
            throw new UserFriendlyException("Không thể lưu câu hỏi vì có đáp án trống. Vui lòng điền đầy đủ nội dung đáp án hoặc xóa đáp án trống.");
        }

        var question = new Question(input.Title, input.Type, input.SubjectId, input.Difficulty, input.DefaultScore)
        {
            GeneralExplanation = input.GeneralExplanation
        };

        foreach (var a in input.Answers)
            question.Answers.Add(new Answer(a.Text, a.IsCorrect, 0, a.Order, a.Explanation));

        _db.Questions.Add(question);
        await _db.SaveChangesAsync();
        return await GetAsync(question.Id);
    }

    public async Task<QuestionDto> UpdateAsync(long id, UpdateQuestionDto input)
    {
        var question = await _db.Questions
            .Include(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new UserFriendlyException($"Không tìm thấy câu hỏi ID={id}");
        var correctCount = input.Answers.Count(a => a.IsCorrect);

        if (input.Type == QuestionType.Single)
        {
            if (correctCount != 1)
                throw new UserFriendlyException("Câu hỏi loại 'Một đáp án đúng' phải có chính xác 1 đáp án đúng.");
        }
        if (correctCount == 0)
            throw new UserFriendlyException("Câu hỏi phải có ít nhất 1 đáp án đúng.");
        if (input.Answers.Any(x => string.IsNullOrEmpty(x.Text)))
        {
            throw new UserFriendlyException("Không thể lưu câu hỏi vì có đáp án trống. Vui lòng điền đầy đủ nội dung đáp án hoặc xóa đáp án trống.");
        }
        question.Title = input.Title;
        question.Type = input.Type;
        question.SubjectId = input.SubjectId;
        question.Difficulty = input.Difficulty;
        question.DefaultScore = input.DefaultScore;
        question.GeneralExplanation = input.GeneralExplanation;

        // Smart in-place update for answers to preserve existing Answer IDs referenced by ExamResults
        var incomingAnswerIds = input.Answers.Where(a => a.Id > 0).Select(a => a.Id).ToHashSet();

        // 1. Check if any removed answers are referenced in results
        var answersToRemove = question.Answers.Where(a => !incomingAnswerIds.Contains(a.Id)).ToList();
        foreach (var oldAnswer in answersToRemove)
        {
            var isUsedInResults = await _db.ExamResultAnswers.AnyAsync(ra => ra.AnswerId == oldAnswer.Id);
            if (isUsedInResults)
            {
                throw new UserFriendlyException($"Không thể xóa đáp án '{oldAnswer.Text}' vì đã có bài thi ghi nhận đáp án này. Bạn có thể sửa nội dung thay vì xóa.");
            }
            _db.Answers.Remove(oldAnswer);
            question.Answers.Remove(oldAnswer);
        }

        // 2. Update existing answers or add new ones
        foreach (var a in input.Answers)
        {
            if (a.Id > 0)
            {
                var existing = question.Answers.FirstOrDefault(x => x.Id == a.Id);
                if (existing != null)
                {
                    existing.Text = a.Text;
                    existing.IsCorrect = a.IsCorrect;
                    existing.Explanation = a.Explanation;
                    existing.Order = a.Order;
                    continue;
                }
            }

            // New answer
            question.Answers.Add(new Answer(a.Text, a.IsCorrect, question.Id, a.Order, a.Explanation));
        }

        await _db.SaveChangesAsync();
        return await GetAsync(question.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var question = await _db.Questions.FindAsync(id)
            ?? throw new UserFriendlyException($"Không tìm thấy câu hỏi ID={id}");

        var isUsedInExam = await _db.ExamQuestions.AnyAsync(eq => eq.QuestionId == id);
        if (isUsedInExam)
        {
            throw new UserFriendlyException("Không thể xóa câu hỏi này vì đã được đưa vào đề thi.");
        }

        var isUsedInResults = await _db.ExamResultAnswers.AnyAsync(ra => ra.QuestionId == id);
        if (isUsedInResults)
        {
            throw new UserFriendlyException("Không thể xóa câu hỏi này vì đã có kết quả thi liên quan.");
        }

        _db.Questions.Remove(question);
        await _db.SaveChangesAsync();
    }

    private static QuestionDto MapToDto(Question q) => new()
    {
        Id = q.Id,
        Title = q.Title,
        Type = q.Type,
        Difficulty = q.Difficulty,
        GeneralExplanation = q.GeneralExplanation,
        DefaultScore = q.DefaultScore,
        Order = q.Order,
        SubjectId = q.SubjectId,
        SubjectName = q.Subject?.Name ?? string.Empty,
        Answers = q.Answers.OrderBy(a => a.Order).Select(a => new AnswerDto
        {
            Id = a.Id,
            Text = a.Text,
            IsCorrect = a.IsCorrect,
            Explanation = a.Explanation,
            Order = a.Order
        }).ToList()
    };
}
