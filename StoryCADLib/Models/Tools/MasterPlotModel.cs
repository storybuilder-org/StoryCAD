namespace StoryCAD.Models.Tools;

public class MasterPlotModel
{
    public string MasterPlotName;
    public string MasterPlotNotes;
    public List<MasterPlotScene> MasterPlotScenes;


    public MasterPlotModel(string name)
    {
        MasterPlotName = name;
        MasterPlotNotes = string.Empty;
        MasterPlotScenes = new List<MasterPlotScene>();
    }
}