namespace LearningBff.Entities;

public enum QuestionType
{
    Single = 1,
    Multi  = 2
}

public enum QuestionDifficulty
{
    Easy   = 1,
    Medium = 2,
    Hard   = 3
}

public enum ExamResultStatus
{
    InProgress = 1,
    Submitted  = 2,
    TimedOut   = 3,
    Cancelled  = 4,
    Expired    = 5
}
