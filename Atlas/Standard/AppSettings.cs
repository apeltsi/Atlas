using System.Numerics;
using System.Text;

namespace SolidCode.Atlas.Standard;

public abstract class AppSettings
{
    private static List<string> _instances = new ();
    protected AppSettings()
    {
        lock (_instances)
        {
            if (_instances.Contains(this.GetType().Name))
            {
                throw new InvalidAppSettingsException("Multiple instances of AppSettings with the same name cannot exist at the same time. Please use a single instance of each AppSettings type or multiple instances with different names.");
            }
             _instances.Add(this.GetType().Name);
            Load();   
        }
    }

    public void Load()
    {
        // Lets load the file first 
        var data = AppStorage.Load("settings/" + this.GetType().Name + ".asettings");
        if (data.Length == 0) return;
        string dataString = Encoding.UTF8.GetString(data);

        string[] strvalues = dataString.Split('\n');
        if (strvalues[0] != "VERSION: 1")
        {
            throw new InvalidAppSettingsException("Unable to parse AppSettings. Invalid version; the file might have been created with a newer version of Atlas AppStorage or the file might be corrupt.");
        }
        // Now lets load the data as a dictionary of strings
        Dictionary<string, string> dataDictionary = new Dictionary<string, string>();
        foreach (var value in strvalues)
        {
            // Because some strings may contain the ":" character we'll have to be careful
            // We'll split the string at the first ":" character
            int index = value.IndexOf(':');
            if (index > 0)
            {
                string key = value.Substring(0, index);
                string dataValue = value.Substring(index + 1);
                dataDictionary.Add(key, dataValue);
            }
        }
        // Now lets map the data to the fields on this object
        var fields = this.GetType().GetFields();
        foreach (var field in fields)
        {
            if (!Attribute.IsDefined(field, typeof(ExcludeFromSettingsAttribute)))
            {
                if (dataDictionary.TryGetValue(field.Name, out string? dataValue))
                {
                    field.SetValue(this, ParseValue(field.FieldType, dataValue));
                }
            }
        }
        
        // And now for the properties
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (!Attribute.IsDefined(property, typeof(ExcludeFromSettingsAttribute)))
            {
                if (dataDictionary.TryGetValue(property.Name, out string? dataValue))
                {
                    property.SetValue(this, ParseValue(property.PropertyType, dataValue));
                }
            }
        }
        
    }
    
    public void Save()
    {
        // First of all, we'll have to serialize the all the fields on this object
        var fields = this.GetType().GetFields();
        Dictionary<string, string> data = new Dictionary<string, string>();
        data.Add("VERSION", "1");
        foreach (var field in fields)
        {
            if (!Attribute.IsDefined(field, typeof(ExcludeFromSettingsAttribute)))
            {
                var value = field.GetValue(this);
                string? serialized = GetSerialized(value);
                if (serialized != null)
                {
                    data.Add(field.Name, serialized);
                }
            }
        }
        
        // Now lets repeat the same process for properties
        var properties = this.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (!Attribute.IsDefined(property, typeof(ExcludeFromSettingsAttribute)))
            {
                var value = property.GetValue(this);
                string? serialized = GetSerialized(value);
                if (serialized != null)
                {
                    data.Add(property.Name, serialized);
                }
            }
        }

        // Now we'll have to save the data to a file
        
        AppStorage.Save("settings/" + this.GetType().Name + ".asettings", Encoding.UTF8.GetBytes(string.Join('\n', data.Select(x => $"{x.Key}: {x.Value}"))));
    }

    private static string? GetSerialized(object value)
    {
        switch (value)
        {
            case string s1:
                return '"' + s1.Replace("\n", "\\n") + '"';
                break;
            case int i1:
                return i1.ToString();
                break;
            case float f1:
                return f1.ToString();
                break;
            case bool b1:
                return b1.ToString();
                break;
            case Vector2 v1:
                return $"{v1.X},{v1.Y}";
                break;
            default:
                Debug.Warning($"Unsupported type '{value.GetType()}'. Add [ExcludeFromSettings] to ignore this field/property or use a different type to represent your data.");
                break;
        }

        return null;
    }

    private static object? ParseValue(Type type, string value)
    {
        try
        {
            switch (type)
            {
                case Type t when t == typeof(string):
                    // Our string are encoded like this: "what ever text\nanother line"
                    // So we'll have to remove our " at the start and end
                    // And we'll have to replace \n with a new line
                    return value.Substring(1, value.Length - 2).Replace("\\n", "\n");
                    break;
                case Type t when t == typeof(int):
                    return int.Parse(value);
                    break;
                case Type t when t == typeof(float):
                    return float.Parse(value);
                    break;
                case Type t when t == typeof(bool):
                    return bool.Parse(value);
                    break;
                case Type t when t == typeof(Vector2):
                    string[] values = value.Split(',');
                    return new Vector2(float.Parse(values[0]), float.Parse(values[1]));
                    break;
                default:
                    Debug.Warning(
                        $"Unsupported type '{type}'. Add [ExcludeFromSettings] to ignore this field/property or use a different type to represent your data.");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Error("Couldn't parse value: " + e.Message);
        }
        

        return null;
    }
    
    ~AppSettings()
    {
        _instances.Remove(this.GetType().Name);
    }
}
[AttributeUsage(AttributeTargets.Field)]
/// The field will not be serialized and saved
public class ExcludeFromSettingsAttribute : Attribute
{
    
}

public class InvalidAppSettingsException : Exception
{
    public InvalidAppSettingsException(string message) : base(message)
    {
    }
}