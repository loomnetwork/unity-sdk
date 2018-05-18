#import <Foundation/Foundation.h>
#import "LoomSDKSwift.h"

#import <UIKit/UIKit.h>
#import "UnityInterface.h"
extern "C" {
    typedef void (*callbackFunc)(const char *);
}
static callbackFunc cb_done=nil;
static callbackFunc cb_err=nil;
static callbackFunc cb_user_info=nil;


extern "C" {
    
    void _ex_callGetAccessToken(const char *message,callbackFunc _cb_done,callbackFunc _cb_err) {
        cb_done=_cb_done;
        cb_err=_cb_err;
        [LoomSDK login:[NSString stringWithUTF8String:message]];
        
    }
    void _ex_callGetUserProfile(const char *baseDomain,const char *accessToken,callbackFunc _cb_user_info) {
        cb_user_info=_cb_user_info;
        [LoomSDK getUserInfo:[NSString stringWithUTF8String:accessToken] domain:[NSString  stringWithUTF8String:baseDomain]];
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
    
    void callAuthCB(const char *message)
    {
        if(cb_user_info)
            cb_user_info(message);
    };
}

