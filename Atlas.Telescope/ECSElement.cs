namespace SolidCode.Atlas.Telescope;

public class ECSElement
{
    public ECSElement(string name, ECSComponent[] components, ECSElement[] children)
    {
        this.name = name;
        this.components = components;
        this.children = children;
    }

    public string name { get; set; }
    public ECSComponent[] components { get; set; }

    public ECSElement[] children { get; set; }
}

public class ECSComponent
{
    public ECSComponent(object c)
    {
        name = c.GetType().Name;
        var fields = new List<ECSComponentField>();
        for (var i = 0; i < c.GetType().GetFields().Length; i++)
        {
            var field = c.GetType().GetFields()[i];
            if (Attribute.IsDefined(field, typeof(HideInInspector))) continue;
            var fieldValue = field.GetValue(c);

            if (fieldValue == null || fieldValue.ToString() == null)
            {
                fields.Add(new ECSComponentField(field.Name, "Null", field.FieldType.ToString()));
            }
            else
            {
                var fieldValueStr = fieldValue.ToString();
                if (fieldValueStr != null)
                    fields.Add(new ECSComponentField(field.Name, fieldValueStr, field.FieldType.ToString()));
                else
                    fields.Add(new ECSComponentField(field.Name, "Null", field.FieldType.ToString()));
            }
        }

        this.fields = fields.ToArray();
    }

    public string name { get; set; }
    public ECSComponentField[] fields { get; set; }
}

public class ECSComponentField
{
    public ECSComponentField(string name, string value, string type)
    {
        this.name = name;
        this.value = value;
        this.type = type;
    }

    public string name { get; set; }
    public string value { get; set; }
    public string type { get; set; }
}