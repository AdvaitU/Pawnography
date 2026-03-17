using System.Text.RegularExpressions;

public static class ExtensionMethods
{

    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    // To Title Case Methods for strings

    public static string ToTitleCase(string str)
    {
        string spaced = Regex.Replace(str, "([a-z])([A-Z])", "$1 $2");                    // 1. Insert a space before each uppercase letter
        return $"{char.ToUpper(spaced[0]) + spaced.Substring(1)}";                        // 2. Capitalize the first letter of the entire string
    }
    public static string ToTitleCase(CardSubCategory subCat)
    {
        string subCategoryName = subCat.ToString();

        if (string.IsNullOrEmpty(subCategoryName) || subCat == CardSubCategory.None) return "???";
        string spaced = Regex.Replace(subCategoryName, "([a-z])([A-Z])", "$1 $2");                    // 1. Insert a space before each uppercase letter
        return $"{char.ToUpper(spaced[0]) + spaced.Substring(1)}s";                                   // 2. Capitalize the first letter of the entire string
    }

    public static string ToTitleCase(FreelancerType type)
    {
        string subCategoryName = type.ToString();

        if (string.IsNullOrEmpty(subCategoryName) || type == FreelancerType.None) return "???";
        string spaced = Regex.Replace(subCategoryName, "([a-z])([A-Z])", "$1 $2");                    // 1. Insert a space before each uppercase letter
        return $"{char.ToUpper(spaced[0]) + spaced.Substring(1)}s";                                   // 2. Capitalize the first letter of the entire string
    }

}