using Godot;

namespace LinesOfSupport.Game;

internal static class TabletopTheme
{
    internal static readonly Color Background=new("e9e3d8"), Paper=new("faf5ea"), Ink=new("343d4b"), Muted=new("66716d"),
        Moss=new("556e65"), Outline=new("b8b29f"), Alert=new("9f4937"), Warning=new("7a5a22"), Success=new("355d48");
    internal static FontFile Font(bool bold=false)
    {
        var path=Path.Combine(GamePaths.AssetRoot,"art-studies","fonts",bold?"AtkinsonHyperlegible-Bold.ttf":"AtkinsonHyperlegible-Regular.ttf");
        var font=new FontFile();
        if(font.LoadDynamicFont(path)!=Error.Ok) throw new IOException("The packaged Tabletop font is missing: "+Path.GetFileName(path));
        return font;
    }
    internal static StyleBoxFlat Box(Color color,int padding=8,int radius=6)
        =>new(){BgColor=color,BorderColor=Outline,BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,
            ContentMarginLeft=padding,ContentMarginRight=padding,ContentMarginTop=padding,ContentMarginBottom=padding,
            CornerRadiusTopLeft=radius,CornerRadiusTopRight=radius,CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius};
    internal static Theme Create()
    {
        var regular=Font(); var bold=Font(true); var theme=new Theme{DefaultFont=regular,DefaultFontSize=18};
        theme.SetTypeVariation("TabletopHeading","Label");theme.SetFont("font","TabletopHeading",bold);theme.SetFontSize("font_size","TabletopHeading",24);
        foreach(var type in new[]{"Label","Button","OptionButton","CheckBox","LineEdit","TextEdit","PopupMenu","Tree","ItemList","TabContainer","Window","TooltipLabel"})
        {
            theme.SetColor("font_color",type,Ink);theme.SetColor("font_hover_color",type,Ink);theme.SetColor("font_pressed_color",type,Ink);
            theme.SetColor("font_focus_color",type,Ink);theme.SetColor("font_disabled_color",type,Muted);theme.SetColor("font_selected_color",type,Paper);
        }
        theme.SetFont("normal_font","RichTextLabel",regular);theme.SetFont("bold_font","RichTextLabel",bold);theme.SetColor("default_color","RichTextLabel",Ink);
        foreach(var type in new[]{"Panel","PanelContainer","PopupPanel","PopupMenu","Tree","ItemList","TooltipPanel"}) theme.SetStylebox("panel",type,Box(Paper,12));
        foreach(var type in new[]{"Button","OptionButton"})
        {
            theme.SetStylebox("normal",type,Box(Paper,8));theme.SetStylebox("hover",type,Box(new Color("e3e5d7"),8));
            theme.SetStylebox("pressed",type,Box(new Color("dfe2d2"),8));theme.SetStylebox("disabled",type,Box(Background,8));
            var focus=Box(Colors.Transparent,0);focus.BorderColor=Moss;focus.SetBorderWidthAll(2);theme.SetStylebox("focus",type,focus);
        }
        theme.SetTypeVariation("TabletopPrimary","Button");
        foreach(var name in new[]{"normal","pressed","hover","hover_pressed"}) theme.SetStylebox(name,"TabletopPrimary",Box(Moss,10));
        foreach(var name in new[]{"font_color","font_hover_color","font_focus_color","font_pressed_color","font_hover_pressed_color"})theme.SetColor(name,"TabletopPrimary",Paper);
        foreach(var type in new[]{"LineEdit","TextEdit"})
        {
            theme.SetStylebox("normal",type,Box(new Color("fffdf5"),6));theme.SetStylebox("read_only",type,Box(Background,6));
            theme.SetStylebox("focus",type,Box(Colors.Transparent,0));theme.SetColor("caret_color",type,Ink);theme.SetColor("selection_color",type,Moss);
        }
        theme.SetStylebox("panel","TabContainer",Box(Paper,10));theme.SetStylebox("tab_selected","TabContainer",Box(Paper,8));
        theme.SetStylebox("tab_unselected","TabContainer",Box(Background,8));theme.SetStylebox("tab_hovered","TabContainer",Box(new Color("e3e5d7"),8));
        theme.SetStylebox("tab_disabled","TabContainer",Box(Background,8));
        theme.SetColor("font_selected_color","TabContainer",Ink);theme.SetColor("font_unselected_color","TabContainer",Muted);
        foreach(var type in new[]{"HScrollBar","VScrollBar"})
        {
            theme.SetStylebox("scroll",type,Box(Background,0,3));theme.SetStylebox("grabber",type,Box(Moss,0,3));
            theme.SetStylebox("grabber_highlight",type,Box(Moss.Lightened(.12f),0,3));theme.SetStylebox("grabber_pressed",type,Box(Ink,0,3));
        }
        theme.SetStylebox("background","ProgressBar",Box(new Color("d6d0bf"),0,3));theme.SetStylebox("fill","ProgressBar",Box(Moss,0,3));
        theme.SetStylebox("hover","PopupMenu",Box(new Color("e3e5d7"),4));
        foreach(var type in new[]{"Tree","ItemList"}){theme.SetStylebox("selected",type,Box(Moss,4));theme.SetStylebox("selected_focus",type,Box(Moss,4));}
        theme.SetStylebox("embedded_border","Window",Box(Paper,12));theme.SetColor("title_color","Window",Ink);
        foreach(var type in new[]{"VBoxContainer","HBoxContainer"})theme.SetConstant("separation",type,10);
        foreach(var state in new[]{false,true})
        {
            var icon=CheckIcon(state);
            theme.SetIcon(state?"checked":"unchecked","CheckBox",icon);theme.SetIcon(state?"checked_disabled":"unchecked_disabled","CheckBox",icon);
            theme.SetIcon(state?"radio_checked":"radio_unchecked","CheckBox",icon);
        }
        var arrow=Arrow();theme.SetIcon("arrow","OptionButton",arrow);
        theme.SetIcon("updown","SpinBox",UpDown());
        foreach(var name in new[]{"increment","increment_highlight","decrement","decrement_highlight"})
            theme.SetIcon(name,"TabContainer",Chevron(name.StartsWith("increment")));
        return theme;
    }
    private static Texture2D CheckIcon(bool selected)
    {
        using var image=Image.CreateEmpty(22,22,false,Image.Format.Rgba8);image.Fill(Colors.Transparent);
        image.FillRect(new Rect2I(2,2,18,18),Moss);image.FillRect(new Rect2I(4,4,14,14),Paper);
        if(selected)
        {
            for(var i=0;i<4;i++)image.FillRect(new Rect2I(6+i,10+i,2,2),Ink);
            for(var i=0;i<7;i++)image.FillRect(new Rect2I(9+i,13-i,2,2),Ink);
        }
        return ImageTexture.CreateFromImage(image);
    }
    private static Texture2D Arrow()
    {
        using var image=Image.CreateEmpty(14,14,false,Image.Format.Rgba8);image.Fill(Colors.Transparent);
        for(var i=0;i<5;i++){image.SetPixel(2+i,4+i,Ink);image.SetPixel(10-i,4+i,Ink);}
        return ImageTexture.CreateFromImage(image);
    }
    private static Texture2D Chevron(bool right)
    {
        using var image=Image.CreateEmpty(14,14,false,Image.Format.Rgba8);image.Fill(Colors.Transparent);
        for(var i=0;i<5;i++){var x=right?4+i:9-i;image.FillRect(new Rect2I(x,2+i,2,2),Ink);image.FillRect(new Rect2I(x,10-i,2,2),Ink);}
        return ImageTexture.CreateFromImage(image);
    }
    private static Texture2D UpDown()
    {
        using var image=Image.CreateEmpty(16,26,false,Image.Format.Rgba8);image.Fill(Colors.Transparent);
        for(var i=0;i<5;i++){image.FillRect(new Rect2I(3+i,8-i,2,2),Ink);image.FillRect(new Rect2I(11-i,8-i,2,2),Ink);
            image.FillRect(new Rect2I(3+i,17+i,2,2),Ink);image.FillRect(new Rect2I(11-i,17+i,2,2),Ink);}
        return ImageTexture.CreateFromImage(image);
    }
}
