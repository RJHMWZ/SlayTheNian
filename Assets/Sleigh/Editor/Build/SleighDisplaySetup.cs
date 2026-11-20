#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace Sleigh.Editor
{
    public static class SleighDisplaySetup
    {
        [MenuItem("Sleigh/设置1920×1080预览")]
        public static void FullHD()
        {
            PlayerSettings.defaultScreenWidth=1920; PlayerSettings.defaultScreenHeight=1080;
            var flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
            var assembly=typeof(EditorWindow).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes");
            var instance=sizesType.BaseType.GetProperty("instance",flags).GetValue(null,null);
            var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group=sizesType.GetMethod("GetGroup",flags).Invoke(instance,new[]{Enum.Parse(groupType,"Standalone")});
            var type=group.GetType(); int count=(int)type.GetMethod("GetTotalCount",flags).Invoke(group,null),index=-1;
            var sizeType=assembly.GetType("UnityEditor.GameViewSize");
            for(int i=0;i<count;i++)
            {
                var size=type.GetMethod("GetGameViewSize",flags).Invoke(group,new object[]{i});
                if ((int)sizeType.GetProperty("width",flags).GetValue(size,null)==1920 && (int)sizeType.GetProperty("height",flags).GetValue(size,null)==1080)
                { index=i;break; }
            }
            if(index<0)
            {
                var kind=Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"),"FixedResolution");
                var size=Activator.CreateInstance(sizeType,flags,null,new object[]{kind,1920,1080,"全高清"},null);
                type.GetMethod("AddCustomSize",flags).Invoke(group,new[]{size});index=count;
            }
            var gameType=assembly.GetType("UnityEditor.GameView");
            var window=EditorWindow.GetWindow(gameType);gameType.GetProperty("selectedSizeIndex",flags).SetValue(window,index,null);window.Repaint();
        }
    }
}
#endif
