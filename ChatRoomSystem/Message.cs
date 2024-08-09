using System;
using Newtonsoft.Json;

public class Message
{
    public string Username { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }

    public Message(string username, string content)
    {
        Username = username;
        Content = content;
        Timestamp = DateTime.Now;
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this);
    }

    public static Message FromJson(string json)
    {
        return JsonConvert.DeserializeObject<Message>(json);
    }
}
