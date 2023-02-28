using System;
using System.IO;
using System.Text.Json;

public class MyClass
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Version { get; set; }
    public string Url { get; set; }

    public MyClass(string title, string description, string version, string url)
    {
        Title = title;
        Description = description;
        Version = version;
        Url = url;
    }

    // Serialization method
    public static void SerializeObject(MyClass obj, string filePath)
    {
        string jsonString = JsonSerializer.Serialize(obj);
        File.WriteAllText(filePath, jsonString);
    }

    // Deserialization method
    public static MyClass DeserializeObject(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<MyClass>(jsonString);
    }
}
