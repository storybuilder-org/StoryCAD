﻿using StoryCAD.Models.Tools;
using System.Reflection;

namespace StoryCAD.DAL;

public class ControlLoader
{
    private IList<string> _lines;
    public async Task<List<object>> Init()
    {
        try
        {
            await using Stream internalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StoryCADLib.Assets.Install.Controls.ini");
            using StreamReader reader = new(internalResourceStream);

            _lines = (await reader.ReadToEndAsync()).Split("\n");
        }
        catch (Exception _ex) 
        {
            Console.WriteLine(_ex.Message);
        }
        // Populate UserControl data source collections
        List<object> Controls = new() {
            LoadConflictTypes(),
            LoadRelationTypes()
        };
        Clear();
        return Controls;
    }

    public SortedDictionary<string, ConflictCategoryModel> LoadConflictTypes()
    {
        ConflictCategoryModel _currentConflictType = null;
        SortedDictionary<string, ConflictCategoryModel> _conflictTypes = new();
        string _currentSubtype = string.Empty;

        string _section = string.Empty;
        string _keyword = string.Empty;
        string _keyvalue = string.Empty;
        foreach (string _line in _lines)
        {
            ParseLine(_line, ref _section, ref _keyword, ref _keyvalue);
            //   Process the parsed values
            switch (_section)
            {
                case "Conflict Types":
                    switch (_keyword)
                    {
                        case "":
                            break;
                        case "Type":
                            _currentConflictType = new ConflictCategoryModel(_keyvalue);
                            _conflictTypes.Add(_keyvalue, _currentConflictType);
                            break;
                        case "Subtype":
                            _currentSubtype = _keyvalue;
                            _currentConflictType!.SubCategories.Add(_keyvalue);
                            _currentConflictType.Examples.Add(_currentSubtype, new List<string>());
                            break;
                        case "Example":
                            _currentConflictType!.Examples[_currentSubtype].Add(_keyvalue);
                            break;
                    }
                    break;
            }
        }
        return _conflictTypes;
    }

    public List<string> LoadRelationTypes()
    {
        List<string> _relationships = new();

        string _section = string.Empty;
        string _keyword = string.Empty;
        string _keyvalue = string.Empty;
        foreach (string _line in _lines)
        {
            ParseLine(_line, ref _section, ref _keyword, ref _keyvalue);
            //   Process the parsed values
            switch (_section)
            {
                case "RelationTypes":
                    switch (_keyword)
                    {
                        case "":
                            break;
                        case "RelationType":
                            _relationships.Add(_keyvalue);
                            break;
                    }
                    break;
            }
        }
        return _relationships;
    }

    /// <summary>
    /// Parse a line from the Controls.ini file into section, keyword, and keyvalue.
    /// 
    /// Parsed tokens are passed by reference and left unchanged if not found in
    /// the parse. So for example, section is not modified if parsing a 
    /// keyword=keyvalue line.
    /// </summary>
    /// <param name="line">The line to be parsed</param>
    /// <param name="section">The [section] section name, if present</param>
    /// <param name="keyword">The keyword=keyvalue keyword parameter, if present</param>
    /// <param name="keyvalue">The </param>
    private static void ParseLine(string line, ref string section, ref string keyword, ref string keyvalue)
    {
        line = line.TrimEnd();
        if (line.Equals(string.Empty))
        {
            keyword = string.Empty;
            keyvalue = string.Empty;
            return;
        }
        if (line.StartsWith(";")) // Comment
            return;
        if (line.StartsWith("["))
        {
            string[] _tokens = line.Split('[', ']');
            section = _tokens[1];
            keyword = "$SECTION$";
            keyvalue = string.Empty;
            return;
        }
        if (line.Contains('='))
        {
            string[] _tokens = line.Split(new[] { '=' });
            keyword = _tokens[0];
            keyvalue = _tokens[1].TrimEnd();
            return;
        }
        if (line.StartsWith("="))
        {
            keyword = string.Empty;
            keyvalue = line[1..].TrimEnd();
        }

    }

    public void Clear() {  _lines = null; }
}