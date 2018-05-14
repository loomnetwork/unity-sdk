//
//  Use this file to import your target's public headers that you would like to expose to Swift.
//

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import "UnityInterface.h"

#import "Auth0/auth0.h"
extern void callFailCB(const char *message);
extern void callDoneCB(const char *message);
extern void callAuthCB(const char *message);

