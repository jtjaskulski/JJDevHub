namespace Learning.Domain
{
    public sealed record StudentProgress(string StudentId, Slug LessonSlug, DateTimeOffset CompletedAt)
    {
        public static StudentProgress Complete(string studentId, Slug lessonSlug, DateTimeOffset at)
        {
            return new StudentProgress(studentId, lessonSlug, at);
        }

        public StudentProgress Complete(DateTimeOffset at)
        {
            return this;
        }
    }
}