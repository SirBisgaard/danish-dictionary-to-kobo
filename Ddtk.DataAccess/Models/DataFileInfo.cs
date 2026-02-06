namespace Ddtk.DataAccess.Models;

public struct DataFileInfo
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime? FileChangeDate { get; set; }
}