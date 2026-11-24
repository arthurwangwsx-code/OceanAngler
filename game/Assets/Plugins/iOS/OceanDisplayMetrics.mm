#import <UIKit/UIKit.h>
#import <dispatch/dispatch.h>
// 游戏仅使用横屏。按当前屏幕的逻辑点宽度计算UI，而不是把渲染像素当作点。
extern "C" double OceanLandscapePointWidth(void) {
    CGSize size = UIScreen.mainScreen.bounds.size;
    return MAX(size.width, size.height);
}
static NSArray<NSString *> *OceanProcessArguments(void) {
    static NSArray<NSString *> *arguments;
    static dispatch_once_t once;
    dispatch_once(&once, ^{ arguments = [NSProcessInfo.processInfo.arguments copy]; });
    return arguments;
}
extern "C" int OceanLaunchArgumentCount(void) {
    return (int)OceanProcessArguments().count;
}
extern "C" const char *OceanLaunchArgumentAt(int index) {
    NSArray<NSString *> *arguments = OceanProcessArguments();
    return index >= 0 && index < (int)arguments.count ? arguments[index].UTF8String : nullptr;
}
