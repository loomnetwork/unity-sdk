import Foundation
import UIKit
import Auth0


class LoomSDK : NSObject {
    
    static func login(_ message: String) {
        let data = message.data(using: String.Encoding.utf8, allowLossyConversion: false)!
        do {
            let json = try JSONSerialization.jsonObject(with: data, options: []) as! [String: AnyObject]
            guard let clientId = json["ClientId"] as? String else { return }
            guard let domain = json["Domain"] as? String else {return}
            guard let audience = json["Audience"] as? String else {return}
            guard let scope = json["Scope"] as? String else {return}
            let auth=Auth0
                .webAuth(clientId: clientId, domain:domain)
                .scope(scope)
                .audience(audience)
            auth.start {
                switch $0 {
                case .failure(let error):
                    callFailCB(error.localizedDescription);
                case .success(let credentials):
                    guard let accessToken = credentials.accessToken else { return }
                    callDoneCB(accessToken as String);
                }
            }
        } catch let error as NSError {
            print("Failed to load: \(error.localizedDescription)")
        }
    }
    static  func getUserInfo(_ accessToken: String, domain: String )
    {
        Auth0.authentication(clientId: "", domain: domain)
            .userInfo(withAccessToken: accessToken)
            .start { result in
                switch(result) {
                case .success(let profile):
                    do {
                        let profileDict =  try JSONSerialization.data(withJSONObject:profile.dictionaryWithValues(forKeys: ["email"]), options: JSONSerialization.WritingOptions.prettyPrinted)
                        let jsonString = String(data: profileDict, encoding: String.Encoding.utf8)
                        callAuthCB(jsonString)
                    } catch _ {
                        callAuthCB("{}")
                    }
                case .failure(_):
                    callAuthCB("{}")                        }
        }
    }
    
    static func resumeAuth(_ str:NSString)
    {
        let url=URL.init(string:str as String);
        Auth0.resumeAuth(url!, options:[:])
    }
}
