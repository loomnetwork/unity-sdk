#import <Foundation/Foundation.h>
#import "LoomSDKSwift.h"

#import <UIKit/UIKit.h>
#import "UnityInterface.h"
extern "C" {
    typedef void (*callbackFunc)(const char *);
}
static callbackFunc cb_done=nil;
static callbackFunc cb_err=nil;



extern "C" {
    
    void _ex_callGetAccessToken(const char *message,callbackFunc _cb_done,callbackFunc _cb_err) {
        cb_done=_cb_done;
        cb_err=_cb_err;
        [LoomSDK login:[NSString stringWithUTF8String:message]];
        
    }
    void _ex_callResumeAuth(const char *message)
    {
        [LoomSDK resumeAuth:[NSString stringWithUTF8String:message]];
    }
    void callFailCB(const char *message)
    {
        if(cb_err)
            cb_err(message);
    }
    
    void callDoneCB(const char *message)
    {
        if(cb_done)
            cb_done(message);
    };
}

