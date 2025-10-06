namespace StoryCAD.Models.Tools;

public class PlotPatternScene
{
    public string Notes;
    public string SceneTitle;

    public PlotPatternScene(string title)
    {
        SceneTitle = title;
        Notes = string.Empty;
    }
}
