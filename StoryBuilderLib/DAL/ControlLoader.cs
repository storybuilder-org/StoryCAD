using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

namespace StoryBuilder.DAL;

public class ControlLoader
{
    private IList<string> _lines;
    public async Task Init(string path)
    {
        try
        {
            StorageFolder _ToolFolder = await StorageFolder.GetFolderFromPathAsync(path);
            StorageFile _IniFile = await _ToolFolder.GetFileAsync("Controls.ini");
            _lines = await FileIO.ReadLinesAsync(_IniFile, UnicodeEncoding.Utf8);
        }
        catch (Exception _Ex) 
        {
            Console.WriteLine(_Ex.Message);
        }
        // Populate UserControl data source collections
        GlobalData.ConflictTypes = LoadConflictTypes();
        GlobalData.RelationTypes = LoadRelationTypes();
    }

    public SortedDictionary<string, ConflictCategoryModel> LoadConflictTypes()
    {
        ConflictCategoryModel _CurrentConflictType = null;
        SortedDictionary<string, ConflictCategoryModel> _ConflictTypes = new();
        string _CurrentSubtype = string.Empty;

        string _Section = string.Empty;
        string _Keyword = string.Empty;
        string _Keyvalue = string.Empty;
        foreach (string _Line in _lines)
        {
            ParseLine(_Line, ref _Section, ref _Keyword, ref _Keyvalue);
            //   Process the parsed values
            switch (_Section)
            {
                case "Conflict Types":
                    switch (_Keyword)
                    {
                        case "":
                            break;
                        case "Type":
                            _CurrentConflictType = new ConflictCategoryModel(_Keyvalue);
                            _ConflictTypes.Add(_Keyvalue, _CurrentConflictType);
                            break;
                        case "Subtype":
                            _CurrentSubtype = _Keyvalue;
                            _CurrentConflictType!.SubCategories.Add(_Keyvalue);
                            _CurrentConflictType.Examples.Add(_CurrentSubtype, new List<string>());
                            break;
                        case "Example":
                            _CurrentConflictType!.Examples[_CurrentSubtype].Add(_Keyvalue);
                            break;
                    }
                    break;
            }
        }
        return _ConflictTypes;
    }

    public List<string> LoadRelationTypes()
    {
        List<string> _Relationships = new();

        string _Section = string.Empty;
        string _Keyword = string.Empty;
        string _Keyvalue = string.Empty;
        foreach (string _Line in _lines)
        {
            ParseLine(_Line, ref _Section, ref _Keyword, ref _Keyvalue);
            //   Process the parsed values
            switch (_Section)
            {
                case "RelationTypes":
                    switch (_Keyword)
                    {
                        case "":
                            break;
                        case "RelationType":
                            _Relationships.Add(_Keyvalue);
                            break;
                    }
                    break;
            }
        }
        return _Relationships;
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
            string[] _Tokens = line.Split('[', ']');
            section = _Tokens[1];
            keyword = "$SECTION$";
            keyvalue = string.Empty;
            return;
        }
        if (line.Contains('='))
        {
            string[] _Tokens = line.Split(new[] { '=' });
            keyword = _Tokens[0];
            keyvalue = _Tokens[1].TrimEnd();
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