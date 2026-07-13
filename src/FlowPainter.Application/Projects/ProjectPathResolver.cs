namespace FlowPainter.Application.Projects;

public static class ProjectPathResolver
{
    public static string ResolveSourcePath(string projectPath, string sourceReference)
    {
        ValidatePath(projectPath, nameof(projectPath));
        ValidatePath(sourceReference, nameof(sourceReference));

        if (Path.IsPathRooted(sourceReference))
        {
            return Path.GetFullPath(sourceReference);
        }

        string fullProjectPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(fullProjectPath)
            ?? throw new ArgumentException("The project path must include a directory.", nameof(projectPath));
        return Path.GetFullPath(Path.Combine(projectDirectory, sourceReference));
    }

    public static string CreateSourceReference(string projectPath, string sourcePath)
    {
        ValidatePath(projectPath, nameof(projectPath));
        ValidatePath(sourcePath, nameof(sourcePath));

        string fullProjectPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(fullProjectPath)
            ?? throw new ArgumentException("The project path must include a directory.", nameof(projectPath));
        string fullSourcePath = Path.GetFullPath(sourcePath);
        string relativePath = Path.GetRelativePath(projectDirectory, fullSourcePath);

        return Path.IsPathRooted(relativePath)
            ? fullSourcePath
            : relativePath;
    }

    private static void ValidatePath(string path, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A non-empty path is required.", parameterName);
        }
    }
}
