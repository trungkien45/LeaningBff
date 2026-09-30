namespace LearningBff.Permissions;

public static class LearningBffPermissions
{
    public const string GroupName = "LearningBff";

    public static class Subjects
    {
        public const string Default = GroupName + ".Subjects";
        public const string Create = Default + ".Create";
        public const string Edit   = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Questions
    {
        public const string Default = GroupName + ".Questions";
        public const string Create = Default + ".Create";
        public const string Edit   = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }

    public static class Exams
    {
        public const string Default = GroupName + ".Exams";
        public const string Create = Default + ".Create";
        public const string Edit   = Default + ".Edit";
        public const string Delete = Default + ".Delete";
    }
}
