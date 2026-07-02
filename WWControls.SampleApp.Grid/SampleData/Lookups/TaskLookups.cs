namespace WWControls.SampleApp.Grid.SampleData.Lookups
{
    internal static class TaskLookups
    {
        public static readonly string[] TaskStatuses =
            { "Backlog", "In Progress", "In Review", "Blocked", "Done" };

        public static readonly string[] Departments =
            { "Engineering", "Product", "Design", "QA", "DevOps", "Support" };

        public static readonly string[] Assignees =
            { "Alice", "Bob", "Carol", "Dan", "Erin", "Frank", "Grace", "Henry" };

        public static readonly (int Id, string Name)[] Priorities =
        {
            (1, "Low"), (2, "Normal"), (3, "High"), (4, "Urgent")
        };

        public static readonly string[] TaskTitles =
        {
            "Draft proposal", "Review pull request", "Schedule kickoff", "Update documentation",
            "Deploy to staging", "QA regression pass", "Triage bug backlog", "Prepare release notes",
            "Run accessibility audit", "Refactor data layer", "Sync with stakeholders",
            "Investigate flaky test", "Write integration tests", "Onboard new contractor",
            "Reset staging environment", "Profile slow query", "Plan next iteration",
            "Pair on auth flow", "Validate migration script", "Demo prototype"
        };
    }
}
