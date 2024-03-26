﻿// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using AssignedNumbersCrawler;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

string numbersDirectory = args[0];
string targetDirectory = args[1];

await using var adTypesWriter = new StreamWriter(File.Create($"{targetDirectory}/AssignedNumbers/AdTypes.cs"));
await adTypesWriter.WriteAsync(ReadAdTypes(numbersDirectory));
await using var companyIdentifierWriter = new StreamWriter(File.Create($"{targetDirectory}/AssignedNumbers/CompanyIdentifiers.cs"));
await companyIdentifierWriter.WriteAsync(ReadCompanyIdentifiers(numbersDirectory));
return;

string ReadAdTypes(string inputDir) => ReadFile<AdType, byte>(
    $@"{inputDir}\assigned_numbers\core\ad_types.yaml",
    "ad_types", (value, i) =>
    {
        string name = FixNaming(value.Name, i);
        return $"""
                    /// <summary> {FixXmlDocNaming(value.Name)} </summary>
                    /// <remarks> {FixXmlDocNaming(value.Reference)} </remarks>
                    {name} = {value.Value},
                """;
    });

string ReadCompanyIdentifiers(string inputDir) => ReadFile<CompanyIdentifier, ushort>(
    $@"{inputDir}\assigned_numbers\company_identifiers\company_identifiers.yaml",
    "company_identifiers", (value, i) =>
    {
        string name = FixNaming(value.Name, i);
        return $"""
                    /// <summary> {FixXmlDocNaming(value.Name)} </summary>
                    {name} = {value.Value},
                """;
    });

string ReadFile<T, TEnum>(string path, string key, Func<T, int, string> func)
    where T : INameable
    where TEnum : INumber<TEnum>
{
    string enumName = ToPascalCase(key);
    string file = File.ReadAllText(path);

    IDeserializer deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    var yamlDictionary = deserializer.Deserialize<Dictionary<string, List<T>>>(file);
    List<T> values = yamlDictionary[key];

    var builder = new StringBuilder();
    string enumType = typeof(TEnum).Name switch
    {
        "Byte" => "byte",
        "UInt16" => "ushort",
        var t => t
    };
    builder.AppendLine(CultureInfo.InvariantCulture, $$"""
                                                       namespace Darp.Ble.Data.AssignedNumbers;

                                                       /// <summary>
                                                       /// The {{enumName}}.
                                                       /// This enum was autogenerated on {{DateTimeOffset.UtcNow:d}}
                                                       /// </summary>
                                                       [System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "InconsistentNaming")]
                                                       public enum {{enumName}} : {{enumType}}
                                                       {
                                                       """);

    Dictionary<string, int> nameDict = new(StringComparer.Ordinal);
    foreach (T value in values)
    {
        if (!nameDict.TryAdd(value.Name, 1))
            nameDict[value.Name]++;
        string newLine = func(value, nameDict[value.Name]);

        builder.AppendLine(newLine);
    }
    builder.AppendLine("}");
    return builder.ToString();
}

string FixXmlDocNaming(string original)
{
    string result = original.Replace("&","&amp;");
    return result;
}

string FixNaming(string original, int i)
{
    if (original.Length == 0) throw new Exception($"Value has an empty name");
    string res = original[0] switch
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
    string result = ToPascalCase(res + original[1..]);
    result = result.Replace("UuiD", "Uuid");
    if (i > 1)
        result = $"{result}{i}";
    return result;
}

string ToPascalCase(string original)
{
    var invalidCharsRgx = new Regex("[^_a-zA-Z0-9]", RegexOptions.None, TimeSpan.FromSeconds(1));
    var whiteSpace = new Regex(@"(?<=\s)", RegexOptions.None, TimeSpan.FromSeconds(1));
    var startsWithLowerCaseChar = new Regex("^[a-z]", RegexOptions.None, TimeSpan.FromSeconds(1));
    var firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$", RegexOptions.None, TimeSpan.FromSeconds(1));
    var lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]", RegexOptions.None, TimeSpan.FromSeconds(1));
    var upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

    // replace white spaces with underscore, then replace all invalid chars with empty string
    IEnumerable<string> pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(original, "_"), string.Empty)
        // split by underscores
        .Split('_', StringSplitOptions.RemoveEmptyEntries)
        // set first letter to uppercase
        .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpperInvariant()))
        // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
        .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLowerInvariant()))
        // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
        .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpperInvariant()))
        // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
        .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLowerInvariant()));

    return string.Concat(pascalCase);
}