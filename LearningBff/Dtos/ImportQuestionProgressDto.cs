namespace LearningBff.Dtos;

public sealed record ImportQuestionProgressDto(
    string Stage,
    int ProcessedRows,
    int TotalRows,
    int ImportedQuestions,
    int ErrorCount,
    int CurrentRow);
