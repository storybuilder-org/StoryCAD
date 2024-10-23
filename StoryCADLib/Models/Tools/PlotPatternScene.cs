namespace StoryCAD.Models.Tools;

public class PlotPatternScene
{
    public string SceneTitle;
    public string Notes;

    public PlotPatternScene(string title)
    {
        SceneTitle = title;
        Notes = string.Empty;
    }
}