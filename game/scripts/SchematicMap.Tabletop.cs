using Godot;

namespace LinesOfSupport.Game;

public partial class SchematicMap
{
    internal TabletopMap3D? Tabletop{get;private set;}
    private string? tabletopSignature;
    private void RefreshTabletop()
    {
        if(Data is null || Tiles.Count==0)return;
        var signature=Data.Id+string.Join(";",Tiles.Select(t=>$"{t.SiteId}:{t.CapabilityId}:{t.Status}:{t.TodayPermille}"))+string.Join(";",Day?.Links.Select(l=>$"{l.Id}:{l.Open}")??[])+string.Join(";",LostConfidenceSites.Order());
        if(signature==tabletopSignature)return;
        Vector3? pan=Tabletop?.Pan;float? size=Tabletop?.CameraSize;
        if(Tabletop is not null){RemoveChild(Tabletop);Tabletop.QueueFree();}
        Tabletop=new TabletopMap3D{Size=GetViewportRect().Size};AddChild(Tabletop);
        Tabletop.Configure(Data,Tiles,Day?.Links??[],MapFont,LostConfidenceSites);Tabletop.Select(SelectedSite);Tabletop.ShowFacts(Day,Math.Clamp(animationSeconds/5f,0,1));
        if(pan is not null&&size is not null)Tabletop.Restore(pan.Value,size.Value);
        tabletopSignature=signature;
    }
}
