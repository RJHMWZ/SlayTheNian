using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace Sleigh
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelLabel : MaskableGraphic
    {
        public string text=""; public float fontScale=1; public int align; public bool shadow=true,wrap;
        List<string> cachedLines;
        string cachedText;
        float cachedWidth,cachedScale;
        bool cachedWrap;
        public override Texture mainTexture => ChinesePixelFont.Texture;
        public static int CharWidth(char c) => ChinesePixelFont.Advance(c);
        public static float Measure(string value,float scale=1)
        { float width=0; foreach(char c in value) width+=CharWidth(c); return width*scale; }
        public static List<string> Lines(string value,float width,float scale,bool wrap)
        {
            var result=new List<string>();
            foreach(var paragraph in Chinese.Text(value).Replace("\r","").Split('\n'))
            {
                if (!wrap || width<=0) { result.Add(paragraph); continue; }
                var line=new StringBuilder(); float used=0;
                foreach(char c in paragraph)
                {
                    float advance=CharWidth(c)*scale;
                    if (line.Length>0 && used+advance>width)
                    {
                        if (line.Length>1 && "，。！？；：、）》】」』…,.!?;:)".IndexOf(c)>=0)
                        { char tail=line[line.Length-1]; line.Length--; result.Add(line.ToString()); line.Clear(); line.Append(tail); used=CharWidth(tail)*scale; }
                        else { result.Add(line.ToString()); line.Clear(); used=0; }
                    }
                    if (line.Length==0 && c==' ') continue;
                    line.Append(c); used+=advance;
                }
                result.Add(line.ToString());
            }
            return result;
        }
        public void Set(string value) { value=Chinese.Text(value); if(value==text)return; text=value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); string display=Chinese.Text(text);
            if (cachedLines==null || cachedText!=display || cachedWidth!=rectTransform.rect.width || cachedScale!=fontScale || cachedWrap!=wrap)
            { cachedText=display;cachedWidth=rectTransform.rect.width;cachedScale=fontScale;cachedWrap=wrap;cachedLines=Lines(display,cachedWidth,fontScale,wrap); }
            float top=rectTransform.rect.yMax;
            foreach(var line in cachedLines)
            {
                float measured=Measure(line,fontScale);
                float scale=!wrap && measured>rectTransform.rect.width ? fontScale*rectTransform.rect.width/measured : fontScale;
                float actual=Measure(line,scale);
                float x=rectTransform.rect.xMin+(align==1?(rectTransform.rect.width-actual)*.5f:align==2?rectTransform.rect.width-actual:0);
                foreach(char c in line)
                {
                    if(c!=' ')
                    {
                        var uv=ChinesePixelFont.UV(c);
                        if(shadow) Quad(vh,x+scale,top-scale,scale*16,uv,new Color(.015f,.015f,.035f,color.a*.9f));
                        Quad(vh,x,top,scale*16,uv,color);
                    }
                    x+=CharWidth(c)*scale;
                }
                top-=12*fontScale;
            }
        }
        static void Quad(VertexHelper vh,float x,float top,float size,Rect uv,Color c)
        {
            int n=vh.currentVertCount;
            vh.AddVert(new Vector3(x,top-size),c,new Vector2(uv.xMin,uv.yMin));vh.AddVert(new Vector3(x,top),c,new Vector2(uv.xMin,uv.yMax));
            vh.AddVert(new Vector3(x+size,top),c,new Vector2(uv.xMax,uv.yMax));vh.AddVert(new Vector3(x+size,top-size),c,new Vector2(uv.xMax,uv.yMin));
            vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n+2,n+3,n);
        }
    }
}
