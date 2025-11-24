using System;

namespace WearMate.Shared.Helpers;

public static class ImageHelper
{
    // Constants
    private static readonly string[] ALLOWED_FORMATS = { ".jpeg", ".jpg", ".png", ".webp" };
    private static readonly string[] ALLOWED_CONTENT_TYPES = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    public const int MAX_FILE_SIZE_MB = 5;

    private static string? _supabaseUrl;
    private static string? _bucket;
    private static string _defaultProductImage = "";
    private static string _defaultAvatarImage = "";

    /// <summary>
    /// Load default settings from environment variables
    /// Must be called on startup of each microservice or WearMate.Web
    /// </summary>
    public static void SetDefaults(string supabaseUrl, string bucket, string defaultProductImagePath, string defaultAvatarImagePath)
    {
        _supabaseUrl = supabaseUrl;
        _bucket = bucket;
        if (!string.IsNullOrWhiteSpace(defaultProductImagePath))
            _defaultProductImage = BuildSupabaseUrl(defaultProductImagePath);
        if (!string.IsNullOrWhiteSpace(defaultAvatarImagePath))
            _defaultAvatarImage = BuildSupabaseUrl(defaultAvatarImagePath);
    }
    /// <summary>
    /// Build full Supabase Storage public URL from bucket + path
    /// </summary>
    private static string BuildSupabaseUrl(string filePath)
    {
        if (string.IsNullOrWhiteSpace(_supabaseUrl))
            return filePath;
        return $"{_supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{_bucket}/{filePath.TrimStart('/')}";
    }

    /// <summary>
    /// Return product image or default fallback
    /// </summary>
    public static string GetProductImage(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl)
            ? _defaultProductImage
            : imageUrl;
    }

    /// <summary>
    /// Return avatar image or default fallback
    /// </summary>
    public static string GetAvatarImage(string? imageUrl)
    {
        return string.IsNullOrWhiteSpace(imageUrl)
            ? _defaultAvatarImage
            : imageUrl;
    }

    // --- Utility functions (unchanged) ---
    public static string GetSupabaseUrl(string supabaseUrl, string bucket, string path)
        => $"{supabaseUrl.TrimEnd('/')}/storage/v1/object/public/{bucket}/{path.TrimStart('/')}";

    /// <summary>
    /// Generate GUID-based unique filename
    /// </summary>
    public static string GenerateFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        var guid = Guid.NewGuid().ToString("N"); // 32-character hex string
        return $"{guid}{extension}";
    }

    /// <summary>
    /// Generate pure GUID filename with extension
    /// </summary>
    public static string GenerateGuidFileName(string extension)
    {
        if (!extension.StartsWith("."))
            extension = "." + extension;
        return $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    public static bool IsValidImage(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ALLOWED_FORMATS.Contains(ext);
    }

    /// <summary>
    /// Validate file format is in allowed list
    /// </summary>
    public static bool ValidateFileFormat(string fileName)
    {
        return IsValidImage(fileName);
    }

    /// <summary>
    /// Validate content type starts with "image/" and is in allowed types
    /// </summary>
    public static bool ValidateContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var lowerContentType = contentType.ToLowerInvariant();
        return lowerContentType.StartsWith("image/") && ALLOWED_CONTENT_TYPES.Contains(lowerContentType);
    }

    public static double GetFileSizeMB(long bytes)
        => Math.Round(bytes / 1024.0 / 1024.0, 2);

    public static bool IsValidSize(long bytes, int maxSizeMB = 5)
        => GetFileSizeMB(bytes) <= maxSizeMB;

    /// <summary>
    /// Extract storage path from full Supabase URL
    /// Example: https://xxx.supabase.co/storage/v1/object/public/bucket/path/file.jpg -> path/file.jpg
    /// </summary>
    public static string? ExtractStoragePath(string? fullUrl)
    {
        if (string.IsNullOrWhiteSpace(fullUrl))
            return null;

        try
        {
            // Pattern: /storage/v1/object/public/{bucket}/{path}
            var pattern = "/storage/v1/object/public/";
            var index = fullUrl.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
                return null;

            var afterPattern = fullUrl.Substring(index + pattern.Length);
            // Remove bucket name (first segment)
            var segments = afterPattern.Split('/', 2);
            return segments.Length > 1 ? segments[1] : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Build full Storage URL from path
    /// </summary>
    public static string BuildStorageUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_bucket))
            return path;

        return BuildSupabaseUrl(path);
    }
}
