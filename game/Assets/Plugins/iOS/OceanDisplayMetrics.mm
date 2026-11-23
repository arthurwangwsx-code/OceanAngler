#import <UIKit/UIKit.h>
// 游戏仅使用横屏。按当前屏幕的逻辑点宽度计算UI，而不是把渲染像素当作点。
extern "C" double OceanLandscapePointWidth(void) {
    CGSize size = UIScreen.mainScreen.bounds.size;
    return MAX(size.width, size.height);
}
