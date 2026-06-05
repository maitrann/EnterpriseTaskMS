namespace EnterpriseTask.Domain.Tasks;

public static class TaskProgressPolicy
{
    public static int Normalize(int progress)
    {
        return Math.Clamp(progress, 0, 100);
    }
}
