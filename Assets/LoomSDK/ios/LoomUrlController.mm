#import <UIKit/UIKit.h>
#import "UnityAppController.h"
#import "UI/UnityView.h"
#import "UI/UnityViewControllerBase.h"
#import "LoomSDKSwift.h"

@interface LoomUrlController : UnityAppController

@end

@implementation LoomUrlController

- (BOOL)application:(UIApplication*)application openURL:(NSURL*)url sourceApplication:(NSString*)sourceApplication annotation:(id)annotation {
    NSLog(@"url recieved: %@", url);
    if (!url) {  return NO; }
    [LoomSDK resumeAuth:[url absoluteString] ];
    return YES;
}

@end

IMPL_APP_CONTROLLER_SUBCLASS(LoomUrlController)
