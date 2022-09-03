using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.DAL;

public class ListLoader
{
    #region Public Methods

    public async Task<Dictionary<string, ObservableCollection<string>>> Init(string path)
    {
        Dictionary<string, ObservableCollection<string>> _lists = new();

        StorageFolder _controlFolder = await StorageFolder.GetFolderFromPathAsync(path);
        StorageFile _iniFile = await _controlFolder.GetFileAsync("Lists.ini");
        //See if the .INI file exists
        //if (!File.Exists(iniFile))
        //    throw new Exception(@"STORY.INI initialization file not found");

        // Read the Application .INI file. Each record is the format 'KeyWord=Keyvalue'.
        // As each record is read, it's moved to the corresponding initialization
        // structure field or loaded as an initialization value for a control
        string _text = await FileIO.ReadTextAsync(_iniFile);
        StringReader _sr = new(_text);
        string _line;
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