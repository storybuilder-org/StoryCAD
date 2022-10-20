using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
// ReSharper disable StringLiteralTypo

namespace StoryBuilder.Services.Reports;

/// <summary>
/// Rich Text Stripper
/// https://chrisbenard.net/2014/08/20/extract-text-from-rtf-in-c-net/
/// </summary>
/// <remarks>
/// Translated from Python located at:
/// http://stackoverflow.com/a/188877/448
/// </remarks>
public class RichTextStripper
{
    private class StackEntry
    {
        public int NumberOfCharactersToSkip { get; }
        public bool Ignorable { get; }

        public StackEntry(int numberOfCharactersToSkip, bool ignorable)
        {
            NumberOfCharactersToSkip = numberOfCharactersToSkip;
            Ignorable = ignorable;
        }
    }

    private static readonly Regex RtfRegex = new(@"\\([a-z]{1,32})(-?\d{1,10})?[ ]?|\\'([0-9a-f]{2})|\\([^a-z])|([{}])|[\r\n]+|(.)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private static readonly List<string> Destinations = new()
    {
        "aftncn","aftnsep","aftnsepc","annotation","atnauthor","atndate","atnicn","atnid",
        "atnparent","atnref","atntime","atrfend","atrfstart","author","background",
        "bkmkend","bkmkstart","blipuid","buptim","category","colorschememapping",
        "colortbl","comment","company","creatim","datafield","datastore","defchp","defpap",
        "do","doccomm","docvar","dptxbxtext","ebcend","ebcstart","factoidname","falt",
        "fchars","ffdeftext","ffentrymcr","ffexitmcr","ffformat","ffhelptext","ffl",
        "ffname","ffstattext","field","file","filetbl","fldinst","fldrslt","fldtype",
        "fname","fontemb","fontfile","fonttbl","footer","footerf","footerl","footerr",
        "footnote","formfield","ftncn","ftnsep","ftnsepc","g","generator","gridtbl",
        "header","headerf","headerl","headerr","hl","hlfr","hlinkbase","hlloc","hlsrc",
        "hsv","htmltag","info","keycode","keywords","latentstyles","lchars","levelnumbers",
        "leveltext","lfolevel","linkval","list","listlevel","listname","listoverride",
        "listoverridetable","listpicture","liststylename","listtable","listtext",
        "lsdlockedexcept","macc","maccPr","mailmerge","maln","malnScr","manager","margPr",
        "mbar","mbarPr","mbaseJc","mbegChr","mborderBox","mborderBoxPr","mbox","mboxPr",
        "mchr","mcount","mctrlPr","md","mdeg","mdegHide","mden","mdiff","mdPr","me",
        "mendChr","meqArr","meqArrPr","mf","mfName","mfPr","mfunc","mfuncPr","mgroupChr",
        "mgroupChrPr","mgrow","mhideBot","mhideLeft","mhideRight","mhideTop","mhtmltag",
        "mlim","mlimloc","mlimlow","mlimlowPr","mlimupp","mlimuppPr","mm","mmaddfieldname",
        "mmath","mmathPict","mmathPr","mmaxdist","mmc","mmcJc","mmconnectstr",
        "mmconnectstrdata","mmcPr","mmcs","mmdatasource","mmheadersource","mmmailsubject",
        "mmodso","mmodsofilter","mmodsofldmpdata","mmodsomappedname","mmodsoname",
        "mmodsorecipdata","mmodsosort","mmodsosrc","mmodsotable","mmodsoudl",
        "mmodsoudldata","mmodsouniquetag","mmPr","mmquery","mmr","mnary","mnaryPr",
        "mnoBreak","mnum","mobjDist","moMath","moMathPara","moMathParaPr","mopEmu",
        "mphant","mphantPr","mplcHide","mpos","mr","mrad","mradPr","mrPr","msepChr",
        "mshow","mshp","msPre","msPrePr","msSub","msSubPr","msSubSup","msSubSupPr","msSup",
        "msSupPr","mstrikeBLTR","mstrikeH","mstrikeTLBR","mstrikeV","msub","msubHide",
        "msup","msupHide","mtransp","mtype","mvertJc","mvfmf","mvfml","mvtof","mvtol",
        "mzeroAsc","mzeroDesc","mzeroWid","nesttableprops","nextfile","nonesttables",
        "objalias","objclass","objdata","object","objname","objsect","objtime","oldcprops",
        "oldpprops","oldsprops","oldtprops","oleclsid","operator","panose","password",
        "passwordhash","pgp","pgptbl","picprop","pict","pn","pnseclvl","pntext","pntxta",
        "pntxtb","printim","private","propname","protend","protstart","protusertbl","pxe",
        "result","revtbl","revtim","rsidtbl","rxe","shp","shpgrp","shpinst",
        "shppict","shprslt","shptxt","sn","sp","staticval","stylesheet","subject","sv",
        "svb","tc","template","themedata","title","txe","ud","upr","userprops",
        "wgrffmtfilter","windowcaption","writereservation","writereservhash","xe","xform",
        "xmlattrname","xmlattrvalue","xmlclose","xmlname","xmlnstbl",
        "xmlopen"
    };

    private static readonly Dictionary<string, string> SpecialCharacters = new()
    {
        { "par", "\n" },
        { "sect", "\n\n" },
        { "page", "\n\n" },
        { "line", "\n" },
        { "tab", "\t" },
        { "emdash", "\u2014" },
        { "endash", "\u2013" },
        { "emspace", "\u2003" },
        { "enspace", "\u2002" },
        { "qmspace", "\u2005" },
        { "bullet", "\u2022" },
        { "lquote", "\u2018" },
        { "rquote", "\u2019" },
        { "ldblquote", "\u201C" },
        { "rdblquote", "\u201D" }
    };
    /// <summary>
    /// Strip RTF Tags from RTF Text
    /// </summary>
    /// <param name="inputRtf">RTF formatted text</param>
    /// <returns>Plain text from RTF</returns>
    public string StripRichTextFormat(string inputRtf)
    {
        if (inputRtf == null)
        {
            return null;
        }

        Stack<StackEntry> _Stack = new();
        bool _Ignorable = false;              // Whether this group (and all inside it) are "ignorable".
        int _Ucskip = 1;                      // Number of ASCII characters to skip after a unicode character.
        int _Curskip = 0;                     // Number of ASCII characters left to skip
        List<string> _OutList = new();    // Output buffer.

        MatchCollection _Matches = RtfRegex.Matches(inputRtf);

        if (_Matches.Count > 0)
        {
            foreach (Match _Match in _Matches)
            {
                string _Word = _Match.Groups[1].Value;
                string _Arg = _Match.Groups[2].Value;
                string _Hex = _Match.Groups[3].Value;
                string _Character = _Match.Groups[4].Value;
                string _Brace = _Match.Groups[5].Value;
                string _Tchar = _Match.Groups[6].Value;

                if (!string.IsNullOrEmpty(_Brace))
                {
                    _Curskip = 0;
                    switch (_Brace)
                    {
                        case "{":
                            // Push state
                            _Stack.Push(new StackEntry(_Ucskip, _Ignorable));
                            break;
                        case "}":
                        {
                            // Pop state
                            StackEntry _Entry = _Stack.Pop();
                            _Ucskip = _Entry.NumberOfCharactersToSkip;
                            _Ignorable = _Entry.Ignorable;
                            break;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(_Character)) // \x (not a letter)
                {
                    _Curskip = 0;
                    if (_Character == "~")
                    {
                        if (!_Ignorable)
                        {
                            _OutList.Add("\xA0");
                        }
                    }
                    else if ("{}\\".Contains(_Character))
                    {
                        if (!_Ignorable)
                        {
                            _OutList.Add(_Character);
                        }
                    }
                    else if (_Character == "*")
                    {
                        _Ignorable = true;
                    }
                }
                else if (!string.IsNullOrEmpty(_Word)) // \foo
                {
                    _Curskip = 0;
                    if (Destinations.Contains(_Word))
                    {
                        _Ignorable = true;
                    }
                    else if (_Ignorable)
                    {
                    }
                    else if (SpecialCharacters.ContainsKey(_Word))
                    {
                        _OutList.Add(SpecialCharacters[_Word]);
                    }
                    else if (_Word == "uc")
                    {
                        _Ucskip = int.Parse(_Arg);
                    }
                    else if (_Word == "u")
                    {
                        int _C = int.Parse(_Arg);
                        if (_C < 0)
                        {
                            _C += 0x10000;
                        }
                        _OutList.Add(char.ConvertFromUtf32(_C));
                        _Curskip = _Ucskip;
                    }
                }
                else if (!string.IsNullOrEmpty(_Hex)) // \'xx
                {
                    if (_Curskip > 0)
                    {
                        _Curskip -= 1;
                    }
                    else if (!_Ignorable)
                    {
                        int _C = int.Parse(_Hex, NumberStyles.HexNumber);
                        _OutList.Add(char.ConvertFromUtf32(_C));
                    }
                }
                else if (!string.IsNullOrEmpty(_Tchar))
                {
                    if (_Curskip > 0)
                    {
                        _Curskip -= 1;
                    }
                    else if (!_Ignorable)
                    {
                        _OutList.Add(_Tchar);
                    }
                }
            }
        }

        string _ReturnString = string.Join(string.Empty, _OutList.ToArray());

        // MakeStringUnicodeCompatible(ref returnString);
        return _ReturnString;
    }
}