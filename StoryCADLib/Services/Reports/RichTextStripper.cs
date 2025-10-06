using System.Globalization;
using System.Text.RegularExpressions;

namespace StoryCAD.Services.Reports;

public class RichTextStripper
{
    // Recognize \* as a control symbol (ignorable destination)
    private static readonly Regex _rtfRegex = new(
        @"\\(?:
                  ([a-zA-Z]+)(-?\d*)\s? |   # Word with optional parameter
                  '([0-9a-fA-F]{2}) |       # Hex character
                  \*                        # Control symbol: ignorable destination
              )|
              ({|})|                        # Group start/end
              ([^\\{}]+)                    # Text characters, including newlines
            ",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

    private static readonly List<string> destinations = new()
    {
        "aftncn", "aftnsep", "aftnsepc", "annotation", "atnauthor", "atndate", "atnicn", "atnid",
        "atnparent", "atnref", "atntime", "atrfend", "atrfstart", "author", "background",
        "bkmkend", "bkmkstart", "blipuid", "buptim", "category", "colorschememapping",
        "colortbl", "comment", "company", "creatim", "datafield", "datastore", "defchp", "defpap",
        "do", "doccomm", "docvar", "dptxbxtext", "ebcend", "ebcstart", "factoidname", "falt",
        "fchars", "ffdeftext", "ffentrymcr", "ffexitmcr", "ffformat", "ffhelptext", "ffl",
        "ffname", "ffstattext", "field", "file", "filetbl", "fldinst", "fldrslt", "fldtype",
        "fname", "fontemb", "fontfile", "fonttbl", "footer", "footerf", "footerl", "footerr",
        "footnote", "formfield", "ftncn", "ftnsep", "ftnsepc", "g", "generator", "gridtbl",
        "header", "headerf", "headerl", "headerr", "hl", "hlfr", "hlinkbase", "hlloc", "hlsrc",
        "hsv", "htmltag", "info", "keycode", "keywords", "latentstyles", "lchars", "levelnumbers",
        "leveltext", "lfolevel", "linkval", "list", "listlevel", "listname", "listoverride",
        "listoverridetable", "listpicture", "liststylename", "listtable", "listtext",
        "lsdlockedexcept", "macc", "maccPr", "mailmerge", "maln", "malnScr", "manager", "margPr",
        "mbar", "mbarPr", "mbaseJc", "mbegChr", "mborderBox", "mborderBoxPr", "mbox", "mboxPr",
        "mchr", "mcount", "mctrlPr", "md", "mdeg", "mdegHide", "mden", "mdiff", "mdPr", "me",
        "mendChr", "meqArr", "meqArrPr", "mf", "mfName", "mfPr", "mfunc", "mfuncPr", "mgroupChr",
        "mgroupChrPr", "mgrow", "mhideBot", "mhideLeft", "mhideRight", "mhideTop", "mhtmltag",
        "mlim", "mlimloc", "mlimlow", "mlimlowPr", "mlimupp", "mlimuppPr", "mm", "mmaddfieldname",
        "mmath", "mmathPict", "mmathPr", "mmaxdist", "mmc", "mmcJc", "mmconnectstr",
        "mmconnectstrdata", "mmcPr", "mmcs", "mmdatasource", "mmheadersource", "mmmailsubject",
        "mmodso", "mmodsofilter", "mmodsofldmpdata", "mmodsomappedname", "mmodsoname",
        "mmodsorecipdata", "mmodsosort", "mmodsosrc", "mmodsotable", "mmodsoudl",
        "mmodsoudldata", "mmodsouniquetag", "mmPr", "mmquery", "mmr", "mnary", "mnaryPr",
        "mnoBreak", "mnum", "mobjDist", "moMath", "moMathPara", "moMathParaPr", "mopEmu",
        "mphant", "mphantPr", "mplcHide", "mpos", "mr", "mrad", "mradPr", "mrPr", "msepChr",
        "mshow", "mshp", "msPre", "msPrePr", "msSub", "msSubPr", "msSubSup", "msSubSupPr", "msSup",
        "msSupPr", "mstrikeBLTR", "mstrikeH", "mstrikeTLBR", "mstrikeV", "msub", "msubHide",
        "msup", "msupHide", "mtransp", "mtype", "mvertJc", "mvfmf", "mvfml", "mvtof", "mvtol",
        "mzeroAsc", "mzeroDesc", "mzeroWid", "nesttableprops", "nextfile", "nonesttables",
        "objalias", "objclass", "objdata", "object", "objname", "objsect", "objtime", "oldcprops",
        "oldpprops", "oldsprops", "oldtprops", "oleclsid", "operator", "panose", "password",
        "passwordhash", "pgp", "pgptbl", "picprop", "pict", "pn", "pnseclvl", "pntext", "pntxta",
        "pntxtb", "printim", "private", "propname", "protend", "protstart", "protusertbl", "pxe",
        "result", "revtbl", "revtim", "rsidtbl", "rxe", "shp", "shpgrp", "shpinst",
        "shppict", "shprslt", "shptxt", "sn", "sp", "staticval", "stylesheet", "subject", "sv",
        "svb", "tc", "template", "themedata", "title", "txe", "ud", "upr", "userprops",
        "wgrffmtfilter", "windowcaption", "writereservation", "writereservhash", "xe", "xform",
        "xmlattrname", "xmlattrvalue", "xmlclose", "xmlname", "xmlnstbl", "xmlopen"
    };

    private static readonly Dictionary<string, string> specialCharacters = new()
    {
        { "par", "\n" },
        { "line", "\n" },
        { "pard", "\n" },
        { "sect", "\n" },
        { "page", "\n" },
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

    public string StripRichTextFormat(string inputRtf)
    {
        if (inputRtf == null)
        {
            return null;
        }

        Stack<StackEntry> stack = new();
        var ignorable = false;
        var ucskip = 1;
        var curskip = 0;
        List<string> outList = new();

        var matches = _rtfRegex.Matches(inputRtf);
        if (matches.Count == 0)
        {
            return string.Empty;
        }

        foreach (Match match in matches)
        {
            // Handle \* control symbol
            if (match.Value == "\\*")
            {
                ignorable = true;
                continue;
            }

            var word = match.Groups[1].Value;
            var arg = match.Groups[2].Value;
            var hex = match.Groups[3].Value;
            var brace = match.Groups[4].Value;
            var tchar = match.Groups[5].Value;

            if (!string.IsNullOrEmpty(brace))
            {
                curskip = 0;
                if (brace == "{")
                {
                    stack.Push(new StackEntry(ucskip, ignorable));
                }
                else // "}"
                {
                    if (stack.Count > 0)
                    {
                        var entry = stack.Pop();
                        ucskip = entry.NumberOfCharactersToSkip;
                        ignorable = entry.Ignorable;
                    }
                    else
                    {
                        ucskip = 1;
                        ignorable = false;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(word))
            {
                curskip = 0;
                if (destinations.Contains(word))
                {
                    ignorable = true;
                }
                else if (ignorable)
                {
                    // ignore
                }
                else if (specialCharacters.TryGetValue(word, out var subst))
                {
                    outList.Add(subst);
                }
                else if (word == "uc")
                {
                    if (!string.IsNullOrEmpty(arg))
                    {
                        ucskip = int.Parse(arg);
                    }
                }
                else if (word == "u")
                {
                    if (!string.IsNullOrEmpty(arg))
                    {
                        var c = int.Parse(arg);
                        if (c < 0)
                        {
                            c += 0x10000;
                        }

                        outList.Add(char.ConvertFromUtf32(c));
                        curskip = ucskip;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(hex))
            {
                if (curskip > 0)
                {
                    curskip -= 1;
                }
                else if (!ignorable)
                {
                    var c = int.Parse(hex, NumberStyles.HexNumber);
                    outList.Add(char.ConvertFromUtf32(c));
                }
            }
            else if (!string.IsNullOrEmpty(tchar))
            {
                if (curskip > 0)
                {
                    curskip -= 1;
                }
                else if (!ignorable)
                {
                    tchar = tchar.Replace("\r\n", "\n").Replace("\r", "\n");
                    outList.Add(tchar);
                }
            }
        }

        return string.Concat(outList).Trim();
    }

    private class StackEntry
    {
        public StackEntry(int numberOfCharactersToSkip, bool ignorable)
        {
            NumberOfCharactersToSkip = numberOfCharactersToSkip;
            Ignorable = ignorable;
        }

        public int NumberOfCharactersToSkip { get; }
        public bool Ignorable { get; }
    }
}
