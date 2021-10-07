
[System.AttributeUsage(System.AttributeTargets.Property)]  
public class EosAttribute : System.Attribute
{
    public string name { get; private set; }
    public object defaultValue { get; private set; }
  
    public EosAttribute(string name, object defaultValue)
    {
        this.name = name;
        this.defaultValue = defaultValue;
    }
}  
