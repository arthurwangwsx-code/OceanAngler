using UnityEngine;
namespace OceanAngler
{
    public sealed partial class GameUI
    {
        bool developerTarget;
        void BuildDeveloperMode()
        {
            var panel=PagePanel("DeveloperMode",Vector2.zero,Vector2.one,520);
            Label(panel,"本人测试可直接体验全部海域、钓具、观察和图鉴模型。\n测试记录单独保存，关闭后回到正常进度。",Compact?18:22,Muted,28,18,-56,112);
            Label(panel,game.DeveloperMode?"当前：开发者模式":"当前：正常模式",Compact?20:24,game.DeveloperMode?Gold:Color.white,28,142,-56,40);
            Button(panel,game.DeveloperMode?"关闭开发者模式":"开启开发者模式",28,202,-56,54,()=>{developerTarget=!game.DeveloperMode;game.ClearNotice();Navigate("切换模式确认");},Teal);
            Button(panel,"重建测试进度并开启",28,270,-56,54,()=>{game.ClearNotice();Navigate("重建测试确认");},Navy);
            Label(panel,game.Notice??"切换前会再次确认。正常进度和积分不会被测试记录替换。",17,Muted,28,344,-56,106);
            Button(panel,"返回家长区",28,462,-56,44,GoBack,Navy);
        }
        void BuildDeveloperConfirmation(bool rebuild)
        {
            var panel=PagePanel("DeveloperConfirmation",Vector2.zero,Vector2.one,470);
            string title=rebuild?"重建测试进度并开启？":developerTarget?"开启开发者模式？":"回到正常模式？";
            Label(panel,title,Compact?24:28,Color.white,28,18,-56,44);
            string detail=rebuild?"现有测试文件会保留，再建立全新的测试进度。正常进度保持。":"开发者测试进度与正常进度分开保存。关闭后，原正常进度和积分限制恢复。";
            detail+="\n切换会结束当前未记录的鱼获，已有记录保留。返回准备后再主动开始钓鱼。";
            Label(panel,detail,Compact?18:22,Muted,28,82,-56,144);
            Button(panel,rebuild?"确认重建测试进度":developerTarget?"确认开启开发者模式":"确认关闭开发者模式",28,244,-56,54,()=>
            {
                bool changed=rebuild?game.RebuildDeveloperProgress():game.SwitchDeveloperMode(developerTarget);
                if(changed){pageTrail.Clear();OpenPage("准备",false);}
            },Teal);
            Button(panel,"取消，保持当前模式",28,312,-56,54,GoBack,Navy);
            Label(panel,game.Notice??"",17,Muted,28,386,-56,64);
        }
    }
}
