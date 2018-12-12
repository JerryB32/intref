//
//  ViewController.swift
//  gui_interflow_osx
//
//  Created by Alex on 3/14/17.
//  Copyright © 2017 Microsoft. All rights reserved.
//

import Cocoa


class ViewController: NSViewController {
    
    //define outlets for gui application
    @IBOutlet weak var TableViewMainView: NSTableView!
    
    @IBOutlet weak var PasswordAPIKey: NSSecureTextField!
    @IBOutlet weak var ButtonSubmit: NSButton!
    
    @IBOutlet weak var ButtonDownloadSelected: NSButton!
    
    @IBOutlet weak var ButtonUploadFile: NSButton!
    @IBOutlet weak var ButtonUploadOneIndicator: NSButton!
    
    @IBOutlet weak var ProgressBar: NSProgressIndicator!
    
    
    //class variables for holding information about the view
    var directoryItems: [[String:Any]?] = []
    
    var LastDir: String = ""
    var CurrentDir: String = ""
    var APIKey: String = ""
    
    //default override methods
    override func viewDidLoad() {
        super.viewDidLoad()

        //set deligate to load table information
        TableViewMainView.delegate = self
        TableViewMainView.dataSource = self
        
        //set action target for double click
        TableViewMainView.target = self
        TableViewMainView.doubleAction = #selector(tableViewDoubleClick(_:))
        
        ProgressBar.minValue = 0
        ProgressBar.maxValue = 1
        ProgressBar.doubleValue = 0.0
        ProgressBar.isIndeterminate = false
    }

    
    
    
    
    
    override var representedObject: Any? {
        didSet {
        // Update the view, if already loaded.
        }
    }
    
    
    
    
    
    
    
    //This is a helper function that takes all of the outputs from the interflow API calls and puts them into the dictionary we populate the view window with, then refreshes the view.
    func reloadFileList(ContentFiles: [[String:Any]?], ContentDirs: [String], ContentSharedFiles: [[String:Any]?] = []) {
        
        var Content: [[String:Any]?] = []
        
        //add in the back directory (../) the dir traversal is handled in the double click handler
        Content.append(["Name":"../", "Length":"dir", "RelativePath":"", "LastModified":""])
        
        //for files
        for file in ContentFiles {
            var tmpFile: [String:Any] = [String:Any]()
            tmpFile["Name"] = file?["Name"]
            tmpFile["Length"] = ByteCountFormatter.string(fromByteCount: (file?["Length"]! as! Int64), countStyle: ByteCountFormatter.CountStyle.file)
            tmpFile["RelativePath"] = file?["RelativePath"]
            tmpFile["LastModified"] = file?["LastModified"]
            Content.append(tmpFile)
        }
        
        //for files
        for file in ContentDirs {
            var tmpFile: [String:Any] = [String:Any]()
            tmpFile["Name"] = file+"/"
            tmpFile["Length"] = "dir"
            tmpFile["RelativePath"] = ""
            tmpFile["LastModified"] = ""
            Content.append(tmpFile)
        }
        
        //for shared files... this will only be populated when in the root directory
        for file in ContentSharedFiles {
            var tmpFile: [String:Any] = [String:Any]()
            tmpFile["Name"] = file?["Name"]
            tmpFile["Length"] = ByteCountFormatter.string(fromByteCount: (file?["Length"]! as! Int64), countStyle: ByteCountFormatter.CountStyle.file)
            tmpFile["RelativePath"] = file?["RelativePath"]
            tmpFile["LastModified"] = file?["LastModified"]
            Content.append(tmpFile)
        }
        
        directoryItems = Content
        TableViewMainView.reloadData()
    }
    
    
    
    
    
    
    
    
    //Handle the user submitting their api key
    @IBAction func ButtonOnClickSubmit(_ sender: Any) {
        
        //first, we are about to make an api call to validate the key. start the progress bar to show we are doing something.
        ProgressBar.isIndeterminate = true
        ProgressBar.startAnimation(nil)
        
        //do the api call and see if the key is valid (check for 200 status)
        interflow.getValidation(apiKey: PasswordAPIKey.stringValue){ responseDict in
            if responseDict["response"] as! Int != 200
            {
                let myPopup: NSAlert = NSAlert()
                myPopup.messageText = "Your API Key seems to be invalid. Please check the API Key entered and try again."
                myPopup.alertStyle = NSAlertStyle.warning
                myPopup.addButton(withTitle: "OK")
                myPopup.runModal()
                
                //stop the animation on the progress bar
                self.ProgressBar.isIndeterminate = false
                self.ProgressBar.stopAnimation(nil)
            }
            else
            {
                //user has authenticated. lets set variables and load up the files and folders that will be in the root
                self.LastDir = ""
                self.CurrentDir = ""
                self.APIKey = self.PasswordAPIKey.stringValue
                
                
                //Get the list of files in the root directory (should be none)
                interflow.getInterflowFiles(apiKey: self.APIKey, path: ""){ fileResponse in
                    
                    //Get the list of directories
                    interflow.getInterflowDirs(apiKey: self.APIKey, path: ""){ dirResponse in
                        
                        //Lastly, since we are in the root directory, we should get the shared files as well.
                        interflow.getInterflowSharedFiles(apiKey: self.APIKey){ sharedResponse in
                            
                            //call reloadFileList to update the view
                            self.reloadFileList(
                                ContentFiles:       (fileResponse["data"] as! [[String:Any]]),
                                ContentDirs:        (dirResponse["data"] as! [String]),
                                ContentSharedFiles: (sharedResponse["data"] as! [[String:Any]])
                            )
                            
                            //stop the animation on the progress bar
                            self.ProgressBar.isIndeterminate = false
                            self.ProgressBar.stopAnimation(nil)
                        }
                    }
                }
            }
        }
    }

    
    
    
    
    
    
    //handle the button press for the download selected button. this can download a list of files, a directory, or a mix of the two
    @IBAction func ButtonOnClickDownloadSelected(_ sender: Any) {
        
        //if no rows are selected, just return
        if(TableViewMainView.selectedRow < 0)
        {
            return
        }
        
        //prompt user for an output folder
        let openDiag : NSOpenPanel = NSOpenPanel()
            
        openDiag.canChooseDirectories = true
        openDiag.canChooseFiles = false
        openDiag.begin(){ buttonPress in
            if(buttonPress == NSFileHandlingPanelOKButton)
            {
                //The user pressed ok, we can go ahead and start processing the file downloads
                
                //start make sure the progress bar is empty
                self.ProgressBar.doubleValue = 0.0
                self.ProgressBar.isIndeterminate = false
                
                var loopCounterCompleted : Int = 0
                var totalLoopingThrough : Int = self.TableViewMainView.selectedRowIndexes.count
                
                //loop through the row indexes the user has selected
                for rowIndex in self.TableViewMainView.selectedRowIndexes
                {
                    
                    //get the contents of that row and put it in item
                    let item : [String:Any] = self.directoryItems[rowIndex]!
                    
                    //check if its a directory:
                    if(item["Length"] as! String != "dir")
                    {
                        //downloading selected file, not a folder
                        
                        //All files in the root are shared files, and files which are not in root are regular files. use this to determine what download endpoint to use.
                        //another option would be to check the RelativePath, as shared files do not have a RelativePath
                        if(self.CurrentDir != "")
                        {
                            //normal file download
                            interflow.downloadInterflowFile(apiKey: self.APIKey, interflow_path: self.CurrentDir, interflow_file: item["Name"] as! String, output_path: openDiag.url!.appendingPathComponent(item["Name"] as! String)){ response in
                                if(response != 200)
                                {
                                    //some error happened. alert on this
                                    let myPopup: NSAlert = NSAlert()
                                    myPopup.messageText = "There was an error downloading the shared file. Download resulted in status code " + String(response) + "\nPlease try downloading the file again"
                                    myPopup.alertStyle = NSAlertStyle.warning
                                    myPopup.addButton(withTitle: "OK")
                                    myPopup.runModal()
                                }
                                //update the counter used in the progress bar calculation
                                loopCounterCompleted += 1
                                //update the progress bar
                                self.ProgressBar.doubleValue = Double(loopCounterCompleted) / Double(totalLoopingThrough)
                                
                                //check for all files being completed
                                if(loopCounterCompleted == totalLoopingThrough)
                                {
                                    let myPopup: NSAlert = NSAlert()
                                    myPopup.messageText = "Finished downloading files."
                                    myPopup.alertStyle = NSAlertStyle.warning
                                    myPopup.addButton(withTitle: "OK")
                                    myPopup.runModal()
                                    self.ProgressBar.doubleValue = 0.0
                                    self.ProgressBar.isIndeterminate = false
                                }
                            }
                        }
                        else
                        {
                            //shared file download
                            interflow.downloadInterflowSharedFile(apiKey: self.APIKey, interflow_file: item["Name"] as! String, output_path: openDiag.url!.appendingPathComponent(item["Name"] as! String)){ response in
                                if(response != 200)
                                {
                                    //some error happened. alert on this
                                    let myPopup: NSAlert = NSAlert()
                                    myPopup.messageText = "There was an error downloading the shared file. Download resulted in status code " + String(response) + "\nPlease try downloading the file again"
                                    myPopup.alertStyle = NSAlertStyle.warning
                                    myPopup.addButton(withTitle: "OK")
                                    myPopup.runModal()
                                }
                                //update the counter used in the progress bar calculation
                                loopCounterCompleted += 1
                                //update the progress bar for shared files too!
                                self.ProgressBar.doubleValue = Double(loopCounterCompleted) / Double(totalLoopingThrough)
                                
                                //check for all files being completed
                                if(loopCounterCompleted == totalLoopingThrough)
                                {
                                    let myPopup: NSAlert = NSAlert()
                                    myPopup.messageText = "Finished downloading files."
                                    myPopup.alertStyle = NSAlertStyle.warning
                                    myPopup.addButton(withTitle: "OK")
                                    myPopup.runModal()
                                    self.ProgressBar.doubleValue = 0.0
                                    self.ProgressBar.isIndeterminate = false
                                }
                            }
                        }
                        
                    }
                    else
                    {
                        //user is downloading a folder now
                        
                        //make sure someone doesnt try and download "../"
                        if(item["Name"] as! String == "../")
                        {
                            //this selection counts in what our progress bar was showing. fix that.
                            totalLoopingThrough -= 1
                            continue
                        }
                        
                        //prompt the user and make sure they want to actually download a directory
                        let dirDownloadWarningPopup: NSAlert = NSAlert()
                        dirDownloadWarningPopup.messageText = "You have selected to download the folder: " + (item["Name"] as! String) + "\n\nThis will create a new directory in the output destination and download all of the contents. BE AWARE that some directories contain multiple gigabytes of data and this operation may take a long time to complete. Do you want to continue? \n\nPressing no will skip this folder and continue with other downloads."
                        dirDownloadWarningPopup.alertStyle = NSAlertStyle.warning
                        dirDownloadWarningPopup.addButton(withTitle: "Cancel")
                        dirDownloadWarningPopup.addButton(withTitle: "OK")
                        let response = dirDownloadWarningPopup.runModal()
                        
                        if( response == NSAlertFirstButtonReturn)
                        {
                            //user pressed cancel. skip this item. remove the count from the status bar
                            totalLoopingThrough -= 1
                            continue
                        }
                        else
                        {
                            //user wants to mirror a folder. make a local path to put it in
                            let newDir = openDiag.url!.appendingPathComponent(item["Name"] as! String)
                            
                            //make the output folder
                            do{
                                try FileManager.default.createDirectory(at: newDir, withIntermediateDirectories: false, attributes: nil)
                            } catch let error as NSError {
                                
                                //something happened when making the folder. throw an error and skip
                                let myPopup: NSAlert = NSAlert()
                                myPopup.messageText = "There was an error creating an output directory for the folder copy. Please make sure you have permissions to create folders in the output directory you chose earlier.\n\nSkipping the folder download (other downloads will still run) \n\n"+error.localizedDescription
                                myPopup.alertStyle = NSAlertStyle.warning
                                myPopup.addButton(withTitle: "OK")
                                myPopup.runModal()
                                
                                //and remove the count in what we are downloading.
                                totalLoopingThrough -= 1
                                continue
                            }
                            
                            //get the names of all the files in that folder. Interflow will never have nested folders, so we don't need to do any appending of current path to item["Name"]
                            interflow.getInterflowFileNamesOnly( apiKey: self.APIKey, path: (item["Name"] as! String)) { response in
                                let files = response["data"] as! NSArray
                                
                                //add the count of what was in the folder (minus 1, since the folder being selected is already counted)
                                totalLoopingThrough += files.count - 1
                                for filename in files
                                {
                                    //shared files cant be downloaded though a folder batch download, so no need to check for that
                                    interflow.downloadInterflowFile(apiKey: self.APIKey, interflow_path: item["Name"] as! String, interflow_file: filename as! String, output_path: newDir.appendingPathComponent(filename as! String)){ response in
                                        loopCounterCompleted += 1
                                        self.ProgressBar.doubleValue = Double(loopCounterCompleted) / Double(totalLoopingThrough)
                                        
                                        //check for all files being completed
                                        if(loopCounterCompleted == totalLoopingThrough)
                                        {
                                            let myPopup: NSAlert = NSAlert()
                                            myPopup.messageText = "Finished downloading files."
                                            myPopup.alertStyle = NSAlertStyle.warning
                                            myPopup.addButton(withTitle: "OK")
                                            myPopup.runModal()
                                            self.ProgressBar.doubleValue = 0.0
                                            self.ProgressBar.isIndeterminate = false
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            
            }
            //user pressed cancel on output folder selection diag
            else
            {
                return
            }
        }
    }
    
    
    
    
    
    
    
    @IBAction func ButtonOnClickUploadFile(_ sender: Any) {
        if(APIKey != "")
        {
        
            let openDiag : NSOpenPanel = NSOpenPanel()
        
            openDiag.canChooseDirectories = false
            openDiag.canChooseFiles = true
            openDiag.begin(){ buttonPress in
            if(buttonPress == NSFileHandlingPanelOKButton)
                {
                    interflow.uploadInterflowGenericFile(apiKey: self.APIKey, path: openDiag.url!)
                    { returnCode in
                        
                        var alertmsg : String = String()
                        switch returnCode{
                        case 409:
                            alertmsg = "A file already exists with this name. Please rename the file and try again"
                        case 202:
                            alertmsg = "File uploaded sucessful"
                        case -1:
                            alertmsg = "An unknown problem has occured. Sometimes this occurs when the file does not have an extention."
                        default:
                            alertmsg = "An unknown problem has occured. " + String(returnCode)
                            
                        }
                        let myPopup: NSAlert = NSAlert()
                        myPopup.messageText = alertmsg
                        myPopup.alertStyle = NSAlertStyle.warning
                        myPopup.addButton(withTitle: "OK")
                        myPopup.runModal()
                    }
                }
            }
        }
        else
        {
            let myPopup: NSAlert = NSAlert()
            myPopup.messageText = "Please set your API key before uploading files."
            myPopup.alertStyle = NSAlertStyle.warning
            myPopup.addButton(withTitle: "OK")
            myPopup.runModal()

        }
    }

    
    
    
    
    
    
    @IBAction func ButtonOnClickUploadOneIndicator(_ sender: Any) {
        if(APIKey != "")
        {
            let popup = NSAlert()
            popup.addButton(withTitle: "OK")
            popup.addButton(withTitle: "Cancel")
            popup.messageText = "OneIndicator for upload"
            
            let textbox = NSTextField(frame: NSRect(x:0,y:0,width:350,height:175))
            popup.accessoryView = textbox
            
            let response: NSModalResponse = popup.runModal()
            
            if (response == NSAlertFirstButtonReturn){
                interflow.uploadInterflowOneIndicator(apiKey: APIKey, OneIndicatorJson: textbox.stringValue)
                { returnCode in
                    
                    var alertmsg : String = String()
                    switch returnCode{
                    case 201:
                        alertmsg = "OneIndicator upload sucessful."
                    case 400:
                        alertmsg = "One or more OneIndicators are not valid"
                    case 409:
                        alertmsg = "One or more indicators was not received or accepted by the hub"
                    default:
                        alertmsg = "An unknown problem has occured." + String(returnCode)
                    }
                    let myPopup: NSAlert = NSAlert()
                    myPopup.messageText = alertmsg
                    myPopup.alertStyle = NSAlertStyle.warning
                    myPopup.addButton(withTitle: "OK")
                    myPopup.runModal()

                }
            }
        }
        else
        {
            let myPopup: NSAlert = NSAlert()
            myPopup.messageText = "Please set your API key before uploading OneIndicators."
            myPopup.alertStyle = NSAlertStyle.warning
            myPopup.addButton(withTitle: "OK")
            myPopup.runModal()
            
        }

    }
    
    
    
    
    
    
    
    //handle double click on the table view
    func tableViewDoubleClick(_ sender: AnyObject)
    {
        //first check to make sure the double click wasnt on empty space
        guard TableViewMainView.selectedRow >= 0,
            let item = directoryItems[TableViewMainView.selectedRow]
            else
            {
                //return if double click wasnt on a row
                return
            }
        
        if item["Length"] as! String == "dir"
        {
            //handle double clicking on the directory
            
            //first, get the progress bar moving
            ProgressBar.isIndeterminate = true
            ProgressBar.startAnimation(nil)
            
            if(item["Name"] as! String != "../")
            {
                //we just had the user double click a directory that isnt the special up dir link. lets "go into it" by setting the data displayed in the table view to be this new folder
                //The user can never click a non ../ dir to get back to root, so no need to check for that, just load up the data and refresh the view!
                
                
                interflow.getInterflowFiles(apiKey: self.APIKey, path: item["Name"] as! String){ fileResponse in
                    interflow.getInterflowDirs(apiKey: self.APIKey, path: item["Name"] as! String){ dirResponse in
                        self.reloadFileList(
                            ContentFiles:       (fileResponse["data"] as! [[String:Any]]),
                            ContentDirs:        (dirResponse["data"] as! [String])
                        )
                        self.ProgressBar.isIndeterminate = false
                        self.ProgressBar.stopAnimation(nil)
                    }
                }
                LastDir = CurrentDir
                CurrentDir += item["Name"] as! String
            }
            else if(item["Name"] as! String == "../")
            {
                //user is trying to go back a directory.
                //someone might try and see what is before the root dir... dont let them (theres nothing there)
                
                if(CurrentDir == "")
                {
                    self.ProgressBar.isIndeterminate = false
                    self.ProgressBar.stopAnimation(nil)
                    return
                }
                
                //take the view back up a directory
                if(LastDir == "")
                {
                    //we need to show the shared files in the root directory, so we handle that seperately
                    interflow.getInterflowFiles(apiKey: self.APIKey, path: ""){ fileResponse in
                        interflow.getInterflowDirs(apiKey: self.APIKey, path: ""){ dirResponse in
                            interflow.getInterflowSharedFiles(apiKey: self.APIKey){ sharedResponse in
                                self.reloadFileList(
                                    ContentFiles:       (fileResponse["data"] as! [[String:Any]]),
                                    ContentDirs:        (dirResponse["data"] as! [String]),
                                    ContentSharedFiles: (sharedResponse["data"] as! [[String:Any]])
                                )
                                self.ProgressBar.isIndeterminate = false
                                self.ProgressBar.stopAnimation(nil)
                            }
                        }
                    }
                }
                else
                {
                    //not going into root, so dont show the shared files
                    interflow.getInterflowFiles(apiKey: self.APIKey, path: ""){ fileResponse in
                        interflow.getInterflowDirs(apiKey: self.APIKey, path: ""){ dirResponse in
                            self.reloadFileList(
                                ContentFiles:       (fileResponse["data"] as! [[String:Any]]),
                                ContentDirs:        (dirResponse["data"] as! [String])
                            )
                            self.ProgressBar.isIndeterminate = false
                            self.ProgressBar.stopAnimation(nil)
                        }
                    }
                }
                
                //change LastDir and CurrentDir
                CurrentDir = LastDir
                if(LastDir != "")
                {
                    print("lastdir is: " + LastDir)
                    //check to see if we have a trailing slash, if we do that will mess up the next part
                    if(LastDir.characters.last! == "/")
                    {
                        LastDir.remove(at: LastDir.index(before: LastDir.endIndex))
                    }
                    let endIndex = LastDir.lastIndex(of: "/")
                    LastDir = (LastDir as NSString).substring(with: NSRange(location: 0, length: endIndex!))
                    print("Lastdir is now "+LastDir)
                }
            }
        }
        else
        {
            //User clicked on a file. Lets open up a save dialog, then output the file in that directory
            let saveDiag: NSSavePanel = NSSavePanel()
            saveDiag.title = "Save Interflow File"
            saveDiag.canCreateDirectories = true
            saveDiag.isExtensionHidden = false
            
            //add the name of the file as the default name
            saveDiag.nameFieldStringValue = item["Name"] as! String
            
            saveDiag.begin(){ buttonPress in
                if(buttonPress == NSFileHandlingPanelOKButton)
                {
                    //save the file!
                    
                    //begin moving the progress bar
                    self.ProgressBar.isIndeterminate = true
                    self.ProgressBar.startAnimation(nil)
                    
                    //All files in the root are shared files, and files which are not in root are regular files. use this to determine what download endpoint to use.
                    //another option would be to check the RelativePath, as shared files do not have a RelativePath
                    if(self.CurrentDir != "")
                    {
                        //normal file download
                        interflow.downloadInterflowFile(apiKey: self.APIKey, interflow_path: self.CurrentDir, interflow_file: item["Name"] as! String, output_path: saveDiag.url!){ response in
                            self.ProgressBar.isIndeterminate = false
                            self.ProgressBar.stopAnimation(nil)
                            if(response != 200)
                            {
                                //some error happened. alert on this
                                let myPopup: NSAlert = NSAlert()
                                myPopup.messageText = "There was an error downloading the file. Download resulted in status code " + String(response) + "\nPlease try downloading the file again"
                                myPopup.alertStyle = NSAlertStyle.warning
                                myPopup.addButton(withTitle: "OK")
                                myPopup.runModal()
                            }
                        }
                    }
                    else
                    {
                        //shared file download
                        interflow.downloadInterflowSharedFile(apiKey: self.APIKey, interflow_file: item["Name"] as! String, output_path: saveDiag.url!){ response in
                            self.ProgressBar.isIndeterminate = false
                            self.ProgressBar.stopAnimation(nil)
                            if(response != 200)
                            {
                                //some error happened. alert on this
                                let myPopup: NSAlert = NSAlert()
                                myPopup.messageText = "There was an error downloading the shared file. Download resulted in status code " + String(response) + "\nPlease try downloading the file again"
                                myPopup.alertStyle = NSAlertStyle.warning
                                myPopup.addButton(withTitle: "OK")
                                myPopup.runModal()
                            }

                        }
                    }
                }
                else
                {
                    //user clicked cancel
                    return
                }
            }
        }
    }
}

//Define an extention to return the number of rows in the view table
extension ViewController: NSTableViewDataSource{
    func numberOfRows(in tableView: NSTableView) -> Int {
        return directoryItems.count 
    }
}

//Define an extention to fill the data in the view table
extension ViewController: NSTableViewDelegate{
    fileprivate enum CellIdentifiers {
        static let NameCell = "NameCellID"
        static let LengthCell = "LengthCellID"
        static let RelPathCell = "RelativePathCellID"
        static let LastModCell = "LastModifiedCellID"
    }
    
    func tableView(_ tableView: NSTableView, viewFor tableColumn: NSTableColumn?, row: Int) -> NSView? {
        
        var text: String = ""
        var cellIdentifier: String = ""
        
        let dateFormatter = DateFormatter()
        dateFormatter.dateStyle = .long
        dateFormatter.timeStyle = .long
        
        guard let item = directoryItems[row] else {
            return nil
        }
        
        if tableColumn == tableView.tableColumns[0]{
            //set text of name cells here
            text = item["Name"]! as! String
            cellIdentifier = CellIdentifiers.NameCell
        }
        else if tableColumn == tableView.tableColumns[1]{
            //set file size here
            text = item["Length"] as! String
            cellIdentifier = CellIdentifiers.LengthCell
        }
        else if tableColumn == tableView.tableColumns[2]{
            //set rel path here
            text = item["RelativePath"]! as? String ?? ""
            cellIdentifier = CellIdentifiers.RelPathCell
        }
        else if tableColumn == tableView.tableColumns[3]{
            text = item["LastModified"] as? String ?? "Never Modified"
            cellIdentifier = CellIdentifiers.LastModCell
        }
        
        if let cell = tableView.make(withIdentifier: cellIdentifier, owner: nil) as? NSTableCellView {
            cell.textField?.stringValue = text
            return cell
        }
        return nil
    }
    
}


//some functionality that strings should have. Thanks Martin R for sharing this snippet on github!
extension String {
    func index(of target: String) -> Int? {
        if let range = self.range(of: target) {
            return characters.distance(from: startIndex, to: range.lowerBound)
        } else {
            return nil
        }
    }
    
    func lastIndex(of target: String) -> Int? {
        if let range = self.range(of: target, options: .backwards) {
            return characters.distance(from: startIndex, to: range.lowerBound)
        } else {
            return nil
        }
    }
}
