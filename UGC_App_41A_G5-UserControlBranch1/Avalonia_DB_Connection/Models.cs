namespace Avalonia_DB_Connection;

public class Models
{
    //This is dedicated as a DataModel for our table in Users.axaml
    public int id { get; set; }
    public string firstname { get; set; } = string.Empty;
    public string lastname { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}