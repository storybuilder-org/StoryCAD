using System.Collections.ObjectModel;
using System.Reflection;

namespace StoryCAD.DAL;

public class ListLoader
{
    #region Public Methods

    public async Task<Dictionary<string, ObservableCollection<string>>> Init()
    {
        Dictionary<string, ObservableCollection<string>> _lists = new();

        await using Stream internalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StoryCADLib.Assets.Install.Lists.ini");
        using StreamReader reader = new(internalResourceStream);

        // Read the Application .INI file. Each record is the format 'KeyWord=Keyvalue'.
        // As each record is read, it's moved to the corresponding initialization
        // structure field or loaded as an initialization value for a control
        string _text = await reader.ReadToEndAsync();
        StringReader _sr = new(_text);
        // ReSharper disable once MoveVariableDeclarationInsideLoopCondition
        string _line; //Not Inlining to keep code readability
        while ((_line = await _sr.ReadLineAsync()) != null)
        {
            _line = _line.TrimEnd();
            if (_line.Equals(string.Empty))
                continue;
            if (_line.StartsWith(";")) // Comment
                continue;
            if (_line.Contains("="))
            {
                string[] _tokens = _line.Split(new[] { '=' });
                string _keyword = _tokens[0];
                string _keyvalue = _tokens[1];
                if (!_lists.ContainsKey(_keyword))
                    _lists.Add(_keyword, new ObservableCollection<string>());
                _lists[_keyword].Add(_keyvalue);
            }
        }
        return _lists;
    }
    #endregion
}