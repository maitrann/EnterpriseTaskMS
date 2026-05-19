namespace EnterpriseTask.Application.Common;

public sealed record OptionDto<T>(T Value, string Label, string? Helper = null);
