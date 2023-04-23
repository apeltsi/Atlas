using System.Reflection;

namespace SolidCode.Atlas.Telescope;
public class ECSElement
{
    public string name { get; set; }
    public ECSComponent[] components { get; set; }

    public ECSElement[] children { get; set; }

    public ECSElement(string name, ECSComponent[] components, ECSElement[] children)
    {
        this.name = name;
        this.components = components;
        this.children = children;
    }
}

public class ECSComponent
{
    public string name { get; set; }
    public ECSComponentField[] fields { get; set; }

    public ECSComponent(object c)
    {
        this.name = c.GetType().Name;
        List<ECSComponentField> fields = new List<ECSComponentField>();
        for (int i = 0; i < c.GetType().GetFields().Length; i++)
        {
            FieldInfo field = c.GetType().GetFields()[i];
            if (Attribute.IsDefined(field, typeof(HideInInspector)))
            {
                continue;
            }
            object? fieldValue = field.GetValue(c);

            if (fieldValue == null || fieldValue.ToString() == null)
            {
                fields.Add(new ECSComponentField(field.Name, "Null", field.FieldType.ToString()));
            }
            else
            {
                string? fieldValueStr = fieldValue.ToString();
                if (fieldValueStr != null)
                {
                    fields.Add(new ECSComponentField(field.Name, fieldValueStr, field.FieldType.ToString()));
                }
                else
                {
                    fields.Add(new ECSComponentField(field.Name, "Null", field.FieldType.ToString()));
                }
            }
        }
        this.fields = fields.ToArray();
    }
}

public class ECSComponentField
{
    public string name { get; set; }
    public string value { get; set; }
    public string type { get; set; }

    public ECSComponentField(string name, string value, string type)
    {
        this.name = name;
        this.value = value;
        this.type = type;
    }
}