//
//  interflow.swift
//  Interflow_GUI
//
//  Created by Alex on 3/3/17.
//  Copyright © 2017 Microsoft. All rights reserved.
//

import Foundation
import Alamofire

class interflow
{
    public static func getValidation(apiKey: String, completionHandler: @escaping ([String: Any?]) -> ())
    {
        /*
         input:
         apiKey - api key for interflow
         
         description:
         Checks if the given API key is valid, and if it is, returns information about the owner
         
         output:
         returns a dictionary:
         {
            "response": <Int>, 		#http response code
            "data": <[String: Any?]> 	#contents of the remote folder
         }
         
         dictionary deffinition for data
         "data": 
         {
            "Id": <string>, 				#Id of user
            "FirstName": <string>, 			#first name of user
            "LastName": <string>, 			#last name of user
            "email": <string>, 				#email of user
            "RegistrationDate": <string>, 	#registration date of user",
            "AuthenticationType": <string>, #auth type of user
            "Association": <string>, 		#organization of user
            "IsAuthenticated": <bool>, 		#if user is authenticated
            "Name": <string> 				#full name of user
         }
         
        */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        Alamofire.request("https://interflow.azure-api.net/api/debug/who", headers: headers).responseJSON{ response in
            if let result = response.result.value{
                let data = result as! NSDictionary
                let statuscode = (data["statusCode"] as? Int ?? 200)
                if statuscode == 200
                {
                    completionHandler(["response":200, "data":data])
                }
                else
                {
                    completionHandler(["response":statuscode, "data":nil])
                }
            }
        }
    }
    
    
    public static func getInterflowDirs(apiKey: String, path: String, completionHandler: @escaping ([String: Any?]) -> ())
    {
        
        /*
         input:
         apiKey - api key for interflow
         path   - directory in interflow to to search for directories
         
         description:
         Gets the list of directory names in a given directory
         
         output:
         returns a dictionary:
         {
            "response": <Int>, 		#http response code
            "data": NSArray[<string>]	#contents of the remote folder
         }
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        let parameters: Parameters = ["directoryPath": path]
        Alamofire.request("https://interflow.azure-api.net/api/downloads/directorylist", parameters: parameters, headers: headers).responseJSON{ response in

            if let status = response.response?.statusCode{
                switch(status)
                {
                case 200:
                    completionHandler(["response":status, "data":response.result.value])
                default:
                    completionHandler(["response":status, "data":nil])
            
                }
            }
        }
    }
    
    
    public static func getInterflowFiles(apiKey: String, path: String, completionHandler: @escaping ([String: Any?]) -> ())
    {
        
        /*
         input:
         apiKey - api key for interflow
         path   - directory in interflow to to search for files
         
         description:
         Gets the list of files in a given directory
         
         output:
         returns a dictionary:
         {
            "response": <Int>, 			#http response code
            "data": array[<[String:Any]>]	#contents of the remote folder
         }
         
         dictionary deffinition for data
         "data": [
         {
            "Name": <string>, 			#file name
            "Length": <int>, 			#length in bytes
            "RelativePath": <string>, 	#full path to file
            "LastModified": <string> 	#last file modification
         }
         ]
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        let parameters: Parameters = ["directoryPath": path]
        Alamofire.request("https://interflow.azure-api.net/api/downloads/filelist", parameters: parameters, headers: headers).responseJSON{ response in
            
            if let status = response.response?.statusCode{
                switch(status)
                {
                case 200:
                    completionHandler(["response":status, "data":response.result.value])
                default:
                    completionHandler(["response":status, "data":nil])
                    
                }
            }
        }
    }
    
    
    public static func getInterflowFileNamesOnly(apiKey: String, path: String, completionHandler: @escaping ([String: Any?]) -> ())
    {
        
        /*
         input:
         apiKey - api key for interflow
         path   - directory in interflow to to search for files
         
         description:
         Gets the list of file names in a given directory
         
         output:
         returns a dictionary:
         {
            "response": <int>, 		#http response code
            "data": array[<string>]	#contents of the remote folder, names only
         }
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        let parameters: Parameters = ["directoryPath": path]
        Alamofire.request("https://interflow.azure-api.net/api/downloads/filelist", parameters: parameters, headers: headers).responseJSON{ response in
            
            if let status = response.response?.statusCode{
                switch(status)
                {
                case 200:
                    var NameList = [String]()
                    for object in (response.result.value as! NSArray)
                    {
                        NameList.append((object as! [String: Any])["Name"] as! String)
                    }
                    completionHandler(["response":status, "data":NameList])
                default:
                    completionHandler(["response":status, "data":nil])
                    
                }
            }
        }
    }

    
    public static func downloadInterflowFile(apiKey: String, interflow_path: String, interflow_file: String, output_path: URL,completionHandler: @escaping (Int) -> ())
    {
        
        /*
         intput:
         apiKey - api key for interflow
         interflow_path   - directory in interflow to download newest file from
         interflow_file   - name of the file to fetch
         output_path      - Output url for the file
         
         description:
         Gets the contents of the specified file in an interflow directory and writes to a file
         
         output:
         returns an Int:
         <int> 	#http response code
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        let parameters: Parameters = ["directoryPath": interflow_path, "fileName": interflow_file]
        
        let destination: DownloadRequest.DownloadFileDestination = {_, _ in
            return (output_path, [.removePreviousFile])
        }
        
        Alamofire.download("https://interflow.azure-api.net/api/downloads/file" ,parameters: parameters, headers: headers, to: destination).response{ response in
            
            if let status = response.response?.statusCode{
                completionHandler(status)
            }
        }
    }
 

    public static func uploadInterflowGenericFile(apiKey: String, path: URL, completionHandler: @escaping (Int) -> ())
    {
        
        /*
         input:
         
         apiKey - api key for interflow
         path - The path to the file you wish to upload
         
         description:
         Uploads a generic file to interflow for analysis.
         
         
         output:
         returns an int: response code from the API call.
         202 File upload sucessful.
         204 No Content: The request does not contain any file. //this should never be a return code the user encounters.
         409 Conflict: File already exists.
         415 Unsupported media type.
         500 Internal server error"
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        Alamofire.upload(
            multipartFormData: {multipartFormData in
                multipartFormData.append( path, withName: path.lastPathComponent)
                },
            to: "https://interflow.azure-api.net/api/file/upload",
            method: .post,
            headers: headers,
            encodingCompletion: {result in
                switch result{
                    case .success(let upload, _, _):
                        upload.responseJSON{ response in
                            completionHandler((response.response?.statusCode)!)
                        }
                    case .failure:
                        completionHandler(-1)
                }
            }
        )
    }
    
    
    //todo: make do this be how is should
    public static func uploadInterflowOneIndicator(apiKey: String, OneIndicatorJson: String, completionHandler: @escaping (Int) -> ())
    {
        
        /*
         input:
         
         apiKey - api key for interflow
         OneIndicatorJson - The array of json one indicators
         
         description:
         Uploads a OneIndicator via a json string.
         
         
         output:
         returns an int: response code from the API call.
         201 Indicator upload sucessful.
         400 One or more indicators is not valid.
         409 One or more indicators was not received or accepted by the hub.
         */
        
        
        var request = URLRequest(url: NSURL(string: "https://interflow.azure-api.net/api/indicators/upload") as! URL)
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.httpBody = OneIndicatorJson.data(using: .utf8)!
        request.httpMethod = "POST"
        request.addValue(apiKey, forHTTPHeaderField: "Ocp-Apim-Subscription-Key")
        
        Alamofire.request(request).responseJSON{ response in
            if let status = response.response?.statusCode{
                completionHandler(status)
            }
        }

    }
    
    
    public static func getInterflowSharedFiles(apiKey: String, completionHandler: @escaping ([String: Any?]) -> ())
    {
        
        /*
         input:
         apiKey - api key for interflow
         
         description:
         Gets the list of shared files
         
         output:
         returns a dictionary:
         {
         "response": <int>, 			#http response code
         "data": array[<dictionary>]	#shared file data
         }
         
         dictionary deffinition for data
         "data": [
         {
            "Name": <string>, 			#file name
            "Length": <int>, 			#length in bytes
            "RelativePath": <string>, 	#full path to file
            "LastModified": <string> 	#last file modification
         }
         ]
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        
        Alamofire.request("https://interflow.azure-api.net/api/file/listsharedfiles", headers: headers).responseJSON{ response in
            
            if let status = response.response?.statusCode{
                switch(status)
                {
                case 200:
                    completionHandler(["response":status, "data":response.result.value])
                default:
                    completionHandler(["response":status, "data":nil])
                    
                }
            }
        }
    }
    
    
    public static func getInterflowSharedFileNamesOnly(apiKey: String, completionHandler: @escaping ([String: Any?]) -> ())
    {
        
        /*
         input:
         apiKey - api key for interflow
         
         description:
         Gets the list of shared files
         
         output:
         returns a dictionary:
         {
         "response": <int>, 			#http response code
         "data": array[<string>]	#file names of shared files
         }
         
         */
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        
        Alamofire.request("https://interflow.azure-api.net/api/file/listsharedfiles", headers: headers).responseJSON{ response in
            
            if let status = response.response?.statusCode{
                switch(status)
                {
                case 200:
                    var NameList = [String]()
                    for object in (response.result.value as! NSArray)
                    {
                        NameList.append((object as! [String: Any])["Name"] as! String)
                    }
                    completionHandler(["response":status, "data":NameList])
                default:
                    completionHandler(["response":status, "data":nil])
                    
                }
            }
        }
    }
    
    
    public static func downloadInterflowSharedFile(apiKey: String, interflow_file: String, output_path: URL,completionHandler: @escaping (Int) -> ())
    {
        
        /*
         intput:
         apiKey - api key for interflow
         interflow_file   - name of the file to fetch
         output_path      - URL of output file
         
         description:
         downloads the specified shared file
         
         output:
         returns an int:
         <int>	#http response code
         */
        
        
        let headers: HTTPHeaders = ["Ocp-Apim-Subscription-Key": apiKey]
        let parameters: Parameters = ["fileName": interflow_file]
        
        let destination: DownloadRequest.DownloadFileDestination = {_, _ in
            return (output_path, [.removePreviousFile])
        }
        
        Alamofire.download("https://interflow.azure-api.net/api/file/download" ,parameters: parameters, headers: headers, to: destination).response{ response in
            if let status = response.response?.statusCode{
                completionHandler(status)
            }
        }
    }
}
