namespace Ddtk.Cli.Helpers;

public class WindowDataHelper
{
    public string ToFileSize(long fileSizeInBytes)
    {
        if (fileSizeInBytes == 0)
        {
            return "N/A";
        }
        
        var sizeKb = fileSizeInBytes / 1024.0;
        var sizeMb = fileSizeInBytes / 1024.0 / 1024.0;
        var sizeGb = fileSizeInBytes / 1024.0 / 1024.0 / 1024.0;
        
        var sizeText = 
            sizeGb >= 1.0 ? 
                $"{sizeGb:F2} GB" : 
                sizeMb >= 1.0 ? 
                    $"{sizeMb:F2} MB" : 
                    $"{sizeKb:F2} KB";
        
        return sizeText;
    }

    public string ToDateTime(DateTime? dateTime)
    {
        if (dateTime is null || dateTime.Value == default)
        {
            return "N/A";
        }
        
        return $"{dateTime.Value:yyyy-MM-dd hh:mm:ss}";
    }

    public string ToTimeSince(DateTime? from, DateTime? to)
    {
        if (from is null || to is null)
        {
            return "Never";
        }
        
        var elapsed = (from - to).Value;
        string timeAgo;
        if (elapsed.TotalMinutes < 1)
        {
            timeAgo = "just now";
        }
        else if (elapsed.TotalHours < 1)
        {
            timeAgo = $"{(int)elapsed.TotalMinutes}m ago";
        }
        else if (elapsed.TotalDays < 1)
        {
            timeAgo = $"{(int)elapsed.TotalHours}h ago";
        }
        else if (elapsed.TotalDays < 365)
        {
            timeAgo = $"{(int)elapsed.TotalDays}d ago";
        }
        else
        {
            timeAgo = $"{(int)elapsed.TotalDays / 365}y ago";
        }
        
        
        return timeAgo;
    }
}