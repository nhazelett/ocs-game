using Godot;

namespace LinesOfSupport.Game;

internal static class TabletopTheme
{
    internal static int Direction { get; private set; } = 2;
    internal static Art.ArtStudyStyle Style => Art.ArtStudyStyle.All[Direction];
    internal static void Select(int index) => Direction = index is >= 0 and < 3 ? index : 2;
    internal static Color Background => new(Style.Background);
    internal static Color Paper => new(Style.Panel);
    internal static Color Ink => new(Style.Ink);
    internal static Color Muted => new(Style.Muted);
    internal static Color Moss => new(Style.Accent);
    internal static Color Outline => Direction == 2 ? new("b8b29f") : Muted;
    internal static Color Alert => new(Style.Alert);
    internal static Color Warning => Direction == 1 ? new("efd17e") : new("7a5a22");
    internal static Color Success => Direction == 1 ? new("96d8a9") : new("355d48");
    internal static Color Hover => Direction == 2 ? new("e3e5d7") : Paper.Lerp(Moss, .15f);
    internal static Color Pressed => Direction == 2 ? new("dfe2d2") : Paper.Lerp(Moss, .25f);
    internal static Color Input => Direction == 2 ? new("fffdf5") : Paper;
    internal static Color Track => Direction == 2 ? new("d6d0bf") : Background;
    internal static Color PrimaryInk => Direction == 1 ? Background : Paper;
    internal static Font Font(bool bold=false) => Art.ArtStudyStyle.LoadFont(bold ? Style.HeadingFont : Style.Font);
    internal static Color[] ColorsByRole() => [Background, Paper, Ink, Muted, Moss, Outline, Alert, Warning, Success, Hover, Pressed, Input, Track];

    internal static StyleBoxFlat Box(Color color,int padding=8,int radius=6)
        =>new(){BgColor=color,BorderColor=Outline,BorderWidthLeft=1,BorderWidthRight=1,BorderWidthTop=1,BorderWidthBottom=1,
            ContentMarginLeft=padding,ContentMarginRight=padding,ContentMarginTop=padding,ContentMarginBottom=padding,
            CornerRadiusTopLeft=radius,CornerRadiusTopRight=radius,CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius};
    internal static Theme Create()
    {
        using var regular=Font(); using var bold=Font(true); var theme=new Theme{DefaultFont=regular,DefaultFontSize=18};
        // SetStylebox retains the native resource. Release its managed wrapper now, before engine shutdown.
        void SetBox(string name, string type, StyleBoxFlat box)
        {
            using (box) theme.SetStylebox(name, type, box);
        }
        theme.SetTypeVariation("TabletopHeading","Label");theme.SetFont("font","TabletopHeading",bold);theme.SetFontSize("font_size","TabletopHeading",24);
        foreach(var type in new[]{"Label","Button","OptionButton","CheckBox","LineEdit","TextEdit","PopupMenu","Tree","ItemList","TabContainer","Window","TooltipLabel"})
        {
            theme.SetColor("font_color",type,Ink);theme.SetColor("font_hover_color",type,Ink);theme.SetColor("font_pressed_color",type,Ink);
            theme.SetColor("font_focus_color",type,Ink);theme.SetColor("font_disabled_color",type,Muted);theme.SetColor("font_selected_color",type,Paper);
        }
        theme.SetFont("normal_font","RichTextLabel",regular);theme.SetFont("bold_font","RichTextLabel",bold);theme.SetColor("default_color","RichTextLabel",Ink);
        foreach(var type in new[]{"Panel","PanelContainer","PopupPanel","PopupMenu","Tree","ItemList","TooltipPanel","AcceptDialog","ConfirmationDialog"}) SetBox("panel",type,Box(Paper,12));
        foreach(var type in new[]{"Button","OptionButton"})
        {
            SetBox("normal",type,Box(Paper,8));SetBox("hover",type,Box(Hover,8));
            SetBox("pressed",type,Box(Pressed,8));SetBox("disabled",type,Box(Background,8));
            using var focus=Box(Colors.Transparent,0);focus.BorderColor=Moss;focus.SetBorderWidthAll(2);theme.SetStylebox("focus",type,focus);
        }
        theme.SetTypeVariation("TabletopPrimary","Button");
        foreach(var name in new[]{"normal","pressed","hover","hover_pressed"}) SetBox(name,"TabletopPrimary",Box(Moss,10));
        foreach(var name in new[]{"font_color","font_hover_color","font_focus_color","font_pressed_color","font_hover_pressed_color"})theme.SetColor(name,"TabletopPrimary",PrimaryInk);
        foreach(var type in new[]{"LineEdit","TextEdit"})
        {
            SetBox("normal",type,Box(Input,6));SetBox("read_only",type,Box(Background,6));
            SetBox("focus",type,Box(Colors.Transparent,0));theme.SetColor("caret_color",type,Ink);theme.SetColor("selection_color",type,Moss);
        }
        SetBox("panel","TabContainer",Box(Paper,10));SetBox("tab_selected","TabContainer",Box(Paper,8));
        SetBox("tab_unselected","TabContainer",Box(Background,8));SetBox("tab_hovered","TabContainer",Box(Hover,8));
        SetBox("tab_disabled","TabContainer",Box(Background,8));
        theme.SetColor("font_selected_color","TabContainer",Ink);theme.SetColor("font_unselected_color","TabContainer",Muted);
        foreach(var type in new[]{"HScrollBar","VScrollBar"})
        {
            SetBox("scroll",type,Box(Background,0,3));SetBox("grabber",type,Box(Moss,0,3));
            SetBox("grabber_highlight",type,Box(Moss.Lightened(.12f),0,3));SetBox("grabber_pressed",type,Box(Ink,0,3));
        }
        SetBox("background","ProgressBar",Box(Track,0,3));SetBox("fill","ProgressBar",Box(Moss,0,3));
        SetBox("hover","PopupMenu",Box(Hover,4));
        foreach(var type in new[]{"Tree","ItemList"}){SetBox("selected",type,Box(Moss,4));SetBox("selected_focus",type,Box(Moss,4));}
        SetBox("embedded_border","Window",Box(Paper,12));theme.SetColor("title_color","Window",Ink);
        foreach(var type in new[]{"VBoxContainer","HBoxContainer"})theme.SetConstant("separation",type,10);
        foreach(var state in new[]{false,true})
        {
            using var icon=CheckIcon(state);
            theme.SetIcon(state?"checked":"unchecked","CheckBox",icon);theme.SetIcon(state?"checked_disabled":"unchecked_disabled","CheckBox",icon);
            theme.SetIcon(state?"radio_checked":"radio_unchecked","CheckBox",icon);
        }
        using var sliderTrack=Box(Outline,0,4);sliderTrack.ContentMarginTop=sliderTrack.ContentMarginBottom=4;
        theme.SetStylebox("slider","HSlider",sliderTrack);
        using var sliderFill=Box(Moss,0,4);sliderFill.ContentMarginTop=sliderFill.ContentMarginBottom=4;
        theme.SetStylebox("grabber_area","HSlider",sliderFill);
        theme.SetStylebox("grabber_area_highlight","HSlider",sliderFill);
        using var knob=Image.CreateEmpty(18,18,false,Image.Format.Rgba8);knob.Fill(Colors.Transparent);
        for(var y=0;y<18;y++)for(var x=0;x<18;x++)
            if(new Vector2(x-8.5f,y-8.5f).Length()<=8)knob.SetPixel(x,y,Moss);
        using var texture=ImageTexture.CreateFromImage(knob);
        theme.SetIcon("grabber","HSlider",texture);theme.SetIcon("grabber_highlight","HSlider",texture);
        using var closeImage=Image.CreateEmpty(18,18,false,Image.Format.Rgba8);closeImage.Fill(Colors.Transparent);
        for(var i=4;i<14;i++){closeImage.FillRect(new Rect2I(i,i,2,2),Ink);closeImage.FillRect(new Rect2I(17-i,i,2,2),Ink);}
        using var closeTexture=ImageTexture.CreateFromImage(closeImage);
        foreach(var name in new[]{"close","close_hl","close_pressed"})theme.SetIcon(name,"Window",closeTexture);
        using var arrow=Arrow();theme.SetIcon("arrow","OptionButton",arrow);
        using var upDown=UpDown();theme.SetIcon("updown","SpinBox",upDown);
        foreach(var name in new[]{"increment","increment_highlight","decrement","decrement_highlight"})
        {
            using var chevron=Chevron(name.StartsWith("increment"));theme.SetIcon(name,"TabContainer",chevron);
        }
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
