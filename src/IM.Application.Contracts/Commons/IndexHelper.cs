namespace IM.Commons;

public class IndexHelper
{
    public static string GetIndex(string name)
    {
        var firstChar = char.ToUpperInvariant(name[0]);
        if (firstChar >= 'A' && firstChar <= 'Z')
        {
            return firstChar.ToString();
        }
        return "#";
    }
}