namespace StoryCAD.Models.Tools;

public class PlotPatternModel
{
    public string PlotPatternName;
    public string PlotPatternNotes;
    public List<PlotPatternScene> PlotPatternScenes;


    public PlotPatternModel(string name)
    {
        PlotPatternName = name;
        PlotPatternNotes = string.Empty;
        PlotPatternScenes = new List<PlotPatternScene>();
    }
}