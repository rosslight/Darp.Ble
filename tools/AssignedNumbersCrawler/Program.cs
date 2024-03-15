// See https://aka.ms/new-console-template for more information

using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

Console.WriteLine("Hello, World!");

string file = File.ReadAllText("C:\\Users\\OleRosskamp\\RiderProjects\\Darp.Ble\\AssignedNumbersCrawler\\public\\assigned_numbers\\core\\ad_types.yaml");

IDeserializer deserializer = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .Build();

var yamlDictionary = deserializer.Deserialize<Dictionary<string, List<AdType>>>(file);
List<AdType> adTypes = yamlDictionary["ad_types"];

var builder = new StringBuilder();
builder.AppendLine("""
                   namespace Darp.Ble.Data;

                   /// <summary> The Ad types </summary>
                   public enum AdType : byte
                   {
                   """);

foreach (AdType adType in adTypes)
{
    string name = adType.Name;
    if (name.Length == 0) continue;
    string res = name[0] switch
    {
        '0' => "Zero",
        '1' => "One",
        '2' => "Two",
        '3' => "Three",
        '4' => "Four",
        '5' => "Five",
        '6' => "Six",
        '7' => "Seven",
        '8' => "Eight",
        '9' => "Nine",
        var s => s.ToString()
    };
    name = ToPascalCase(res + name[1..]);
    name = name.Replace("UuiD", "Uuid");
    builder.AppendLine($"""
                            /// <summary> {adType.Name} </summary>
                            /// <remarks> {adType.Reference} </remarks>
                            {name} = {adType.Value},
                        """);
}
builder.AppendLine("}");

var result = builder.ToString();
Console.WriteLine(result);


string ToPascalCase(string original)
{
    Regex invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
    Regex whiteSpace = new Regex(@"(?<=\s)");
    Regex startsWithLowerCaseChar = new Regex("^[a-z]");
    Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
    Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
    Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

    // replace white spaces with undescore, then replace all invalid chars with empty string
    var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)
        // split by underscores
        .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
        // set first letter to uppercase
        .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
        // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
        .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
        // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
        .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
        // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
        .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

    return string.Concat(pascalCase);
}

public sealed class AdType
{
    public required string Value { get; init; }
    public required string Name { get; init; }
    public required string Reference { get; init; }
}