#import <Foundation/Foundation.h>

// Reflection-based bridge to FBAdSettings (Meta Audience Network).
// We deliberately avoid #import <FBAudienceNetwork/...> so this file compiles
// regardless of whether the MAX Meta adapter's native SDK arrived via
// CocoaPods or Swift Package Manager, and degrades to a no-op + log line if
// the class isn't linked in yet instead of failing the build.

extern "C" {

void _MetaSetDataProcessingOptions(const char* optionsCsv, int country, int state)
{
    Class fbAdSettings = NSClassFromString(@"FBAdSettings");
    if (!fbAdSettings) {
        NSLog(@"[MetaAdSettingsBridge] FBAdSettings not found - Meta adapter not linked");
        return;
    }

    SEL selector = @selector(setDataProcessingOptions:country:state:);
    NSMethodSignature *sig = [fbAdSettings methodSignatureForSelector:selector];
    if (!sig) {
        NSLog(@"[MetaAdSettingsBridge] setDataProcessingOptions:country:state: not found");
        return;
    }

    NSString *csv = [NSString stringWithUTF8String:optionsCsv];
    NSArray *options = csv.length > 0 ? [csv componentsSeparatedByString:@","] : @[];
    NSUInteger uCountry = (NSUInteger)country;
    NSUInteger uState = (NSUInteger)state;

    NSInvocation *inv = [NSInvocation invocationWithMethodSignature:sig];
    inv.selector = selector;
    inv.target = fbAdSettings;
    [inv setArgument:&options atIndex:2];
    [inv setArgument:&uCountry atIndex:3];
    [inv setArgument:&uState atIndex:4];
    [inv invoke];

    NSLog(@"[MetaAdSettings] DPO set: (%@) country=%d state=%d", [options componentsJoinedByString:@","], country, state);
}

void _MetaSetAdvertiserTrackingEnabled(bool enabled)
{
    Class fbAdSettings = NSClassFromString(@"FBAdSettings");
    if (!fbAdSettings) {
        NSLog(@"[MetaAdSettingsBridge] FBAdSettings not found - Meta adapter not linked");
        return;
    }

    SEL selector = @selector(setAdvertiserTrackingEnabled:);
    NSMethodSignature *sig = [fbAdSettings methodSignatureForSelector:selector];
    if (!sig) {
        NSLog(@"[MetaAdSettingsBridge] setAdvertiserTrackingEnabled: not found");
        return;
    }

    NSInvocation *inv = [NSInvocation invocationWithMethodSignature:sig];
    inv.selector = selector;
    inv.target = fbAdSettings;
    BOOL b = enabled;
    [inv setArgument:&b atIndex:2];
    [inv invoke];

    NSLog(@"[MetaAdSettings] ATE set: %d", enabled);
}

}
