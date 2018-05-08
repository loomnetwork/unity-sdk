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
                    callFailCB(error.localizedDescription as! String);
                case .success(let credentials):
                    guard let accessToken = credentials.accessToken else { return }
                    callDoneCB(accessToken as String);
                    
                }
            }
        } catch let error as NSError {
            print("Failed to load: \(error.localizedDescription)")
        }
    }
    static func resumeAuth(_ str:NSString)
    {
        let url=URL.init(string:str as String);
        Auth0.resumeAuth(url!, options:[:])
    }
}
